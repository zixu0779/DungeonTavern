using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

internal static class PaletteOriginalLayoutBuilder
{
    private const string ArtRoot = "Assets/DungeonTavern/Art";
    private const string PaletteRoot = ArtRoot + "/Palettes";
    private const string TileRoot = ArtRoot + "/TileAssets";
    private const int SheetGap = 2;

    private sealed class PaletteDefinition
    {
        public string Name;
        public string[] Sources;

        public PaletteDefinition(string name, params string[] sources)
        {
            Name = name;
            Sources = sources;
        }
    }

    private static readonly PaletteDefinition[] Definitions =
    {
        new PaletteDefinition("Dungeon_Decoration",
            ArtRoot + "/Environment/Decoration/TX Props.png"),
        new PaletteDefinition("Dungeon_Ground",
            ArtRoot + "/Environment/Ground/TX Tileset Stone Ground.png"),
        new PaletteDefinition("Dungeon_Walls",
            ArtRoot + "/Environment/Walls/TX Tileset Wall.png",
            ArtRoot + "/Environment/Walls/Walls_interior.png"),
        new PaletteDefinition("Tavern_Furniture",
            ArtRoot + "/Environment/Furniture/Interior_1st_floor.png",
            ArtRoot + "/Environment/Furniture/Interior_2nd_floor.png"),
        new PaletteDefinition("Tavern_Structure",
            ArtRoot + "/Environment/Structure/TX Struct.png")
    };

    [MenuItem("Tools/Dungeon Tavern/Rebuild Palettes From Original Layout")]
    private static void Rebuild()
    {
        if (!EditorUtility.DisplayDialog(
                "Rebuild Palettes",
                "Recreate Tile assets and palettes from the current sprite slicing? Sprite placement will follow each source image's original coordinates.",
                "Rebuild",
                "Cancel"))
            return;

        EnsureFolder(ArtRoot, "TileAssets");
        int createdTiles = 0;
        int relocatedCount = 0;
        int offGridCount = 0;

        foreach (PaletteDefinition definition in Definitions)
        {
            string tileFolder = TileRoot + "/" + definition.Name;
            RecreateFolder(TileRoot, definition.Name);
            Tilemap tilemap = LoadPaletteTilemap(definition.Name, out GameObject root, out string prefabPath);
            if (tilemap == null)
            {
                Debug.LogError($"[DungeonTavern] Palette '{definition.Name}' has no Tilemap.");
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
                continue;
            }

            try
            {
                tilemap.ClearAllTiles();
                var occupied = new HashSet<Vector3Int>();
                int sheetOriginX = 0;

                foreach (string sourcePath in definition.Sources)
                {
                    TextureImporter importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
                    if (importer == null)
                        continue;

                    float ppu = importer.spritePixelsPerUnit;
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
                    Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(sourcePath)
                        .OfType<Sprite>()
                        .OrderBy(sprite => sprite.rect.y)
                        .ThenBy(sprite => sprite.rect.x)
                        .ToArray();

                    foreach (Sprite sprite in sprites)
                    {
                        float cellXFloat = sprite.rect.x / ppu;
                        float cellYFloat = sprite.rect.y / ppu;
                        int localX = Mathf.RoundToInt(cellXFloat);
                        int localY = Mathf.RoundToInt(cellYFloat);
                        if (!Mathf.Approximately(cellXFloat, localX) || !Mathf.Approximately(cellYFloat, localY))
                        {
                            offGridCount++;
                            Debug.LogWarning($"[DungeonTavern] Off-grid sprite rounded for palette placement: {sourcePath} / {sprite.name}, rect origin ({sprite.rect.x}, {sprite.rect.y}), PPU {ppu}.");
                        }

                        Vector3Int requestedCell = new Vector3Int(sheetOriginX + localX, localY, 0);
                        Vector3Int cell = requestedCell;
                        if (occupied.Contains(cell))
                        {
                            cell = FindNearestFreeCell(requestedCell, occupied);
                            relocatedCount++;
                            Debug.LogWarning($"[DungeonTavern] Palette cell collision; sprite moved to nearest free cell: {sourcePath} / {sprite.name}, {requestedCell} -> {cell}.");
                        }
                        occupied.Add(cell);

                        var tile = ScriptableObject.CreateInstance<Tile>();
                        tile.sprite = sprite;
                        tile.name = sprite.name;
                        string filename = MakeSafeFilename(Path.GetFileNameWithoutExtension(sourcePath) + "_" + sprite.name) + ".asset";
                        string tilePath = AssetDatabase.GenerateUniqueAssetPath(tileFolder + "/" + filename);
                        AssetDatabase.CreateAsset(tile, tilePath);
                        tilemap.SetTile(cell, tile);
                        createdTiles++;
                    }

                    int sheetWidthCells = texture != null
                        ? Mathf.CeilToInt(texture.width / ppu)
                        : Mathf.CeilToInt(sprites.Select(s => s.rect.xMax / ppu).DefaultIfEmpty(0).Max());
                    sheetOriginX += sheetWidthCells + SheetGap;
                }

                tilemap.CompressBounds();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DungeonTavern] Palettes rebuilt from original sprite positions: {createdTiles} Tiles, {Definitions.Length} palettes, {relocatedCount} collision relocations, {offGridCount} off-grid origins.");
    }

    private static Tilemap LoadPaletteTilemap(string paletteName, out GameObject root, out string prefabPath)
    {
        prefabPath = PaletteRoot + "/" + paletteName + "/" + paletteName + ".prefab";
        root = PrefabUtility.LoadPrefabContents(prefabPath);
        return root.GetComponentInChildren<Tilemap>(true);
    }

    private static void RecreateFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (AssetDatabase.IsValidFolder(path))
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateFolder(parent, child);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static string MakeSafeFilename(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value.Replace('/', '_').Replace('\\', '_');
    }

    private static Vector3Int FindNearestFreeCell(Vector3Int origin, HashSet<Vector3Int> occupied)
    {
        for (int radius = 1; radius <= 64; radius++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        continue;
                    Vector3Int candidate = new Vector3Int(origin.x + x, origin.y + y, 0);
                    if (!occupied.Contains(candidate))
                        return candidate;
                }
            }
        }
        throw new InvalidOperationException($"No free palette cell found near {origin}.");
    }
}

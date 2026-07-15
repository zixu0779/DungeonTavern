using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

internal static class SpriteWorkspaceCleanup
{
    private const string ArtRoot = "Assets/DungeonTavern/Art";
    private const string TileAssetsRoot = ArtRoot + "/TileAssets";
    private const string PalettesRoot = ArtRoot + "/Palettes";

    private static readonly string[] SpriteSheets =
    {
        ArtRoot + "/Environment/Decoration/Decorative_cracks.png",
        ArtRoot + "/Environment/Decoration/TX Props.png",
        ArtRoot + "/Environment/Ground/TX Tileset Stone Ground.png",
        ArtRoot + "/Environment/Structure/TX Struct.png",
        ArtRoot + "/Environment/Walls/TX Tileset Wall.png",
        ArtRoot + "/Shared/Shadows/TX Shadow.png",
        ArtRoot + "/Tavern/Furniture/Interior_1st_floor.png",
        ArtRoot + "/Tavern/Furniture/Interior_2nd_floor.png",
        ArtRoot + "/Tavern/Structure/Animation_windows_doors.png",
        ArtRoot + "/Tavern/Structure/Walls_interior.png",
        ArtRoot + "/Tavern/Structure/door_small.png"
    };

    [MenuItem("Tools/Dungeon Tavern/Clear Sprite Slicing Workspace")]
    private static void ClearWorkspace()
    {
        if (!EditorUtility.DisplayDialog(
                "Clear Sprite Slicing Workspace",
                "This removes generated Tile assets, empties project palettes and clears sprite rectangles on environment/tavern sprite sheets. Original PNG files and character animations are retained.",
                "Clear",
                "Cancel"))
        {
            return;
        }

        int clearedPalettes = ClearPalettePrefabs();
        int deletedTiles = DeleteGeneratedTiles();
        int clearedSheets = ClearSpriteRects();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DungeonTavern] Sprite workspace cleared: {clearedSheets} sprite sheets, {deletedTiles} Tile assets, {clearedPalettes} palettes. Original PNG files retained.");
    }

    private static int ClearPalettePrefabs()
    {
        int count = 0;
        string[] paletteGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PalettesRoot });
        foreach (string guid in paletteGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
                {
                    tilemap.ClearAllTiles();
                    tilemap.CompressBounds();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                count++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        return count;
    }

    private static int DeleteGeneratedTiles()
    {
        if (!AssetDatabase.IsValidFolder(TileAssetsRoot))
            return 0;

        int count = AssetDatabase.FindAssets("t:TileBase", new[] { TileAssetsRoot }).Length;
        AssetDatabase.DeleteAsset(TileAssetsRoot);
        EnsureFolder(ArtRoot, "TileAssets");
        return count;
    }

    private static int ClearSpriteRects()
    {
        int count = 0;
        var factories = new SpriteDataProviderFactories();
        factories.Init();

        foreach (string path in SpriteSheets)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(Array.Empty<SpriteRect>());
            provider.Apply();
            importer.SaveAndReimport();
            count++;
        }
        return count;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}

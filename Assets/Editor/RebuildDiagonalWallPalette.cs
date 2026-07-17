using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

internal static class RebuildDiagonalWallPalette
{
    private const string PalettePath = "Assets/DungeonTavern/Art/Palettes/Dungeon_Walls/Dungeon_Walls.prefab";
    private const string TileDirectory = "Assets/DungeonTavern/Art/TileAssets/Dungeon_Walls";

    private static readonly (string slope, string direction, int y)[] Rows =
    {
        ("1x1", "Up", 7),
        ("1x1", "Down", 6),
        ("2x1", "Up", 4),
        ("2x1", "Down", 3),
        ("3x2", "Up", 1),
        ("3x2", "Down", 0),
    };

    [MenuItem("Tools/Dungeon Tavern/Rebuild Diagonal Wall Palette")]
    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var root = PrefabUtility.LoadPrefabContents(PalettePath);
        var cleared = 0;
        try
        {
            var tilemap = root.GetComponentInChildren<Tilemap>(true)
                ?? throw new InvalidOperationException("Wall palette has no Tilemap.");

            foreach (var position in tilemap.cellBounds.allPositionsWithin)
            {
                var tile = tilemap.GetTile(position);
                if (tile != null && tile.name.StartsWith("Walls_Diagonal_", StringComparison.Ordinal))
                {
                    tilemap.SetTile(position, null);
                    cleared++;
                }
            }

            foreach (var row in Rows)
            {
                for (var index = 0; index < 10; index++)
                {
                    var name = $"Walls_Diagonal_{row.slope}_{row.direction}_{index:00}";
                    var path = $"{TileDirectory}/{name}.asset";
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(path)
                        ?? throw new FileNotFoundException("Required diagonal Tile is missing", path);
                    tilemap.SetTile(new Vector3Int(42 + index, row.y, 0), tile);
                }
            }

            tilemap.CompressBounds();
            EditorUtility.SetDirty(tilemap);
            PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        var removed = 0;
        foreach (var absolutePath in Directory.GetFiles(TileDirectory, "Walls_Diagonal_Walls_Diagonal_*.asset"))
        {
            var assetPath = absolutePath.Replace('\\', '/');
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                assetPath = "Assets/" + assetPath.Substring(assetPath.IndexOf("Assets/", StringComparison.Ordinal) + 7);
            }
            if (!AssetDatabase.DeleteAsset(assetPath))
                throw new InvalidOperationException("Could not delete obsolete diagonal Tile: " + assetPath);
            removed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Validate();
        Debug.Log($"Diagonal wall Palette rebuilt: cleared {cleared} old cells, placed 60 Tiles, removed {removed} obsolete Tiles.");
    }

    [MenuItem("Tools/Dungeon Tavern/Validate Diagonal Wall Series")]
    public static void Validate()
    {
        const string atlasPath = "Assets/DungeonTavern/Art/Environment/Walls/Walls_Diagonal.png";
        if (AssetDatabase.AssetPathToGUID(atlasPath) != "70ae1e6fe186d4d64ab6bd49987d5820")
            throw new InvalidOperationException("Walls_Diagonal atlas GUID changed.");

        var importer = AssetImporter.GetAtPath(atlasPath) as TextureImporter
            ?? throw new InvalidOperationException("Walls_Diagonal has no TextureImporter.");
        if (importer.spriteImportMode != SpriteImportMode.Multiple || importer.spritePixelsPerUnit != 16 ||
            importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
            importer.textureCompression != TextureImporterCompression.Uncompressed)
            throw new InvalidOperationException("Walls_Diagonal importer settings are invalid.");

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath)
            ?? throw new InvalidOperationException("Walls_Diagonal texture failed to import.");
        if (texture.width != 352 || texture.height != 533)
            throw new InvalidOperationException($"Unexpected atlas size {texture.width}x{texture.height}.");

        var sprites = AssetDatabase.LoadAllAssetsAtPath(atlasPath).OfType<Sprite>().ToArray();
        if (sprites.Length != 60 || sprites.Any(sprite => Math.Abs(sprite.rect.width - 16) > 0.001f))
            throw new InvalidOperationException("Expected exactly 60 sixteen-pixel-wide diagonal Sprites.");

        var expectedPositions = new HashSet<Vector3Int>();
        var root = PrefabUtility.LoadPrefabContents(PalettePath);
        try
        {
            var tilemap = root.GetComponentInChildren<Tilemap>(true)
                ?? throw new InvalidOperationException("Wall palette has no Tilemap.");
            foreach (var row in Rows)
            {
                for (var index = 0; index < 10; index++)
                {
                    var position = new Vector3Int(42 + index, row.y, 0);
                    expectedPositions.Add(position);
                    var expectedName = $"Walls_Diagonal_{row.slope}_{row.direction}_{index:00}";
                    var tile = tilemap.GetTile<Tile>(position);
                    if (tile == null || tile.name != expectedName || tile.sprite == null ||
                        tile.sprite.name != expectedName || tile.colliderType != Tile.ColliderType.Sprite)
                    {
                        var actual = tile == null
                            ? "null Tile"
                            : $"Tile={tile.name}, Sprite={(tile.sprite == null ? "null" : tile.sprite.name)}, Collider={tile.colliderType}";
                        throw new InvalidOperationException($"Invalid diagonal Tile at Palette cell {position}: expected {expectedName}; actual {actual}.");
                    }
                }
            }

            var diagonalCells = 0;
            foreach (var position in tilemap.cellBounds.allPositionsWithin)
            {
                var tile = tilemap.GetTile(position);
                if (tile != null && tile.name.StartsWith("Walls_Diagonal_", StringComparison.Ordinal))
                {
                    diagonalCells++;
                    if (!expectedPositions.Contains(position))
                        throw new InvalidOperationException($"Unexpected diagonal Tile outside the planned block at {position}.");
                }
            }
            if (diagonalCells != 60)
                throw new InvalidOperationException($"Expected 60 diagonal Palette cells, found {diagonalCells}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        var tileAssets = Directory.GetFiles(TileDirectory, "Walls_Diagonal_*.asset");
        if (tileAssets.Length != 60 || tileAssets.Any(path => Path.GetFileName(path).StartsWith("Walls_Diagonal_Walls_Diagonal_", StringComparison.Ordinal)))
            throw new InvalidOperationException("Diagonal Tile asset set does not contain exactly the 60 formal series Tiles.");

        Debug.Log("Diagonal wall validation passed: atlas GUID/importer, 60 Sprites, 60 Tiles, and all Palette positions are valid.");
    }
}

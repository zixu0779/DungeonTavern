using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

internal static class FinalizeVerticalWallConnections
{
    private const string AtlasPath = "Assets/DungeonTavern/Art/Environment/Walls/Walls_Vertical_Connections.png";
    private const string PalettePath = "Assets/DungeonTavern/Art/Palettes/Dungeon_Walls/Dungeon_Walls.prefab";
    private const string TemporaryPalettePath = "Assets/DungeonTavern/Art/Palettes/Dungeon_Walls/Dungeon_Walls_Vertical_Temp.prefab";
    private const string TileDirectory = "Assets/DungeonTavern/Art/TileAssets/Dungeon_Walls";
    private const int StartX = 57;
    private const string ThirdTileSpecialPrefix = "Walls_HV_Special_RightDownThirdTileShort_";
    private const string UserExtendedSprite = "Walls_HV_RightToDown_04_Special";
    private static readonly Vector3Int UserPalettePosition = new Vector3Int(68, 16, 0);

    private static readonly (string prefix, int y)[] Rows =
    {
        ("Walls_HV_LeftToDown_", 20),
        ("Walls_HV_RightToDown_", 16),
        ("Walls_HV_LeftToUp_", 11),
        ("Walls_HV_RightToUp_", 6),
    };

    private static readonly (string name, int x)[] Singles =
    {
        ("Walls_HV_Special_LeftDownStripToLeft", StartX),
        ("Walls_HV_Special_RightDownStripToRight", StartX + 1),
    };

    [MenuItem("Tools/Dungeon Tavern/Finalize Vertical Wall Connections")]
    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateImporterAndAssets();

        var paletteAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath)
            ?? throw new FileNotFoundException("Wall Palette prefab is missing.", PalettePath);
        var previewScene = EditorSceneManager.NewPreviewScene();
        var root = UnityEngine.Object.Instantiate(paletteAsset);
        // The previous temporary-prefab workflow accidentally persisted the temporary
        // root name. Always restore the public Palette asset name explicitly.
        root.name = Path.GetFileNameWithoutExtension(PalettePath);
        SceneManager.MoveGameObjectToScene(root, previewScene);
        try
        {
            var tilemap = root.GetComponentInChildren<Tilemap>(true)
                ?? throw new InvalidOperationException("Wall Palette has no Tilemap.");
            Undo.RegisterCompleteObjectUndo(tilemap, "Finalize Vertical Wall Connections");

            // Clear every coordinate used by either the corrected layout or the
            // discarded 54-piece layout. Deleted Tile assets deserialize as
            // nameless missing references, so name-based cleanup cannot detect
            // those stale cells.
            foreach (var y in new[] { 26, 21, 20, 16, 11, 6 })
            {
                for (var x = StartX; x < StartX + 10; x++)
                    tilemap.SetTile(new Vector3Int(x, y, 0), null);
            }
            foreach (var y in new[] { 31, 25 })
            {
                tilemap.SetTile(new Vector3Int(StartX, y, 0), null);
                tilemap.SetTile(new Vector3Int(StartX + 1, y, 0), null);
            }
            for (var x = StartX; x < StartX + 10; x++)
                tilemap.SetTile(new Vector3Int(x, 1, 0), null);
            for (var x = StartX; x < StartX + 2; x++)
                tilemap.SetTile(new Vector3Int(x, -1, 0), null);

            foreach (var position in tilemap.cellBounds.allPositionsWithin)
            {
                var existing = tilemap.GetTile(position);
                if (existing != null &&
                    existing.name != UserExtendedSprite &&
                    (existing.name.StartsWith("Walls_Vertical_", StringComparison.Ordinal) ||
                     existing.name.StartsWith("Walls_HV_", StringComparison.Ordinal)))
                {
                    tilemap.SetTile(position, null);
                }
            }

            Place(tilemap, new Vector3Int(StartX, 25, 0), "Walls_Vertical_Right");
            Place(tilemap, new Vector3Int(StartX + 1, 25, 0), "Walls_Vertical_Left");

            foreach (var row in Rows)
            {
                for (var index = 0; index < 10; index++)
                {
                    var position = new Vector3Int(StartX + index, row.y, 0);
                    var existing = tilemap.GetTile(position);
                    if (existing != null)
                        throw new InvalidOperationException($"Palette target {position} is occupied by unrelated Tile {existing.name}.");
                    Place(tilemap, position, row.prefix + index.ToString("00"));
                }
            }
            foreach (var single in Singles)
                Place(tilemap, new Vector3Int(single.x, -1, 0), single.name);
            for (var index = 0; index < 10; index++)
                Place(tilemap, new Vector3Int(StartX + index, 1, 0),
                    ThirdTileSpecialPrefix + index.ToString("00"));
            Place(tilemap, UserPalettePosition, UserExtendedSprite);

            ValidateTilemap(tilemap);
            tilemap.CompressBounds();
            ValidateTilemap(tilemap);
            tilemap.RefreshAllTiles();
            EditorUtility.SetDirty(tilemap);
            EditorUtility.SetDirty(tilemap.gameObject);
            EditorUtility.SetDirty(root);
            PrefabUtility.RecordPrefabInstancePropertyModifications(tilemap);
            EditorSceneManager.MarkSceneDirty(root.scene);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TemporaryPalettePath) != null)
                AssetDatabase.DeleteAsset(TemporaryPalettePath);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, TemporaryPalettePath);
            if (saved == null)
                throw new InvalidOperationException("Could not save the temporary Wall Palette prefab.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }

        ValidatePaletteAtPath(TemporaryPalettePath);
        File.Copy(TemporaryPalettePath, PalettePath, true);
        AssetDatabase.DeleteAsset(TemporaryPalettePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(PalettePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        RestoreManualCellSizing();
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(PalettePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidatePalette();
        Debug.Log("Vertical wall kit finalized: 55 Sprites, 55 Tiles, and 55 Palette cells.");
    }

    private static void Place(Tilemap tilemap, Vector3Int position, string name)
    {
        var path = $"{TileDirectory}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path)
            ?? throw new FileNotFoundException("Required vertical-wall Tile is missing.", path);
        tilemap.SetTile(position, tile);
    }

    private static void ValidateImporterAndAssets()
    {
        var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter
            ?? throw new InvalidOperationException("Vertical-wall atlas has no TextureImporter.");
        if (importer.spriteImportMode != SpriteImportMode.Multiple ||
            importer.spritePixelsPerUnit != 16 ||
            importer.filterMode != FilterMode.Point ||
            importer.mipmapEnabled ||
            importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            throw new InvalidOperationException("Vertical-wall atlas importer settings are invalid.");
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath)
            ?? throw new InvalidOperationException("Vertical-wall atlas failed to import.");
        if (texture.width != 176 || texture.height != 321)
            throw new InvalidOperationException($"Unexpected vertical-wall atlas size {texture.width}x{texture.height}.");

        var sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
        if (sprites.Length != 55)
            throw new InvalidOperationException($"Expected 55 vertical-wall Sprites, found {sprites.Length}.");

        var names = new HashSet<string>(sprites.Select(sprite => sprite.name));
        if (!names.Contains(UserExtendedSprite))
            throw new InvalidOperationException($"Missing user-authored Sprite: {UserExtendedSprite}.");
        var userTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDirectory}/{UserExtendedSprite}.asset");
        if (userTile == null || userTile.sprite == null ||
            userTile.sprite.name != UserExtendedSprite ||
            userTile.colliderType != Tile.ColliderType.Sprite)
        {
            throw new InvalidOperationException($"Invalid user-authored Tile: {UserExtendedSprite}.");
        }
        foreach (var side in new[] { "Right", "Left" })
        {
            var name = $"Walls_Vertical_{side}";
            if (!names.Contains(name))
                throw new InvalidOperationException($"Missing vertical body Sprite: {side}.");
            var tile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDirectory}/{name}.asset");
            if (tile == null || tile.sprite == null || tile.sprite.name != name ||
                tile.colliderType != Tile.ColliderType.Sprite)
            {
                throw new InvalidOperationException($"Invalid vertical body Tile: {name}.");
            }
        }
        foreach (var row in Rows)
        {
            for (var index = 0; index < 10; index++)
            {
                var name = row.prefix + index.ToString("00");
                if (!names.Contains(name))
                    throw new InvalidOperationException($"Missing connector Sprite: {name}.");
                var tile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDirectory}/{name}.asset");
                if (tile == null || tile.sprite == null || tile.sprite.name != name ||
                    tile.colliderType != Tile.ColliderType.Sprite)
                {
                    throw new InvalidOperationException($"Invalid connector Tile: {name}.");
                }
            }
        }
        foreach (var single in Singles)
        {
            if (!names.Contains(single.name))
                throw new InvalidOperationException($"Missing special connector Sprite: {single.name}.");
            var tile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDirectory}/{single.name}.asset");
            if (tile == null || tile.sprite == null || tile.sprite.name != single.name ||
                tile.colliderType != Tile.ColliderType.Sprite)
            {
                throw new InvalidOperationException($"Invalid special connector Tile: {single.name}.");
            }
        }
        for (var index = 0; index < 10; index++)
        {
            var name = ThirdTileSpecialPrefix + index.ToString("00");
            if (!names.Contains(name))
                throw new InvalidOperationException($"Missing third-Tile special Sprite: {name}.");
            var tile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDirectory}/{name}.asset");
            if (tile == null || tile.sprite == null || tile.sprite.name != name ||
                tile.colliderType != Tile.ColliderType.Sprite)
            {
                throw new InvalidOperationException($"Invalid third-Tile special Tile: {name}.");
            }
        }
    }

    private static void ValidatePalette()
    {
        ValidatePaletteAtPath(PalettePath);
    }

    private static void ValidatePaletteAtPath(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var tilemap = root.GetComponentInChildren<Tilemap>(true)
                ?? throw new InvalidOperationException("Wall Palette has no Tilemap.");
            ValidateTilemap(tilemap);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ValidateTilemap(Tilemap tilemap)
    {
        Require(tilemap, new Vector3Int(StartX, 25, 0), "Walls_Vertical_Right");
        Require(tilemap, new Vector3Int(StartX + 1, 25, 0), "Walls_Vertical_Left");
        foreach (var row in Rows)
        {
            for (var index = 0; index < 10; index++)
                Require(tilemap, new Vector3Int(StartX + index, row.y, 0), row.prefix + index.ToString("00"));
        }
        foreach (var single in Singles)
            Require(tilemap, new Vector3Int(single.x, -1, 0), single.name);
        for (var index = 0; index < 10; index++)
            Require(tilemap, new Vector3Int(StartX + index, 1, 0),
                ThirdTileSpecialPrefix + index.ToString("00"));
        Require(tilemap, UserPalettePosition, UserExtendedSprite);
    }

    private static void RestoreManualCellSizing()
    {
        var paletteSettings = AssetDatabase.LoadAllAssetsAtPath(PalettePath)
            .FirstOrDefault(asset => asset != null && asset.GetType().FullName == "UnityEditor.GridPalette");
        if (paletteSettings == null)
        {
            var gridPaletteType = Type.GetType("UnityEditor.GridPalette, UnityEditor")
                ?? throw new InvalidOperationException("UnityEditor.GridPalette type is unavailable.");
            paletteSettings = ScriptableObject.CreateInstance(gridPaletteType);
            paletteSettings.name = "Palette Settings";
            AssetDatabase.AddObjectToAsset(paletteSettings, PalettePath);
        }
        var serialized = new SerializedObject(paletteSettings);
        var cellSizing = serialized.FindProperty("cellSizing")
            ?? throw new InvalidOperationException("GridPalette cellSizing property is missing.");
        cellSizing.intValue = 100;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(paletteSettings);
    }

    private static void Require(Tilemap tilemap, Vector3Int position, string name)
    {
        var tile = tilemap.GetTile<Tile>(position);
        if (tile == null || tile.name != name || tile.sprite == null || tile.sprite.name != name)
            throw new InvalidOperationException($"Invalid Palette cell {position}; expected {name}.");
    }
}

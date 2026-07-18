using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

internal static class InstallAgedWoodGround
{
    private const string SourcePath = "Assets/DungeonTavern/Art/Environment/Ground/Ground_AgedWood_Seamless.png";
    private const string TileFolder = "Assets/DungeonTavern/Art/TileAssets/Dungeon_Ground/AgedWood";
    private const string VariantBSourcePath = "Assets/DungeonTavern/Art/Environment/Ground/Ground_AgedWood_Seamless_VariantB.png";
    private const string VariantBTileFolder = "Assets/DungeonTavern/Art/TileAssets/Dungeon_Ground/AgedWoodVariantB";
    private const string PalettePath = "Assets/DungeonTavern/Art/Palettes/Dungeon_Ground/Dungeon_Ground.prefab";
    private const string ScenePath = "Assets/Scenes/Tavern/Tavern_ReadabilityPrototype.unity";
    private const string SourcePrefix = "Ground_AgedWood_Seamless";
    private const string VariantBPrefix = "Ground_AgedWood_Seamless_VariantB";

    [MenuItem("Tools/Dungeon Tavern/Install Aged Wood Ground Tiles And Palette")]
    private static void Install()
    {
        InstallAtlas(SourcePath, TileFolder, SourcePrefix);
        Debug.Log("[DungeonTavern] Aged wood Ground tiles installed and added to Dungeon_Ground Palette.");
    }

    [MenuItem("Tools/Dungeon Tavern/Install Aged Wood Variant B Tiles And Palette")]
    private static void InstallVariantB()
    {
        InstallAtlas(VariantBSourcePath, VariantBTileFolder, VariantBPrefix);
        Debug.Log("[DungeonTavern] Aged wood Variant B tiles installed and added to Dungeon_Ground Palette.");
    }

    private static void InstallAtlas(string sourcePath, string tileFolder, string spritePrefix)
    {
        ConfigureImporterAndSlice(sourcePath, spritePrefix);
        var tiles = CreateTiles(sourcePath, tileFolder, spritePrefix);
        AddTilesToGroundPalette(tiles, spritePrefix);
        SetAuthoritativeSceneSorting();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureImporterAndSlice(string sourcePath, string spritePrefix)
    {
        if (AssetImporter.GetAtPath(sourcePath) is not TextureImporter importer)
            throw new FileNotFoundException("Aged wood texture importer not found.", sourcePath);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        SetUncompressedPlatform(importer, "Standalone");
        SetUncompressedPlatform(importer, "Android");
        SetUncompressedPlatform(importer, "WebGL");

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        var rects = new SpriteRect[32];
        var index = 0;
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 8; column++)
        {
            rects[index] = new SpriteRect
            {
                name = $"{spritePrefix}_{index}",
                rect = new Rect(column * 32, (3 - row) * 32, 32, 32),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate()
            };
            index++;
        }

        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static void SetUncompressedPlatform(TextureImporter importer, string platform)
    {
        var settings = importer.GetPlatformTextureSettings(platform);
        settings.name = platform;
        settings.overridden = true;
        settings.maxTextureSize = 2048;
        settings.textureCompression = TextureImporterCompression.Uncompressed;
        settings.crunchedCompression = false;
        importer.SetPlatformTextureSettings(settings);
    }

    private static Tile[] CreateTiles(string sourcePath, string tileFolder, string spritePrefix)
    {
        EnsureFolder("Assets/DungeonTavern/Art/TileAssets/Dungeon_Ground");
        EnsureFolder(tileFolder);

        var sprites = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<Sprite>()
            .OrderBy(sprite => int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1)))
            .ToArray();
        if (sprites.Length != 32)
            throw new InvalidOperationException($"Expected 32 aged wood sprites, found {sprites.Length}.");

        var tiles = new Tile[32];
        for (var i = 0; i < sprites.Length; i++)
        {
            var path = $"{tileFolder}/{spritePrefix}_{i}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.name = Path.GetFileNameWithoutExtension(path);
            tile.sprite = sprites[i];
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            tiles[i] = tile;
        }

        return tiles;
    }

    private static void AddTilesToGroundPalette(Tile[] tiles, string spritePrefix)
    {
        var root = PrefabUtility.LoadPrefabContents(PalettePath);
        try
        {
            var tilemap = root.GetComponentInChildren<Tilemap>(true)
                ?? throw new InvalidOperationException("Dungeon_Ground Palette has no Tilemap.");
            var existingBounds = tilemap.cellBounds;
            foreach (var position in existingBounds.allPositionsWithin)
            {
                if (IsAtlasTile(tilemap.GetTile(position), spritePrefix))
                    tilemap.SetTile(position, null);
            }

            tilemap.CompressBounds();
            var startY = FindEmptyBandStart(tilemap, 8, 4);

            for (var i = 0; i < tiles.Length; i++)
            {
                var position = new Vector3Int(i % 8, startY - i / 8, 0);
                if (tilemap.HasTile(position))
                    throw new InvalidOperationException($"Palette collision at {position}; refusing to overwrite an existing Tile.");
                tilemap.SetTile(position, tiles[i]);
            }

            tilemap.CompressBounds();
            EditorUtility.SetDirty(tilemap);
            PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool IsAtlasTile(TileBase tile, string spritePrefix)
    {
        if (tile == null) return false;
        if (HasIndexedPrefix(tile.name, spritePrefix)) return true;
        return tile is Tile concrete && concrete.sprite != null &&
               HasIndexedPrefix(concrete.sprite.name, spritePrefix);
    }

    private static bool HasIndexedPrefix(string name, string spritePrefix)
    {
        var prefix = spritePrefix + "_";
        return name.StartsWith(prefix, StringComparison.Ordinal) &&
               int.TryParse(name.Substring(prefix.Length), out _);
    }

    private static int FindEmptyBandStart(Tilemap tilemap, int width, int height)
    {
        var y = tilemap.cellBounds.yMin - 2;
        while (true)
        {
            var empty = true;
            for (var row = 0; row < height && empty; row++)
            for (var column = 0; column < width; column++)
            {
                if (tilemap.HasTile(new Vector3Int(column, y - row, 0)))
                {
                    empty = false;
                    break;
                }
            }

            if (empty) return y;
            y -= height + 1;
        }
    }

    private static void SetAuthoritativeSceneSorting()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
                throw new InvalidOperationException(
                    $"Refusing to replace dirty active scene '{scene.path}' while configuring '{ScenePath}'.");
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        EnsureTilemapRenderer("Tavern_Main/World/Grid/Ground/GroundTiles", 0);
        EnsureTilemapRenderer("Tavern_Main/World/Grid/GroundDetails/GroundDetailTiles", 10);
        EnsureTilemapRenderer("Tavern_Main/World/Grid/Walls/WallTiles", 20);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureTilemapRenderer(string path, int sortingOrder)
    {
        var gameObject = GameObject.Find(path)
            ?? throw new InvalidOperationException($"Required Tilemap object not found: {path}");
        if (!gameObject.TryGetComponent<Tilemap>(out _))
            gameObject.AddComponent<Tilemap>();
        var renderer = gameObject.GetComponent<TilemapRenderer>()
            ?? gameObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        EditorUtility.SetDirty(gameObject);
        EditorUtility.SetDirty(renderer);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Invalid Unity folder path: " + path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}

using System;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

internal static class GroundAutotileImporterSetup
{
    private const string AssetPath =
        "Assets/DungeonTavern/Art/Environment/Ground/Ground_Cracked_Autotile.png";

    [MenuItem("Tools/Dungeon Tavern/Configure Ground Autotile 8x8")]
    private static void Configure()
    {
        if (AssetImporter.GetAtPath(AssetPath) is not TextureImporter importer)
        {
            Debug.LogError($"[DungeonTavern] Texture importer not found: {AssetPath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        var rects = new SpriteRect[64];
        int index = 0;
        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                rects[index++] = new SpriteRect
                {
                    name = $"Ground_Cracked_Autotile_r{row + 1}_c{column + 1}",
                    rect = new Rect(column * 32, (7 - row) * 32, 32, 32),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate()
                };
            }
        }

        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
        Debug.Log("[DungeonTavern] Ground Autotile configured: 64 sprites, 32x32, 32 PPU, Point, uncompressed.");
    }
}

using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps new Dungeon Tavern Ground PNGs on the project's pixel-art import contract.
/// This is intentionally limited to the Ground folder so unrelated art keeps its
/// existing importer settings.
/// </summary>
internal sealed class DungeonTavernGroundTextureDefaults : AssetPostprocessor
{
    private const string GroundRoot = "Assets/DungeonTavern/Art/Environment/Ground/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(GroundRoot, System.StringComparison.OrdinalIgnoreCase) ||
            !assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (assetImporter is not TextureImporter importer)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.wrapMode = TextureWrapMode.Repeat;

        SetUncompressedPlatform(importer, "Standalone");
        SetUncompressedPlatform(importer, "Android");
        SetUncompressedPlatform(importer, "WebGL");
    }

    private static void SetUncompressedPlatform(TextureImporter importer, string platform)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
        settings.name = platform;
        settings.overridden = true;
        settings.maxTextureSize = 2048;
        settings.textureCompression = TextureImporterCompression.Uncompressed;
        settings.crunchedCompression = false;
        importer.SetPlatformTextureSettings(settings);
    }
}

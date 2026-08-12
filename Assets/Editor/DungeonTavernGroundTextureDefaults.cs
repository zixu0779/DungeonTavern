using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps new Dungeon Tavern Ground PNGs on the project's oblique 2.5D import
/// contract. Ground sprites use bilinear sampling to avoid nearest-neighbour
/// shimmer while the camera moves. Mip maps stay disabled because these sources
/// are tightly packed multi-sprite atlases without mip-safe padding.
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
        importer.filterMode = FilterMode.Bilinear;
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

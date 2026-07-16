using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

internal static class GroundPaletteAndSeamlessOptimizer
{
    private const string GroundRoot = "Assets/DungeonTavern/Art/Environment/Ground";
    private const string AutotilePath = GroundRoot + "/Ground_Cracked_Autotile.png";
    private const string SeamlessPath = GroundRoot + "/Ground_Cracked_Seamless.png";
    private const string TileFolder = "Assets/DungeonTavern/Art/TileAssets/Dungeon_Ground/Autotile";
    private const string PalettePath = "Assets/DungeonTavern/Art/Palettes/Dungeon_Ground/Dungeon_Ground.prefab";
    private const string PreviewPath = "Assets/Screenshots/Ground_Seamless_Adjacency_Preview.png";

    private enum Kind { Undamaged, Crack, Damage, Repair, Mixed }

    // Coordinates are one-based (row, column), with row 1 at the top of the source image.
    private static readonly Dictionary<Kind, (int row, int col)[]> Classification = new()
    {
        [Kind.Undamaged] = new[] { (3, 3), (4, 1), (6, 1), (7, 1), (8, 6) },
        [Kind.Crack] = new[]
        {
            (1,1),(1,2),(1,3),(1,4),(1,5),(1,6),(1,8),
            (2,3),(2,4),(2,5),(2,7),(5,2),(5,7),(8,8)
        },
        [Kind.Damage] = new[]
        {
            (2,2),(2,8),(3,2),(3,8),(4,2),(6,3),(6,5),(6,6),(7,2),(7,7)
        },
        [Kind.Repair] = new[]
        {
            (4,3),(4,5),(4,6),(5,1),(5,3),(5,4),(5,5),(5,8),
            (6,2),(6,7),(6,8),(7,3),(7,4),(7,5),(7,6),(7,8),
            (8,1),(8,2),(8,3),(8,4),(8,5),(8,7)
        },
        [Kind.Mixed] = new[]
        {
            (1,7),(2,1),(2,6),(3,1),(3,4),(3,5),(3,6),(3,7),
            (4,4),(4,7),(4,8),(5,6),(6,4)
        }
    };

    [MenuItem("Tools/Dungeon Tavern/Build Ground Palette And Optimize Seamless")]
    private static void Execute()
    {
        ValidateClassification();
        Directory.CreateDirectory(TileFolder);
        OptimizeSeamless();
        AssetDatabase.ImportAsset(SeamlessPath, ImportAssetOptions.ForceUpdate);
        CreateAutotileAssetsAndPopulatePalette();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Ground Palette classification and Seamless adjacency optimization completed.");
    }

    private static void ValidateClassification()
    {
        var all = Classification.Values.SelectMany(x => x).ToArray();
        if (all.Length != 64 || all.Distinct().Count() != 64 ||
            all.Any(p => p.row < 1 || p.row > 8 || p.col < 1 || p.col > 8))
            throw new InvalidOperationException("Autotile classification must contain every 8x8 coordinate exactly once.");
    }

    private static void OptimizeSeamless()
    {
        var source = ReadTexture(SeamlessPath);
        if (source.width != 256 || source.height != 256)
            throw new InvalidOperationException("Seamless source must be 256x256.");

        var tiles = new Color32[64][];
        for (var row = 0; row < 8; row++)
        for (var col = 0; col < 8; col++)
            tiles[row * 8 + col] = ExtractTopDownTile(source, row, col);

        var horizontal = new double[64,64];
        var vertical = new double[64,64];
        for (var a = 0; a < 64; a++)
        for (var b = 0; b < 64; b++)
        {
            horizontal[a,b] = EdgeDifference(tiles[a], tiles[b], true);
            vertical[a,b] = EdgeDifference(tiles[a], tiles[b], false);
        }

        var order = Enumerable.Range(0, 64).ToArray();
        var originalScore = Score(order, horizontal, vertical);
        var best = (int[])order.Clone();
        var bestScore = originalScore;
        var rng = new System.Random(240718);

        // Deterministic simulated annealing. Swap-only means all original tile pixels remain intact.
        const int restarts = 6;
        const int iterationsPerRestart = 30000;
        for (var restart = 0; restart < restarts; restart++)
        {
            Shuffle(order, rng);
            var score = Score(order, horizontal, vertical);
            for (var iteration = 0; iteration < iterationsPerRestart; iteration++)
            {
                var a = rng.Next(64);
                var b = rng.Next(64);
                if (a == b) continue;
                Swap(order, a, b);
                var next = Score(order, horizontal, vertical);
                var temperature = 900.0 * (1.0 - iteration / (double)iterationsPerRestart) + 0.5;
                if (next <= score || rng.NextDouble() < Math.Exp((score - next) / temperature))
                    score = next;
                else
                    Swap(order, a, b);

                if (score < bestScore)
                {
                    bestScore = score;
                    Array.Copy(order, best, 64);
                }
            }
        }

        var output = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        output.filterMode = FilterMode.Point;
        for (var row = 0; row < 8; row++)
        for (var col = 0; col < 8; col++)
            PlaceTopDownTile(output, row, col, tiles[best[row * 8 + col]]);
        output.Apply(false, false);
        File.WriteAllBytes(SeamlessPath, output.EncodeToPNG());

        CreatePreview(output);
        UnityEngine.Object.DestroyImmediate(output);
        UnityEngine.Object.DestroyImmediate(source);
        Debug.Log($"Seamless edge score: {originalScore:F0} -> {bestScore:F0} ({(1.0-bestScore/originalScore)*100.0:F1}% lower). No damage pixels were edited.");
    }

    private static Texture2D ReadTexture(string assetPath)
    {
        var bytes = File.ReadAllBytes(assetPath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes, false)) throw new InvalidOperationException("Could not decode " + assetPath);
        return texture;
    }

    private static Color32[] ExtractTopDownTile(Texture2D texture, int row, int col)
    {
        // Unity texture coordinates start at the bottom; authored row coordinates start at the top.
        var all = texture.GetPixels32();
        var result = new Color32[32 * 32];
        var startX = col * 32;
        var startY = (7 - row) * 32;
        for (var y = 0; y < 32; y++)
            Array.Copy(all, (startY + y) * texture.width + startX, result, y * 32, 32);
        return result;
    }

    private static void PlaceTopDownTile(Texture2D texture, int row, int col, Color32[] pixels)
        => texture.SetPixels32(col * 32, (7 - row) * 32, 32, 32, pixels);

    private static double EdgeDifference(Color32[] leftOrTop, Color32[] rightOrBottom, bool horizontal)
    {
        double total = 0;
        for (var along = 0; along < 32; along++)
        for (var depth = 0; depth < 3; depth++)
        {
            var ia = horizontal ? along * 32 + (31 - depth) : (31 - depth) * 32 + along;
            var ib = horizontal ? along * 32 + depth : depth * 32 + along;
            var a = leftOrTop[ia];
            var b = rightOrBottom[ib];
            var dr = a.r - b.r; var dg = a.g - b.g; var db = a.b - b.b;
            total += dr * dr + dg * dg + db * db;
        }
        return total / 96.0;
    }

    private static double Score(int[] order, double[,] h, double[,] v)
    {
        double score = 0;
        for (var row = 0; row < 8; row++)
        for (var col = 0; col < 8; col++)
        {
            var i = row * 8 + col;
            if (col < 7) score += h[order[i], order[i + 1]];
            if (row < 7) score += v[order[i], order[i + 8]];
        }
        return score;
    }

    private static void Shuffle(int[] values, System.Random rng)
    {
        for (var i = values.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            Swap(values, i, j);
        }
    }

    private static void Swap(int[] values, int a, int b)
        => (values[a], values[b]) = (values[b], values[a]);

    private static void CreatePreview(Texture2D atlas)
    {
        var preview = new Texture2D(192, 192, TextureFormat.RGBA32, false);
        preview.filterMode = FilterMode.Point;
        for (var row = 0; row < 6; row++)
        for (var col = 0; col < 6; col++)
        {
            var sourceRow = row % 8;
            var sourceCol = col % 8;
            var pixels = ExtractTopDownTile(atlas, sourceRow, sourceCol);
            preview.SetPixels32(col * 32, (5 - row) * 32, 32, 32, pixels);
        }
        preview.Apply(false, false);
        Directory.CreateDirectory(Path.GetDirectoryName(PreviewPath));
        File.WriteAllBytes(PreviewPath, preview.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(preview);
    }

    private static void CreateAutotileAssetsAndPopulatePalette()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(AutotilePath)
            .OfType<Sprite>()
            .ToDictionary(ParseCoordinate, s => s);
        if (sprites.Count != 64) throw new InvalidOperationException("Autotile must import as 64 sprites.");

        var tileByCoordinate = new Dictionary<(int row, int col), Tile>();
        foreach (var pair in sprites)
        {
            var path = $"{TileFolder}/Ground_Cracked_Autotile_r{pair.Key.row}_c{pair.Key.col}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.name = $"Ground_Cracked_Autotile_r{pair.Key.row}_c{pair.Key.col}";
            tile.sprite = pair.Value;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            tileByCoordinate[pair.Key] = tile;
        }

        var root = PrefabUtility.LoadPrefabContents(PalettePath);
        try
        {
            var tilemap = root.GetComponentInChildren<Tilemap>(true);
            if (tilemap == null) throw new InvalidOperationException("Ground palette has no Tilemap.");

            // Preserve the existing Seamless block. Clear only the reserved Autotile region below it.
            for (var y = -40; y <= -4; y++)
            for (var x = -1; x <= 6; x++)
                tilemap.SetTile(new Vector3Int(x, y, 0), null);

            var yCursor = -4; // y=-3 is the separator below the Seamless block.
            foreach (var kind in new[] { Kind.Undamaged, Kind.Crack, Kind.Damage, Kind.Repair, Kind.Mixed })
            {
                var entries = Classification[kind];
                for (var i = 0; i < entries.Length; i++)
                {
                    var x = -1 + i % 8;
                    var y = yCursor - i / 8;
                    tilemap.SetTile(new Vector3Int(x, y, 0), tileByCoordinate[entries[i]]);
                }
                yCursor -= (entries.Length + 7) / 8 + 1;
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

    private static (int row, int col) ParseCoordinate(Sprite sprite)
    {
        var name = sprite.name;
        var r = name.LastIndexOf("_r", StringComparison.Ordinal);
        var c = name.LastIndexOf("_c", StringComparison.Ordinal);
        if (r < 0 || c < 0 || c <= r)
            throw new InvalidOperationException("Unexpected Autotile sprite name: " + name);
        return (int.Parse(name.Substring(r + 2, c - r - 2)), int.Parse(name.Substring(c + 2)));
    }
}

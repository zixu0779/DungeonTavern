using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

internal static class FinalizeGroundAssetsAndPalette
{
    private const string Root = "Assets/DungeonTavern/Art/Environment/Ground";
    private const string Seamless = Root + "/Ground_Cracked_Seamless.png";
    private const string SeamlessCandidate = Root + "/Ground_Cracked_Seamless_Candidate.png";
    private const string Autotile = Root + "/Ground_Cracked_Autotile.png";
    private const string AutotileCandidate = Root + "/Ground_Cracked_Autotile_Clarity_Candidate.png";
    private const string SeamlessTiles = "Assets/DungeonTavern/Art/TileAssets/Dungeon_Ground";
    private const string AutotileTiles = SeamlessTiles + "/Autotile";
    private const string Palette = "Assets/DungeonTavern/Art/Palettes/Dungeon_Ground/Dungeon_Ground.prefab";

    private enum Kind { Undamaged, Crack, Damage, Repair, Mixed }

    private static readonly Dictionary<Kind, (int row, int col)[]> AutotileClassification = new()
    {
        [Kind.Undamaged] = new[] { (3,3),(4,1),(6,1),(7,1),(8,6) },
        [Kind.Crack] = new[] { (1,1),(1,2),(1,3),(1,4),(1,5),(1,6),(1,8),(2,3),(2,4),(2,5),(2,7),(5,2),(5,7),(8,8) },
        [Kind.Damage] = new[] { (2,2),(2,8),(3,2),(3,8),(4,2),(6,3),(6,5),(6,6),(7,2),(7,7) },
        [Kind.Repair] = new[] { (4,3),(4,5),(4,6),(5,1),(5,3),(5,4),(5,5),(5,8),(6,2),(6,7),(6,8),(7,3),(7,4),(7,5),(7,6),(7,8),(8,1),(8,2),(8,3),(8,4),(8,5),(8,7) },
        [Kind.Mixed] = new[] { (1,7),(2,1),(2,6),(3,1),(3,4),(3,5),(3,6),(3,7),(4,4),(4,7),(4,8),(5,6),(6,4) }
    };

    [MenuItem("Tools/Dungeon Tavern/Finalize Ground Candidates And Rebuild Palette")]
    private static void Execute()
    {
        RequireFile(SeamlessCandidate);
        RequireFile(AutotileCandidate);
        File.Copy(SeamlessCandidate, Seamless, true);
        File.Copy(AutotileCandidate, Autotile, true);
        AssetDatabase.ImportAsset(Seamless, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(Autotile, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        var seamless = EnsureSeamlessTiles();
        var autotile = EnsureAutotileTiles();
        RebuildPalette(seamless, autotile);

        if (!AssetDatabase.DeleteAsset(SeamlessCandidate)) throw new InvalidOperationException("Could not delete " + SeamlessCandidate);
        if (!AssetDatabase.DeleteAsset(AutotileCandidate)) throw new InvalidOperationException("Could not delete " + AutotileCandidate);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Ground candidates finalized, candidate assets deleted, and Ground palette rebuilt.");
    }

    private static void RequireFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Required candidate is missing", path);
    }

    private static Dictionary<int, Tile> EnsureSeamlessTiles()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(Seamless).OfType<Sprite>()
            .ToDictionary(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)));
        if (sprites.Count != 64) throw new InvalidOperationException("Seamless must contain 64 sprites.");
        var result = new Dictionary<int, Tile>();
        for (var i = 0; i < 64; i++)
        {
            var path = $"{SeamlessTiles}/Ground_Cracked_Seamless_{i}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.name = $"Ground_Cracked_Seamless_{i}";
            tile.sprite = sprites[i]; tile.color = Color.white; tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile); result[i] = tile;
        }
        return result;
    }

    private static Dictionary<(int row, int col), Tile> EnsureAutotileTiles()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(Autotile).OfType<Sprite>().ToDictionary(ParseAutotileCoordinate);
        if (sprites.Count != 64) throw new InvalidOperationException("Autotile must contain 64 sprites.");
        var result = new Dictionary<(int row, int col), Tile>();
        foreach (var pair in sprites)
        {
            var path = $"{AutotileTiles}/Ground_Cracked_Autotile_r{pair.Key.row}_c{pair.Key.col}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.name = Path.GetFileNameWithoutExtension(path);
            tile.sprite = pair.Value; tile.color = Color.white; tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile); result[pair.Key] = tile;
        }
        return result;
    }

    private static void RebuildPalette(Dictionary<int, Tile> seamless, Dictionary<(int row, int col), Tile> autotile)
    {
        var root = PrefabUtility.LoadPrefabContents(Palette);
        try
        {
            var tilemap = root.GetComponentInChildren<Tilemap>(true) ?? throw new InvalidOperationException("Ground palette has no Tilemap.");
            tilemap.ClearAllTiles();

            var y = 0;
            y = PlaceRows(tilemap, seamless, Enumerable.Range(0,30), y); y--;
            y = PlaceSeamlessCracks(tilemap, seamless, y); y--;
            y = PlaceSeamlessDamage(tilemap, seamless, y); y--;
            y = PlaceRows(tilemap, seamless, Enumerable.Range(55,4), y); y--;
            y = PlaceRows(tilemap, seamless, Enumerable.Range(59,5), y); y--;

            foreach (var kind in new[] { Kind.Undamaged, Kind.Crack, Kind.Damage, Kind.Repair, Kind.Mixed })
            {
                var entries = AutotileClassification[kind];
                for (var i = 0; i < entries.Length; i++)
                    tilemap.SetTile(new Vector3Int(i % 8, y - i / 8, 0), autotile[entries[i]]);
                y -= (entries.Length + 7) / 8 + 1;
            }
            tilemap.CompressBounds(); EditorUtility.SetDirty(tilemap);
            PrefabUtility.SaveAsPrefabAsset(root, Palette);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static int PlaceRows(Tilemap map, Dictionary<int, Tile> tiles, IEnumerable<int> indices, int y)
    {
        var values = indices.ToArray();
        for (var i = 0; i < values.Length; i++) map.SetTile(new Vector3Int(i % 8, y - i / 8, 0), tiles[values[i]]);
        return y - (values.Length + 7) / 8;
    }

    private static int PlaceSeamlessCracks(Tilemap map, Dictionary<int, Tile> t, int top)
    {
        // Closed 2x2 loop: ES/SW over NE/NW.
        Put(map,t,33,0,top); Put(map,t,34,1,top); Put(map,t,32,0,top-1); Put(map,t,35,1,top-1);
        // Straight connectors retain their authored spatial relationship.
        Put(map,t,30,3,top); Put(map,t,41,4,top); Put(map,t,31,3,top-2); Put(map,t,42,3,top-3);
        // Multi-way connected cluster around NESW.
        Put(map,t,39,6,top); Put(map,t,36,5,top-1); Put(map,t,40,6,top-1); Put(map,t,38,7,top-1); Put(map,t,37,6,top-2);
        // Alternate partial loop and internal crack variant.
        Put(map,t,44,0,top-3); Put(map,t,45,1,top-3); Put(map,t,43,0,top-4); Put(map,t,46,3,top-5);
        return top - 6;
    }

    private static int PlaceSeamlessDamage(Tilemap map, Dictionary<int, Tile> t, int top)
    {
        // Base giant hole: SE/SW over NE/NW.
        Put(map,t,49,0,top); Put(map,t,50,1,top); Put(map,t,48,0,top-1); Put(map,t,47,1,top-1);
        // Expanded giant hole using the alternate NE/SW quadrants.
        Put(map,t,49,3,top); Put(map,t,52,4,top); Put(map,t,51,3,top-1); Put(map,t,47,4,top-1);
        Put(map,t,53,6,top); Put(map,t,54,7,top);
        return top - 2;
    }

    private static void Put(Tilemap map, Dictionary<int, Tile> tiles, int index, int x, int y)
        => map.SetTile(new Vector3Int(x,y,0), tiles[index]);

    private static (int row, int col) ParseAutotileCoordinate(Sprite sprite)
    {
        var name=sprite.name; var r=name.LastIndexOf("_r",StringComparison.Ordinal); var c=name.LastIndexOf("_c",StringComparison.Ordinal);
        return (int.Parse(name.Substring(r+2,c-r-2)), int.Parse(name.Substring(c+2)));
    }
}

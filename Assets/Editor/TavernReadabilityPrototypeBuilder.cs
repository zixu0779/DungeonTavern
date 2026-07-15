using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TavernReadabilityPrototypeBuilder
{
    private const string SourceScene = "Assets/Scenes/Tavern/Tavern_Main.unity";
    private const string PrototypeScene = "Assets/Scenes/Tavern/Tavern_ReadabilityPrototype.unity";
    private const string WallFolder = "Assets/DungeonTavern/Art/TileAssets/Dungeon_Walls/";
    private const string FurnitureFolder = "Assets/DungeonTavern/Art/TileAssets/Tavern_Furniture/";
    private const string GroundPalettePath = "Assets/DungeonTavern/Art/Palettes/Dungeon_Ground/Dungeon_Ground.prefab";
    private const string CrackGroundTilePath = "Assets/DungeonTavern/Art/TileAssets/Dungeon_Decoration/Decorative_cracks_Decorative_cracks_001.asset";

    private static readonly Vector2[] MainCavern =
    {
        new(5, 4), new(33, 4), new(39, 8), new(38, 20),
        new(34, 25), new(23, 27), new(9, 25), new(4, 19)
    };

    [MenuItem("Tools/Dungeon Tavern/Build Readability Prototype")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, PrototypeScene, true))
            throw new System.InvalidOperationException("Could not create readability prototype scene.");

        GameObject gridObject = GameObject.Find("Tavern_Main/World/Grid");
        if (gridObject == null)
            throw new System.InvalidOperationException("Required scene path Tavern_Main/World/Grid was not found.");

        Grid grid = gridObject.GetComponent<Grid>();
        if (grid == null)
            grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        Transform existing = gridObject.transform.Find("ArtPrototype");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        DisableRenderers("Tavern_Main/World/Grid/Ground");
        DisableRenderers("Tavern_Main/World/Grid/GroundDetails");
        DisableRenderers("Tavern_Main/World/Grid/Walls");
        DisableRenderers("Tavern_Main/World/Grid/FurnitureBlockout");

        GameObject artRoot = new("ArtPrototype");
        artRoot.transform.SetParent(gridObject.transform, false);

        Tilemap ground = CreateTilemap("GroundArt", artRoot.transform, 0, new Color(0.72f, 0.76f, 0.78f, 1));
        Tilemap wallTops = CreateTilemap("WallTopArt", artRoot.transform, 10, new Color(0.60f, 0.58f, 0.56f, 1));
        Tilemap wallFaces = CreateTilemap("WallFaceArt", artRoot.transform, 15, new Color(0.72f, 0.66f, 0.61f, 1));
        Tilemap furniture = CreateTilemap("FurnitureArt", artRoot.transform, 20, new Color(0.78f, 0.72f, 0.68f, 1));

        HashSet<Vector2Int> floorCells = BuildFloor(ground);
        BuildWalls(wallTops, wallFaces, floorCells);
        BuildFurniture(furniture);
        BuildSigns(artRoot.transform);
        ConfigureCamera();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, PrototypeScene);
        Selection.activeGameObject = artRoot;
        Debug.Log($"Dungeon Tavern readability prototype built: {PrototypeScene}");
    }

    [MenuItem("Tools/Dungeon Tavern/Rebuild Crack Ground Palette")]
    public static void RebuildCrackGroundPalette()
    {
        GameObject paletteRoot = PrefabUtility.LoadPrefabContents(GroundPalettePath);
        try
        {
            paletteRoot.name = "Dungeon_Ground";
            Grid grid = paletteRoot.GetComponent<Grid>();
            if (grid == null)
                grid = paletteRoot.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            Tilemap tilemap = paletteRoot.GetComponentInChildren<Tilemap>(true);
            if (tilemap == null)
            {
                GameObject layer = new("CrackedStoneGround");
                layer.transform.SetParent(paletteRoot.transform, false);
                tilemap = layer.AddComponent<Tilemap>();
                layer.AddComponent<TilemapRenderer>();
            }
            tilemap.gameObject.name = "CrackedStoneGround";
            tilemap.ClearAllTiles();
            tilemap.color = Color.white;

            TileBase crackTile = LoadTile(CrackGroundTilePath);
            Matrix4x4 identity = Matrix4x4.identity;
            Matrix4x4 flipX = Matrix4x4.Scale(new Vector3(-1, 1, 1));
            Matrix4x4 flipY = Matrix4x4.Scale(new Vector3(1, -1, 1));
            Matrix4x4 flipXY = Matrix4x4.Scale(new Vector3(-1, -1, 1));
            Matrix4x4[] variants = { identity, flipX, flipY, flipXY };

            // One compact row: base tile plus non-destructive mirrored variants.
            // No deleted Stone Ground assets are restored or referenced.
            for (int x = 0; x < variants.Length; x++)
            {
                Vector3Int cell = new(x, 0, 0);
                tilemap.SetTile(cell, crackTile);
                tilemap.SetTransformMatrix(cell, variants[x]);
            }
            tilemap.CompressBounds();

            PrefabUtility.SaveAsPrefabAsset(paletteRoot, GroundPalettePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Rebuilt crack-stone Ground Palette: {GroundPalettePath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(paletteRoot);
        }
    }

    [MenuItem("Tools/Dungeon Tavern/Repair Active Prototype Ground")]
    public static void RepairActivePrototypeGround()
    {
        GameObject groundObject = GameObject.Find("Tavern_Main/World/Grid/ArtPrototype/GroundArt");
        if (groundObject == null)
            throw new System.InvalidOperationException("Active scene does not contain ArtPrototype/GroundArt.");

        Tilemap ground = groundObject.GetComponent<Tilemap>();
        if (ground == null)
            throw new System.InvalidOperationException("GroundArt does not contain a Tilemap.");

        // Preserve the user's Transform and every other prototype layer.
        ground.ClearAllTiles();
        ground.color = new Color(0.86f, 0.88f, 0.90f, 1);
        BuildFloor(ground);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Repaired active prototype GroundArt without rebuilding furniture or walls.");
    }

    private static Tilemap CreateTilemap(string name, Transform parent, int order, Color tint)
    {
        GameObject go = new(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        Tilemap tilemap = go.GetComponent<Tilemap>();
        tilemap.color = tint;
        TilemapRenderer renderer = go.GetComponent<TilemapRenderer>();
        renderer.mode = TilemapRenderer.Mode.Individual;
        renderer.sortingOrder = order;
        return tilemap;
    }

    private static HashSet<Vector2Int> BuildFloor(Tilemap tilemap)
    {
        TileBase crackTile = LoadTile(CrackGroundTilePath);
        Matrix4x4[] variants =
        {
            Matrix4x4.identity,
            Matrix4x4.Scale(new Vector3(-1, 1, 1)),
            Matrix4x4.Scale(new Vector3(1, -1, 1)),
            Matrix4x4.Scale(new Vector3(-1, -1, 1))
        };
        HashSet<Vector2Int> cells = new();

        for (int y = 3; y <= 30; y++)
        for (int x = 0; x <= 49; x++)
        {
            Vector2 sample = new(x + 0.5f, y + 0.5f);
            bool inside = PointInPolygon(sample, MainCavern)
                          || InRect(sample, 0, 5, 7, 10)
                          || InRect(sample, 37, 20, 50, 31)
                          || InRect(sample, 39, 10, 50, 20);
            if (!inside)
                continue;

            Vector2Int cell = new(x, y);
            cells.Add(cell);
            int variant = Mathf.Abs((x * 17 + y * 31) % variants.Length);
            tilemap.SetTile((Vector3Int)cell, crackTile);
            tilemap.SetTransformMatrix((Vector3Int)cell, variants[variant]);
        }

        tilemap.CompressBounds();
        return cells;
    }

    private static void BuildWalls(Tilemap wallTops, Tilemap wallFaces, HashSet<Vector2Int> floorCells)
    {
        // Coherent adjacency set from the first authored room in TX Tileset Wall:
        // 0/1-2/3 = north edge, 8/10 = west/east sides,
        // 15/16-17/26 = south edge with the player-facing brick wall.
        TileBase northLeft = WallTile(0);
        TileBase[] northStraight = { WallTile(1), WallTile(2) };
        TileBase northRight = WallTile(3);
        TileBase westSide = WallTile(8);
        TileBase eastSide = WallTile(10);
        TileBase southLeft = WallTile(15);
        TileBase[] southFace = { WallTile(16), WallTile(17) };
        TileBase southRight = WallTile(26);

        foreach (Vector2Int cell in floorCells)
        {
            bool north = !floorCells.Contains(cell + Vector2Int.up);
            bool south = !floorCells.Contains(cell + Vector2Int.down);
            bool west = !floorCells.Contains(cell + Vector2Int.left);
            bool east = !floorCells.Contains(cell + Vector2Int.right);
            if (!(north || south || west || east) || (cell.x <= 1 && cell.y >= 6 && cell.y <= 8))
                continue;

            // North edge: use only the matching top-cap family.
            if (north)
            {
                TileBase tile = west ? northLeft : east ? northRight
                    : northStraight[Mathf.Abs(cell.x % northStraight.Length)];
                wallTops.SetTile((Vector3Int)cell, tile);
            }

            // Matching side pieces from the same authored adjacency set.
            if (west)
                wallFaces.SetTile((Vector3Int)cell, westSide);
            if (east)
                wallFaces.SetTile((Vector3Int)cell, eastSide);

            // The visible brick wall belongs on the southern/player-facing edge.
            if (south)
            {
                TileBase tile = west ? southLeft : east ? southRight
                    : southFace[Mathf.Abs(cell.x % southFace.Length)];
                wallFaces.SetTile((Vector3Int)cell, tile);
            }
        }

        // Back-of-house divider, with a two-cell staff opening behind the bar.
        for (int y = 11; y <= 29; y++)
        {
            if (y == 21 || y == 22)
                continue;
            wallFaces.SetTile(new Vector3Int(38, y, 0), eastSide);
        }

        wallTops.CompressBounds();
        wallFaces.CompressBounds();
    }

    private static void BuildFurniture(Tilemap tilemap)
    {
        // Recognizable service counter and back-bar from the CraftPix tavern sheet.
        Place(tilemap, FurnitureTile(2), 29, 20);
        Place(tilemap, FurnitureTile(3), 27, 23);
        Place(tilemap, FurnitureTile(4), 32, 23);

        // Mixed seating: the same table families are interleaved instead of faction-zoned.
        Place(tilemap, FurnitureTile(0), 10, 11);
        Place(tilemap, FurnitureTile(6), 16, 10);
        Place(tilemap, FurnitureTile(13), 11, 17);
        Place(tilemap, FurnitureTile(15), 19, 17);
        Place(tilemap, FurnitureTile(16), 23, 10);
        Place(tilemap, FurnitureTile(20), 27, 16);

        // Kitchen and storage silhouettes only; small prop dressing is intentionally deferred.
        Place(tilemap, FurnitureTile(5), 42, 24);
        Place(tilemap, FurnitureTile(8), 47, 24);
        Place(tilemap, FurnitureTile(3), 42, 16);
        Place(tilemap, FurnitureTile(4), 47, 16);

        // One shared entrance and three visibly sealed future openings.
        Place(tilemap, StructureTile(0), 1, 7);
        Place(tilemap, StructureTile(1), 12, 26);
        Place(tilemap, StructureTile(1), 21, 27);
        Place(tilemap, StructureTile(1), 39, 9);
        tilemap.CompressBounds();
    }

    private static void BuildSigns(Transform parent)
    {
        GameObject signs = new("AreaSigns");
        signs.transform.SetParent(parent, false);
        AddLabel(signs.transform, "ENTRANCE", new Vector3(3, 8.5f, -0.5f), new Color(0.92f, 0.76f, 0.38f));
        AddLabel(signs.transform, "MIXED HALL", new Vector3(17, 21.5f, -0.5f), new Color(0.92f, 0.82f, 0.58f));
        AddLabel(signs.transform, "BAR", new Vector3(29, 24.5f, -0.5f), new Color(0.92f, 0.66f, 0.36f));
        AddLabel(signs.transform, "KITCHEN", new Vector3(43, 29, -0.5f), new Color(0.72f, 0.84f, 0.66f));
        AddLabel(signs.transform, "STORAGE", new Vector3(44, 19, -0.5f), new Color(0.72f, 0.84f, 0.66f));
        AddLabel(signs.transform, "LOCKED: LODGING", new Vector3(12, 28, -0.5f), new Color(0.86f, 0.38f, 0.45f));
        AddLabel(signs.transform, "LOCKED: QUIET ROOMS", new Vector3(21, 29, -0.5f), new Color(0.86f, 0.38f, 0.45f));
        AddLabel(signs.transform, "LOCKED: STAGE", new Vector3(42, 9, -0.5f), new Color(0.86f, 0.38f, 0.45f));
    }

    private static void AddLabel(Transform parent, string text, Vector3 position, Color color)
    {
        GameObject go = new(text);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        TextMesh label = go.AddComponent<TextMesh>();
        label.text = text;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = 0.16f;
        label.fontSize = 48;
        label.color = color;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 100;
    }

    private static void ConfigureCamera()
    {
        GameObject cameraObject = GameObject.Find("Tavern_Main/CameraRig/Main Camera");
        if (cameraObject == null)
            return;

        cameraObject.transform.position = new Vector3(10, 11, -10);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.015f, 0.018f, 0.024f, 1);
    }

    private static void DisableRenderers(string path)
    {
        GameObject root = GameObject.Find(path);
        if (root == null)
            return;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
    }

    private static bool InRect(Vector2 p, float minX, float minY, float maxX, float maxY) =>
        p.x >= minX && p.x < maxX && p.y >= minY && p.y < maxY;

    private static bool PointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            if ((a.y > point.y) != (b.y > point.y)
                && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    private static void Place(Tilemap tilemap, TileBase tile, int x, int y)
    {
        if (tile != null)
            tilemap.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static TileBase WallTile(int index) => LoadTile(
        $"{WallFolder}TX Tileset Wall_TX Tileset Wall_{index}.asset");

    private static TileBase FurnitureTile(int index) => LoadTile(
        $"{FurnitureFolder}Interior_1st_floor_Interior_1st_floor_{index}.asset");

    private static TileBase StructureTile(int index) => LoadTile(index == 0
        ? "Assets/DungeonTavern/Art/TileAssets/Tavern_Structure/TX Struct_TX Struct Gate T1 B.asset"
        : "Assets/DungeonTavern/Art/TileAssets/Tavern_Structure/TX Struct_TX Struct Gate T2 B.asset");

    private static TileBase LoadTile(string path)
    {
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
        if (tile == null)
            Debug.LogWarning($"Prototype tile was not found: {path}");
        return tile;
    }
}

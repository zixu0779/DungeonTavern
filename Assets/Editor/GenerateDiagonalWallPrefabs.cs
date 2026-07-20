using System;
using System.Collections.Generic;
using System.Linq;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEditor;
using UnityEngine;

public static class GenerateDiagonalWallPrefabs
{
    private const string WallPrefabRoot =
        "Assets/DungeonTavern/Prototypes/Rotation25D/WallPrefabs";
    private const string SharedRoot = WallPrefabRoot + "/Shared";
    private const string DiagonalRoot = WallPrefabRoot + "/Diagonal";
    private const string OneToOneRoot = DiagonalRoot + "/1x1";
    private const string TwoToOneRoot = DiagonalRoot + "/2x1";
    private const string FrontAtlas =
        "Assets/DungeonTavern/Art/Environment/Walls/Walls_interior.png";
    private const string TopAtlas =
        "Assets/DungeonTavern/Art/Environment/Walls/Walls_Vertical_Connections.png";

    private static readonly string[] FrontNames =
    {
        "Walls_interior_7",
        "Walls_interior_20",
        "Walls_interior_14",
        "Walls_interior_21",
        "Walls_interior_22",
        "Walls_interior_15",
        "Walls_interior_23",
        "Walls_interior_16",
        "Walls_interior_24",
        "Walls_interior_25"
    };

    private enum SlopeDirection
    {
        Up,
        Down
    }

    [MenuItem("Tools/Dungeon Tavern/Generate 1x1 and 2x1 Diagonal Wall Prefabs")]
    public static void Generate()
    {
        EnsureFolders();

        Dictionary<string, Sprite> fronts = AssetDatabase.LoadAllAssetsAtPath(FrontAtlas)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name, sprite => sprite);
        Sprite top = AssetDatabase.LoadAllAssetsAtPath(TopAtlas)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == "Walls_Vertical_Top");
        if (top == null)
            throw new InvalidOperationException($"Walls_Vertical_Top not found in {TopAtlas}");

        foreach (string name in FrontNames)
        {
            if (!fronts.ContainsKey(name))
                throw new InvalidOperationException($"{name} not found in {FrontAtlas}");
        }

        Material[] materials = LoadSharedMaterials();
        Sprite sizeSource = fronts["Walls_interior_22"];
        float height = sizeSource.bounds.size.y;
        const float depth = 0.25f;

        CreateSlopeFamily(
            familyName: "1x1",
            outputRoot: OneToOneRoot,
            run: 1f,
            rise: 1f,
            height,
            depth,
            fronts,
            top,
            materials);
        CreateSlopeFamily(
            familyName: "2x1",
            outputRoot: TwoToOneRoot,
            run: 1f,
            rise: 0.5f,
            height,
            depth,
            fronts,
            top,
            materials);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "Generated 40 diagonal wall prefabs: " +
            "1x1 and 2x1, Up and Down, ten Walls_interior face variants each.");
    }

    private static void CreateSlopeFamily(
        string familyName,
        string outputRoot,
        float run,
        float rise,
        float height,
        float depth,
        IReadOnlyDictionary<string, Sprite> fronts,
        Sprite top,
        Material[] materials)
    {
        float length = Mathf.Sqrt(run * run + rise * rise);
        float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
        string meshPath = $"{SharedRoot}/DiagonalWall_{familyName}_FiveFaces.asset";
        Mesh mesh = CreateOrUpdateMesh(
            meshPath,
            $"DiagonalWall_{familyName}_FiveIndependentFaces",
            length,
            height,
            depth);

        foreach (SlopeDirection direction in Enum.GetValues(typeof(SlopeDirection)))
        {
            float yaw = direction == SlopeDirection.Up ? -angle : angle;
            foreach (string frontName in FrontNames)
            {
                Sprite front = fronts[frontName];
                string objectName =
                    $"Wall_{familyName}_{direction}_{frontName}";
                GameObject wall = new(objectName);
                try
                {
                    wall.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

                    MeshFilter filter = wall.AddComponent<MeshFilter>();
                    filter.sharedMesh = mesh;

                    MeshRenderer renderer = wall.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = materials;

                    EditableWallFaceSprites faces =
                        wall.AddComponent<EditableWallFaceSprites>();
                    faces.Front = front;
                    faces.Back = front;
                    faces.Left = null;
                    faces.Right = null;
                    faces.Top = top;
                    faces.MirrorBackHorizontally = true;

                    BoxCollider collider = wall.AddComponent<BoxCollider>();
                    collider.center = new Vector3(0f, height * 0.5f, 0f);
                    collider.size = new Vector3(length, height, depth);

                    string prefabPath = $"{outputRoot}/{objectName}.prefab";
                    PrefabUtility.SaveAsPrefabAsset(wall, prefabPath, out bool success);
                    if (!success)
                        throw new InvalidOperationException(
                            $"Could not save diagonal wall prefab: {prefabPath}");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(wall);
                }
            }
        }
    }

    private static Mesh CreateOrUpdateMesh(
        string path,
        string meshName,
        float length,
        float height,
        float depth)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, path);
        }

        float x0 = -length * 0.5f;
        float x1 = length * 0.5f;
        float z0 = -depth * 0.5f;
        float z1 = depth * 0.5f;
        Vector3[] vertices =
        {
            new(x0, 0f, z0), new(x0, height, z0), new(x1, height, z0), new(x1, 0f, z0),
            new(x1, 0f, z1), new(x1, height, z1), new(x0, height, z1), new(x0, 0f, z1),
            new(x0, 0f, z1), new(x0, height, z1), new(x0, height, z0), new(x0, 0f, z0),
            new(x1, 0f, z0), new(x1, height, z0), new(x1, height, z1), new(x1, 0f, z1),
            new(x0, height, z0), new(x0, height, z1), new(x1, height, z1), new(x1, height, z0)
        };

        Vector2[] uv = new Vector2[vertices.Length];
        for (int face = 0; face < 4; face++)
        {
            int offset = face * 4;
            uv[offset] = new Vector2(0f, 0f);
            uv[offset + 1] = new Vector2(0f, 1f);
            uv[offset + 2] = new Vector2(1f, 1f);
            uv[offset + 3] = new Vector2(1f, 0f);
        }

        // Rotate the 4x16 top Sprite so its long axis follows the wall.
        uv[16] = new Vector2(0f, 0f);
        uv[17] = new Vector2(1f, 0f);
        uv[18] = new Vector2(1f, 1f);
        uv[19] = new Vector2(0f, 1f);

        mesh.Clear();
        mesh.name = meshName;
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.subMeshCount = 5;
        for (int face = 0; face < 5; face++)
        {
            int offset = face * 4;
            mesh.SetTriangles(
                new[]
                {
                    offset, offset + 1, offset + 2,
                    offset, offset + 2, offset + 3
                },
                face);
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static Material[] LoadSharedMaterials()
    {
        string[] names = { "Front", "Back", "Left", "Right", "Top" };
        Material[] materials = new Material[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            string path = $"{SharedRoot}/MAT_StraightWall_{names[i]}.mat";
            materials[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (materials[i] == null)
            {
                GenerateStraightWallPrefabs.Generate();
                materials[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
            }

            if (materials[i] == null)
                throw new InvalidOperationException($"Shared wall material not found: {path}");
        }

        return materials;
    }

    private static void EnsureFolders()
    {
        EnsureFolder(WallPrefabRoot, "Diagonal");
        EnsureFolder(DiagonalRoot, "1x1");
        EnsureFolder(DiagonalRoot, "2x1");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }
}

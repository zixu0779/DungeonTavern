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
        DeleteLegacyMiterMeshes();

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
        Mesh squareMesh = CreateOrUpdateMesh(
            meshPath,
            $"DiagonalWall_{familyName}_FiveFaces",
            length,
            height,
            depth,
            startRelativeAngle: 0f,
            endRelativeAngle: 0f);

        foreach (SlopeDirection direction in Enum.GetValues(typeof(SlopeDirection)))
        {
            float yaw = direction == SlopeDirection.Up ? -angle : angle;
            float signedSlopeAngle =
                direction == SlopeDirection.Up ? angle : -angle;
            Mesh[] interfaceMeshes = CreateOrUpdateInterfaceMeshes(
                familyName,
                direction,
                length,
                height,
                depth,
                signedSlopeAngle,
                squareMesh);

            for (int index = 0; index < FrontNames.Length; index++)
            {
                string frontName = FrontNames[index];
                Sprite front = fronts[frontName];
                string objectName =
                    $"Wall_{familyName}_{direction}_{index + 1:00}";
                GameObject wall = new(objectName);
                try
                {
                    wall.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

                    MeshFilter filter = wall.AddComponent<MeshFilter>();
                    filter.sharedMesh = squareMesh;

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

                    EditableWallMiter miter = wall.AddComponent<EditableWallMiter>();
                    miter.Configure(interfaceMeshes);

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
        float depth,
        float startRelativeAngle,
        float endRelativeAngle)
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
        float startOffset =
            depth * 0.5f *
            Mathf.Tan(startRelativeAngle * 0.5f * Mathf.Deg2Rad);
        float endOffset =
            depth * 0.5f *
            Mathf.Tan(endRelativeAngle * 0.5f * Mathf.Deg2Rad);
        float startFrontX = x0 + startOffset;
        float startBackX = x0 - startOffset;
        float endFrontX = x1 + endOffset;
        float endBackX = x1 - endOffset;

        Vector3[] vertices =
        {
            new(startFrontX, 0f, z0), new(startFrontX, height, z0),
            new(endFrontX, height, z0), new(endFrontX, 0f, z0),

            new(endBackX, 0f, z1), new(endBackX, height, z1),
            new(startBackX, height, z1), new(startBackX, 0f, z1),

            new(startBackX, 0f, z1), new(startBackX, height, z1),
            new(startFrontX, height, z0), new(startFrontX, 0f, z0),

            new(endFrontX, 0f, z0), new(endFrontX, height, z0),
            new(endBackX, height, z1), new(endBackX, 0f, z1),

            new(startFrontX, height, z0), new(startBackX, height, z1),
            new(endBackX, height, z1), new(endFrontX, height, z0)
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

    private static Mesh[] CreateOrUpdateInterfaceMeshes(
        string familyName,
        SlopeDirection direction,
        float length,
        float height,
        float depth,
        float selfAngle,
        Mesh squareMesh)
    {
        WallMiterInterface[] interfaces =
            Enum.GetValues(typeof(WallMiterInterface))
                .Cast<WallMiterInterface>()
                .ToArray();
        Mesh[] meshes = new Mesh[interfaces.Length * interfaces.Length];

        foreach (WallMiterInterface start in interfaces)
        {
            foreach (WallMiterInterface end in interfaces)
            {
                int index = (int)start * interfaces.Length + (int)end;
                float startRelativeAngle = RelativeAngle(start, selfAngle);
                float endRelativeAngle = RelativeAngle(end, selfAngle);
                if (Mathf.Approximately(startRelativeAngle, 0f) &&
                    Mathf.Approximately(endRelativeAngle, 0f))
                {
                    meshes[index] = squareMesh;
                    continue;
                }

                string name =
                    $"DiagonalWall_{familyName}_{direction}_Start{start}_End{end}";
                string path = $"{SharedRoot}/{name}.asset";
                meshes[index] = CreateOrUpdateMesh(
                    path,
                    name,
                    length,
                    height,
                    depth,
                    startRelativeAngle,
                    endRelativeAngle);
            }
        }

        return meshes;
    }

    private static float RelativeAngle(
        WallMiterInterface wallInterface,
        float selfAngle)
    {
        if (wallInterface == WallMiterInterface.Square)
            return 0f;

        return NormalizeLineAngle(InterfaceAngle(wallInterface) - selfAngle);
    }

    private static float InterfaceAngle(WallMiterInterface value)
    {
        return value switch
        {
            WallMiterInterface.OneToOneUp => 45f,
            WallMiterInterface.OneToOneDown => -45f,
            WallMiterInterface.TwoToOneUp =>
                Mathf.Atan2(0.5f, 1f) * Mathf.Rad2Deg,
            WallMiterInterface.TwoToOneDown =>
                -Mathf.Atan2(0.5f, 1f) * Mathf.Rad2Deg,
            WallMiterInterface.Straight => 0f,
            WallMiterInterface.Vertical => 90f,
            _ => 0f
        };
    }

    private static float NormalizeLineAngle(float angle)
    {
        angle = Mathf.DeltaAngle(0f, angle);
        if (angle > 90f)
            angle -= 180f;
        else if (angle < -90f)
            angle += 180f;
        return angle;
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

    private static void DeleteLegacyMiterMeshes()
    {
        string[] families = { "1x1", "2x1" };
        string[] directions = { "Up", "Down" };
        string[] suffixes = { "MiterStart", "MiterEnd", "MiterBoth" };

        foreach (string family in families)
        {
            foreach (string direction in directions)
            {
                foreach (string suffix in suffixes)
                {
                    string path =
                        $"{SharedRoot}/DiagonalWall_{family}_{direction}_{suffix}.asset";
                    if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
                        AssetDatabase.DeleteAsset(path);
                }
            }
        }
    }

}

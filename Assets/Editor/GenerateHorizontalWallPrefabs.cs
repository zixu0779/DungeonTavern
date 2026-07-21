using System;
using System.Collections.Generic;
using System.Linq;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEditor;
using UnityEngine;

public static class GenerateHorizontalWallPrefabs
{
    private const string Root =
        "Assets/DungeonTavern/Prototypes/Rotation25D/WallPrefabs";
    private const string SharedRoot = Root + "/Shared";
    private const string HorizontalRoot = Root + "/Horizontal";
    private const string FrontAtlas =
        "Assets/DungeonTavern/Art/Environment/Walls/Walls_interior.png";
    private const string TopAtlas =
        "Assets/DungeonTavern/Art/Environment/Walls/Walls_Vertical_Connections.png";
    private const string MeshPath = SharedRoot + "/HorizontalWall_FiveFaces.asset";

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

    [MenuItem("Tools/Dungeon Tavern/Generate Horizontal Wall Prefabs")]
    public static void Generate()
    {
        const string obsoleteBasicWall =
            "Assets/DungeonTavern/Prototypes/Rotation25D/BasicWall";
        if (AssetDatabase.IsValidFolder(obsoleteBasicWall))
            AssetDatabase.DeleteAsset(obsoleteBasicWall);

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

        Sprite sizeSource = fronts["Walls_interior_22"];
        float width = sizeSource.bounds.size.x;
        float height = sizeSource.bounds.size.y;
        const float depth = 0.25f;

        Mesh mesh = CreateOrUpdateMesh(width, height, depth);
        Mesh[] horizontalInterfaceMeshes = CreateOrUpdateInterfaceMeshes(
            width,
            height,
            depth,
            mesh,
            selfAngle: 0f,
            orientationName: "Horizontal");
        Mesh[] verticalInterfaceMeshes = CreateOrUpdateInterfaceMeshes(
            width,
            height,
            depth,
            mesh,
            selfAngle: 90f,
            orientationName: "Vertical");
        Material[] materials =
        {
            CreateOrLoadFaceMaterial("Front"),
            CreateOrLoadFaceMaterial("Back"),
            CreateOrLoadFaceMaterial("Left"),
            CreateOrLoadFaceMaterial("Right"),
            CreateOrLoadFaceMaterial("Top")
        };

        for (int index = 0; index < FrontNames.Length; index++)
        {
            string frontName = FrontNames[index];
            Sprite front = fronts[frontName];
            string prefabName = $"Wall_Horizontal_{index + 1:00}";
            GameObject wall = new(prefabName);
            try
            {
                MeshFilter filter = wall.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;

                MeshRenderer renderer = wall.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;

                EditableWallFaceSprites faces = wall.AddComponent<EditableWallFaceSprites>();
                faces.Front = front;
                faces.Back = front;
                faces.Left = null;
                faces.Right = null;
                faces.Top = top;
                faces.MirrorBackHorizontally = true;

                EditableHorizontalWallMiter miter =
                    wall.AddComponent<EditableHorizontalWallMiter>();
                miter.Configure(
                    horizontalInterfaceMeshes,
                    verticalInterfaceMeshes);

                BoxCollider collider = wall.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, height * 0.5f, 0f);
                collider.size = new Vector3(width, height, depth);

                string prefabPath = $"{HorizontalRoot}/{prefabName}.prefab";
                PrefabUtility.SaveAsPrefabAsset(wall, prefabPath, out bool success);
                if (!success)
                    throw new InvalidOperationException($"Could not save prefab: {prefabPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wall);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"Generated {FrontNames.Length} horizontal wall prefabs. " +
            "Front uses the selected Walls_interior sprite, Back mirrors Front, " +
            "Top uses Walls_Vertical_Top, and Left/Right are hidden.");
    }

    private static Mesh CreateOrUpdateMesh(float width, float height, float depth)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null)
        {
            mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }

        float x0 = -width * 0.5f;
        float x1 = width * 0.5f;
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

        // The top source is 4x16. Rotate its UVs so 16 pixels span wall width
        // and 4 pixels span the unchanged 0.25-unit wall thickness.
        uv[16] = new Vector2(0f, 0f);
        uv[17] = new Vector2(1f, 0f);
        uv[18] = new Vector2(1f, 1f);
        uv[19] = new Vector2(0f, 1f);

        mesh.Clear();
        mesh.name = "HorizontalWall_FiveFaces";
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
        float width,
        float height,
        float depth,
        Mesh squareMesh,
        float selfAngle,
        string orientationName)
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

                string wallDirectionName =
                    orientationName == "Vertical" ? "VerticalWall" : "HorizontalWall";
                string name =
                    $"{wallDirectionName}_Start{start}_End{end}";
                string path = $"{SharedRoot}/{name}.asset";
                meshes[index] = CreateOrUpdateInterfaceMesh(
                    path,
                    name,
                    width,
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
            WallMiterInterface.TwoToOneUp => Mathf.Atan2(0.5f, 1f) * Mathf.Rad2Deg,
            WallMiterInterface.TwoToOneDown => -Mathf.Atan2(0.5f, 1f) * Mathf.Rad2Deg,
            WallMiterInterface.Horizontal => 0f,
            WallMiterInterface.Vertical => 90f,
            WallMiterInterface.HorizontalReverse => 180f,
            WallMiterInterface.VerticalReverse => -90f,
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

    private static Mesh CreateOrUpdateInterfaceMesh(
        string path,
        string meshName,
        float width,
        float height,
        float depth,
        float startAngle,
        float endAngle)
    {
        float x0 = -width * 0.5f;
        float x1 = width * 0.5f;
        float z0 = -depth * 0.5f;
        float z1 = depth * 0.5f;
        float startOffset =
            depth * 0.5f * Mathf.Tan(startAngle * 0.5f * Mathf.Deg2Rad);
        float endOffset =
            depth * 0.5f * Mathf.Tan(endAngle * 0.5f * Mathf.Deg2Rad);

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

        uv[16] = new Vector2(0f, 0f);
        uv[17] = new Vector2(1f, 0f);
        uv[18] = new Vector2(1f, 1f);
        uv[19] = new Vector2(0f, 1f);

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, path);
        }

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

    private static Material CreateOrLoadFaceMaterial(string faceName)
    {
        string path = $"{SharedRoot}/MAT_HorizontalWall_{faceName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP Unlit shader was not found.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = $"MAT_HorizontalWall_{faceName}";
        material.renderQueue = 2450;
        material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_AlphaClip", 1f);
        material.SetFloat("_Cutoff", 0.5f);
        material.EnableKeyword("_ALPHATEST_ON");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolders()
    {
        EnsureFolder(
            "Assets/DungeonTavern/Prototypes/Rotation25D",
            "WallPrefabs");
        EnsureFolder(Root, "Shared");
        EnsureFolder(Root, "Horizontal");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

}

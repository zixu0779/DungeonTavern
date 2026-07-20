using System;
using System.Collections.Generic;
using System.Linq;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEditor;
using UnityEngine;

public static class GenerateStraightWallPrefabs
{
    private const string Root =
        "Assets/DungeonTavern/Prototypes/Rotation25D/WallPrefabs";
    private const string SharedRoot = Root + "/Shared";
    private const string StraightRoot = Root + "/Straight";
    private const string FrontAtlas =
        "Assets/DungeonTavern/Art/Environment/Walls/Walls_interior.png";
    private const string TopAtlas =
        "Assets/DungeonTavern/Art/Environment/Walls/Walls_Vertical_Connections.png";
    private const string MeshPath = SharedRoot + "/StraightWall_FiveFaces.asset";

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

    [MenuItem("Tools/Dungeon Tavern/Generate Straight Wall Prefabs")]
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
        Material[] materials =
        {
            CreateOrLoadFaceMaterial("Front"),
            CreateOrLoadFaceMaterial("Back"),
            CreateOrLoadFaceMaterial("Left"),
            CreateOrLoadFaceMaterial("Right"),
            CreateOrLoadFaceMaterial("Top")
        };

        foreach (string frontName in FrontNames)
        {
            Sprite front = fronts[frontName];
            GameObject wall = new($"Wall_{frontName}");
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

                BoxCollider collider = wall.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, height * 0.5f, 0f);
                collider.size = new Vector3(width, height, depth);

                string prefabPath = $"{StraightRoot}/Wall_{frontName}.prefab";
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
            $"Generated {FrontNames.Length} straight wall prefabs. " +
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
        mesh.name = "StraightWall_FiveIndependentFaces";
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
        string path = $"{SharedRoot}/MAT_StraightWall_{faceName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP Unlit shader was not found.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = $"MAT_StraightWall_{faceName}";
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
        EnsureFolder(Root, "Straight");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }
}

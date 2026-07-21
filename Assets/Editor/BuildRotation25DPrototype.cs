using System;
using System.Collections.Generic;
using System.Linq;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BuildRotation25DPrototype
{
    private const string FormalScene = "Assets/Scenes/Tavern/Tavern_Main.unity";
    private const string PrototypeScene = "Assets/Scenes/Prototypes/Tavern_25D_RotationPrototype.unity";
    private const string PrototypeRoot = "Assets/DungeonTavern/Prototypes/Rotation25D";
    private const string RendererPath = PrototypeRoot + "/Renderer3D_25D.asset";
    private const string PipelinePath = "Assets/Settings/UniversalRP.asset";
    private const string CharacterAtlas =
        "Assets/DungeonTavern/Art/Characters/Patrons/Animation_LutePlayer.png";

    private sealed class TileRecord
    {
        public Vector3Int Cell;
        public Sprite Sprite;
        public Matrix4x4 Transform;
        public Color Color;
        public string TileName;
    }

    [MenuItem("Tools/Dungeon Tavern/Rebuild 2.5D Prototype from Tavern_Main")]
    public static void Build()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.isDirty && active.path != PrototypeScene)
        {
            throw new InvalidOperationException(
                "Save the active non-prototype scene before rebuilding the 2.5D prototype.");
        }

        EnsureFolder("Assets/Scenes", "Prototypes");
        EnsureFolder("Assets/DungeonTavern", "Prototypes");
        EnsureFolder("Assets/DungeonTavern/Prototypes", "Rotation25D");

        Scene source = EditorSceneManager.OpenScene(FormalScene, OpenSceneMode.Single);
        Tilemap groundTilemap = FindTilemap("GroundTiles");
        List<TileRecord> groundRecords = CaptureTiles(groundTilemap);
        Vector3 playerStart = FindMarkerPosition("PlayerStart", new Vector3(5f, 7f, 0f));

        if (groundRecords.Count == 0)
            throw new InvalidOperationException("Tavern_Main contains no Ground tiles.");

        CleanupOldWallPrototypeAssets();
        int rendererIndex = EnsureUniversalRenderer();

        Scene prototype = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new("Tavern_25D_RotationPrototype");
        GameObject environment = Child(root, "Environment");
        GameObject floorRoot = Child(environment, "Floor_FromTavernMain");
        Child(environment, "Walls_ToBeFilled");
        GameObject characters = Child(root, "Characters");
        GameObject systems = Child(root, "PrototypeSystems");
        GameObject cameraRigObject = Child(root, "CameraRig");

        Camera camera = BuildCamera(cameraRigObject.transform, rendererIndex);
        BuildFloor(floorRoot.transform, groundRecords);
        ValidateFloorClone(floorRoot.transform, groundRecords);
        GameObject player = BuildPlayer(characters.transform, playerStart, camera.transform);

        PrototypeCameraOrbit orbit = cameraRigObject.AddComponent<PrototypeCameraOrbit>();
        orbit.FollowTarget = player.transform;
        cameraRigObject.transform.position = player.transform.position;
        cameraRigObject.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

        PrototypePlayerMover mover = player.AddComponent<PrototypePlayerMover>();
        mover.CameraTransform = camera.transform;

        PrototypeHud hud = systems.AddComponent<PrototypeHud>();
        SerializedObject hudObject = new(hud);
        hudObject.FindProperty("orbit").objectReferenceValue = orbit;
        hudObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject contract = Child(systems, "PrototypeOnly_TavernMainRemainsReadOnly");
        contract.transform.position = new Vector3(0f, -100f, 0f);

        EditorSceneManager.MarkSceneDirty(prototype);
        if (!EditorSceneManager.SaveScene(prototype, PrototypeScene))
            throw new InvalidOperationException($"Could not save prototype scene: {PrototypeScene}");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = floorRoot;

        Debug.Log(
            $"Rebuilt the 2.5D prototype from Tavern_Main with " +
            $"{groundRecords.Count} matching floor tiles and an empty Walls_ToBeFilled root. " +
            $"Scene: {PrototypeScene}");
    }

    private static List<TileRecord> CaptureTiles(Tilemap tilemap)
    {
        List<TileRecord> records = new();
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(cell);
            Sprite sprite = tilemap.GetSprite(cell);
            if (tile == null || sprite == null)
                continue;

            records.Add(new TileRecord
            {
                Cell = cell,
                Sprite = sprite,
                Transform = tilemap.GetTransformMatrix(cell),
                Color = tilemap.GetColor(cell) * tilemap.color,
                TileName = tile.name
            });
        }

        return records
            .OrderBy(record => record.Cell.y)
            .ThenBy(record => record.Cell.x)
            .ToList();
    }

    private static void BuildFloor(Transform parent, IReadOnlyList<TileRecord> records)
    {
        foreach (TileRecord record in records)
        {
            GameObject tile = Child(
                parent.gameObject,
                $"Floor_{record.Cell.x:00}_{record.Cell.y:00}");
            tile.transform.localPosition =
                new Vector3(record.Cell.x + 0.5f, 0f, record.Cell.y + 0.5f);
            tile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            GameObject visual = Child(tile, "Sprite");
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = record.Sprite;
            renderer.color = record.Color;

            ApplyTileTransform(visual.transform, record.Transform);
        }
    }

    private static void ApplyTileTransform(Transform target, Matrix4x4 matrix)
    {
        Vector2 xAxis = new(matrix.m00, matrix.m10);
        Vector2 yAxis = new(matrix.m01, matrix.m11);
        float scaleX = Mathf.Max(0.0001f, xAxis.magnitude);
        float determinant = matrix.m00 * matrix.m11 - matrix.m01 * matrix.m10;
        float scaleY = determinant / scaleX;
        float angle = Mathf.Atan2(xAxis.y, xAxis.x) * Mathf.Rad2Deg;

        target.localRotation = Quaternion.Euler(0f, 0f, angle);
        target.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private static void ValidateFloorClone(
        Transform floorRoot,
        IReadOnlyList<TileRecord> sourceRecords)
    {
        if (floorRoot.childCount != sourceRecords.Count)
        {
            throw new InvalidOperationException(
                $"Floor clone count mismatch: source={sourceRecords.Count}, " +
                $"prototype={floorRoot.childCount}.");
        }

        for (int i = 0; i < sourceRecords.Count; i++)
        {
            TileRecord source = sourceRecords[i];
            Transform tile = floorRoot.GetChild(i);
            Vector3 expectedPosition =
                new(source.Cell.x + 0.5f, 0f, source.Cell.y + 0.5f);
            SpriteRenderer renderer = tile.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null ||
                renderer.sprite != source.Sprite ||
                renderer.color != source.Color ||
                tile.localPosition != expectedPosition ||
                !TileTransformMatches(renderer.transform, source.Transform))
            {
                throw new InvalidOperationException(
                    $"Floor clone mismatch at cell {source.Cell}: {tile.name}");
            }
        }
    }

    private static bool TileTransformMatches(Transform target, Matrix4x4 matrix)
    {
        Vector2 xAxis = new(matrix.m00, matrix.m10);
        Vector2 yAxis = new(matrix.m01, matrix.m11);
        float expectedScaleX = Mathf.Max(0.0001f, xAxis.magnitude);
        float determinant = matrix.m00 * matrix.m11 - matrix.m01 * matrix.m10;
        float expectedScaleY = determinant / expectedScaleX;
        float expectedAngle = Mathf.Atan2(xAxis.y, xAxis.x) * Mathf.Rad2Deg;

        return Mathf.Approximately(target.localScale.x, expectedScaleX) &&
               Mathf.Approximately(target.localScale.y, expectedScaleY) &&
               Mathf.Abs(Mathf.DeltaAngle(target.localEulerAngles.z, expectedAngle)) < 0.01f;
    }

    private static GameObject BuildPlayer(
        Transform parent,
        Vector3 sourcePosition,
        Transform cameraTransform)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(CharacterAtlas)
            .OfType<Sprite>()
            .OrderBy(candidate => candidate.name)
            .FirstOrDefault();
        if (sprite == null)
            throw new InvalidOperationException($"No character Sprite found in {CharacterAtlas}");

        GameObject player = Child(parent.gameObject, "Player_PrototypeMarker");
        player.transform.localPosition =
            new Vector3(sourcePosition.x + 0.5f, 0f, sourcePosition.y + 0.5f);

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.4f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0f, 0.7f, 0f);

        GameObject visual = Child(player, "CharacterSprite");
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 100;
        renderer.rendererPriority = 100;
        visual.transform.localPosition =
            new Vector3(0f, -sprite.bounds.min.y + 0.01f, 0f);
        PrototypeBillboard billboard = visual.AddComponent<PrototypeBillboard>();
        billboard.CameraTransform = cameraTransform;
        return player;
    }

    private static Camera BuildCamera(Transform rig, int rendererIndex)
    {
        GameObject cameraObject = Child(rig.gameObject, "Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.localPosition = new Vector3(0f, 8f, -8f);
        cameraObject.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.032f, 0.04f);
        camera.allowMSAA = false;
        camera.allowHDR = false;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;

        UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.SetRenderer(rendererIndex);

        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<PrototypePixelOutput>();
        return camera;
    }

    private static int EnsureUniversalRenderer()
    {
        UniversalRenderPipelineAsset pipeline =
            QualitySettings.renderPipeline as UniversalRenderPipelineAsset ??
            GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset ??
            AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline == null)
            throw new InvalidOperationException($"URP asset not found: {PipelinePath}");

        // This project already carries a URP asset whose default renderer is Renderer2D,
        // but GraphicsSettings may still point at Built-in. Activating that asset keeps
        // the 2D renderer as renderer 0 while allowing this prototype camera to select
        // the appended 3D renderer explicitly.
        if (GraphicsSettings.defaultRenderPipeline != pipeline)
            GraphicsSettings.defaultRenderPipeline = pipeline;

        UniversalRendererData rendererData =
            AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (rendererData == null)
        {
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.name = "Renderer3D_25D";
            AssetDatabase.CreateAsset(rendererData, RendererPath);
        }

        SerializedObject pipelineObject = new(pipeline);
        SerializedProperty rendererList = pipelineObject.FindProperty("m_RendererDataList");
        if (rendererList == null)
            throw new InvalidOperationException("Could not locate URP m_RendererDataList.");

        for (int i = 0; i < rendererList.arraySize; i++)
        {
            if (rendererList.GetArrayElementAtIndex(i).objectReferenceValue == rendererData)
                return i;
        }

        int index = rendererList.arraySize;
        rendererList.arraySize++;
        rendererList.GetArrayElementAtIndex(index).objectReferenceValue = rendererData;
        pipelineObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();
        return index;
    }

    private static void CleanupOldWallPrototypeAssets()
    {
        const string generated = PrototypeRoot + "/Generated";
        const string cutawayShader =
            PrototypeRoot + "/Shaders/PrototypeWallCutaway.shader";
        const string cutawayController =
            PrototypeRoot + "/Scripts/PrototypeWallCutawayController.cs";
        const string unusedCardinalSprite =
            PrototypeRoot + "/Scripts/PrototypeCardinalSprite.cs";
        const string unusedDepthSort =
            PrototypeRoot + "/Scripts/PrototypeSpriteDepthSort.cs";

        if (AssetDatabase.IsValidFolder(generated))
            AssetDatabase.DeleteAsset(generated);
        AssetDatabase.DeleteAsset(cutawayShader);
        AssetDatabase.DeleteAsset(cutawayController);
        AssetDatabase.DeleteAsset(unusedCardinalSprite);
        AssetDatabase.DeleteAsset(unusedDepthSort);
        if (AssetDatabase.IsValidFolder(PrototypeRoot + "/Shaders"))
            AssetDatabase.DeleteAsset(PrototypeRoot + "/Shaders");

        string[] screenshots =
            AssetDatabase.FindAssets("", new[] { "Assets/Screenshots" });
        foreach (string guid in screenshots)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Rotation25D_", StringComparison.Ordinal))
                AssetDatabase.DeleteAsset(path);
        }
    }

    private static Tilemap FindTilemap(string name)
    {
        Tilemap tilemap = UnityEngine.Object.FindObjectsByType<Tilemap>(
                FindObjectsInactive.Include)
            .FirstOrDefault(candidate => candidate.name == name);
        return tilemap != null
            ? tilemap
            : throw new InvalidOperationException($"Tilemap not found in formal scene: {name}");
    }

    private static Vector3 FindMarkerPosition(string name, Vector3 fallback)
    {
        Transform marker = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include)
            .FirstOrDefault(candidate => candidate.name == name);
        return marker != null ? marker.position : fallback;
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }
}

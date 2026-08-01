using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonTavern.Tavern25D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class B1SceneSetup
{
    private const string TavernScenePath = "Assets/Scenes/Tavern/Tavern_Main.unity";
    private const string B1ScenePath = "Assets/Scenes/SealRoom/SealRoom_B1.unity";
    private const string MaterialFolder = "Assets/DungeonTavern/Tavern25D/Gameplay/Materials";

    [MenuItem("Tools/Dungeon Tavern/Migrate B1 To Separate Scene")]
    public static void Build()
    {
        Scene tavernScene = EditorSceneManager.GetActiveScene();
        if (tavernScene.path != TavernScenePath)
        {
            Debug.LogError($"B1 migration requires {TavernScenePath} to be the active scene.");
            return;
        }

        Stage40SceneSetup.Build();
        Directory.CreateDirectory("Assets/Scenes/SealRoom");

        Scene b1Scene = File.Exists(B1ScenePath)
            ? EditorSceneManager.OpenScene(B1ScenePath, OpenSceneMode.Additive)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        ClearScene(b1Scene);
        BuildB1(b1Scene);
        if (!EditorSceneManager.SaveScene(b1Scene, B1ScenePath))
        {
            Debug.LogError("B1 migration stopped because the separate scene could not be saved.");
            return;
        }

        SceneManager.SetActiveScene(tavernScene);
        RemoveLegacyB1();
        ConfigureInitialEntry();
        ConfigureBuildSettings();
        EditorSceneManager.MarkSceneDirty(tavernScene);
        EditorSceneManager.SaveScene(tavernScene);
        EditorSceneManager.CloseScene(b1Scene, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("B1 migration complete: SealRoom_B1 is an additive scene with fade transitions in both directions.");
    }

    private static void BuildB1(Scene scene)
    {
        GameObject root = new("SealRoom_B1");
        SceneManager.MoveGameObjectToScene(root, scene);
        Transform environment = CreateEmpty("Environment", root.transform);
        Transform gameplay = CreateEmpty("Gameplay", root.transform);
        Transform spawnPoints = CreateEmpty("SpawnPoints", gameplay);
        Transform interactionPoints = CreateEmpty("InteractionPoints", gameplay);

        Material floor = LoadMaterial("MAT_Stage40_Floor");
        Material wall = LoadMaterial("MAT_Stage40_Wall");
        Material stair = LoadMaterial("MAT_Stage40_Stair");
        Material core = LoadMaterial("MAT_Stage40_Core");

        CreateCube("Floor", environment, new Vector3(80f, -0.1f, 20f), new Vector3(12f, 0.2f, 10f), floor);
        CreateCube("Wall_West", environment, new Vector3(74f, 1.5f, 20f), new Vector3(0.3f, 3f, 10f), wall);
        CreateCube("Wall_East", environment, new Vector3(86f, 1.5f, 20f), new Vector3(0.3f, 3f, 10f), wall);
        CreateCube("Wall_South", environment, new Vector3(80f, 1.5f, 15f), new Vector3(12f, 3f, 0.3f), wall);
        CreateCube("Wall_North", environment, new Vector3(80f, 1.5f, 25f), new Vector3(12f, 3f, 0.3f), wall);

        for (int index = 0; index < 5; index++)
        {
            float x = 75.1f + index * 0.42f;
            float y = -0.3f + index * 0.08f;
            CreateCube($"UpStep_{index + 1:00}", environment, new Vector3(x, y, 20f), new Vector3(0.48f, 0.14f, 1.6f), stair);
        }

        CreateCylinder("SealCore_Greybox", environment, new Vector3(82f, 0.65f, 20f), new Vector3(2.4f, 0.65f, 2.4f), core);
        CreateMarker("PlayerSealRoomStart", spawnPoints, new Vector3(81f, 0f, 20f));
        CreateMarker("SealCore", interactionPoints, new Vector3(82f, 0f, 20f));
        CreateMarker("SealRoomStairArrival", root.transform, new Vector3(76.8f, 0f, 20f));

        Transform portal = CreateEmpty("SealRoomToStorage", root.transform);
        portal.position = new Vector3(74.55f, 0.8f, 20f);
        BoxCollider collider = portal.gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(0.8f, 1.6f, 2f);
        Rigidbody body = portal.gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        AdditiveScenePortal scenePortal = portal.gameObject.AddComponent<AdditiveScenePortal>();
        scenePortal.Configure(string.Empty, "SealRoom_B1", new Vector3(40.2f, 0f, 22.5f), new Vector3(0f, -90f, 0f));
    }

    private static void ConfigureInitialEntry()
    {
        GameObject player = GameObject.Find("Tavern_Main/Characters/Player");
        if (player == null)
        {
            Debug.LogError("B1 migration could not find the player.");
            return;
        }

        InitialAdditiveSceneLoader loader = player.GetComponent<InitialAdditiveSceneLoader>();
        if (loader == null)
            loader = Undo.AddComponent<InitialAdditiveSceneLoader>(player);
        loader.Configure("SealRoom_B1", new Vector3(81f, 0f, 20f), new Vector3(0f, -90f, 0f));
    }

    private static void RemoveLegacyB1()
    {
        GameObject legacy = GameObject.Find("Tavern_Main/Environment/Stage40_Foundation/SealRoom_B1_Greybox");
        if (legacy != null)
            Undo.DestroyObjectImmediate(legacy);

        RemoveIfPresent("Gameplay/SpawnPoints/PlayerSealRoomStart");
        RemoveIfPresent("Gameplay/InteractionPoints/SealCore");
    }

    private static void ConfigureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        AddOrEnable(scenes, TavernScenePath);
        AddOrEnable(scenes, B1ScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddOrEnable(List<EditorBuildSettingsScene> scenes, string path)
    {
        int index = scenes.FindIndex(scene => scene.path == path);
        if (index >= 0)
            scenes[index] = new EditorBuildSettingsScene(path, true);
        else
            scenes.Add(new EditorBuildSettingsScene(path, true));
    }

    private static void ClearScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);
    }

    private static void RemoveIfPresent(string path)
    {
        GameObject target = GameObject.Find(path);
        if (target != null)
            Undo.DestroyObjectImmediate(target);
    }

    private static Transform CreateEmpty(string name, Transform parent)
    {
        GameObject gameObject = new(name);
        gameObject.transform.SetParent(parent, false);
        return gameObject.transform;
    }

    private static void CreateMarker(string name, Transform parent, Vector3 position)
    {
        Transform marker = CreateEmpty(name, parent);
        marker.position = position;
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        return CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale, material);
    }

    private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        return CreatePrimitive(name, PrimitiveType.Cylinder, parent, position, scale, material);
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, true);
        gameObject.transform.SetPositionAndRotation(position, Quaternion.identity);
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        return gameObject;
    }

    private static Material LoadMaterial(string name)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
        if (material == null)
            Debug.LogError($"B1 migration could not load material {name}.");
        return material;
    }
}

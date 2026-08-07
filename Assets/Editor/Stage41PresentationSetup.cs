using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using Ink.UnityIntegration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage41PresentationSetup
{
    private const string TavernPath = "Assets/Scenes/Tavern/Tavern_Main.unity";
    private const string B1Path = "Assets/Scenes/SealRoom/SealRoom_B1.unity";
    private const string InkPath = "Assets/DungeonTavern/Tavern25D/Narrative/Chapter01/Chapter01.ink";

    [MenuItem("Tools/Dungeon Tavern/Fix Stage 4.1 Presentation And Scene Ownership")]
    public static void Build()
    {
        Scene tavern = EditorSceneManager.GetActiveScene();
        if (tavern.path != TavernPath)
        {
            Debug.LogError($"Open {TavernPath} before running the Stage 4.1 presentation setup.");
            return;
        }

        GameObject duplicateFloor = Find(tavern, "TavernWalkableFloorCollision");
        if (duplicateFloor != null)
            Object.DestroyImmediate(duplicateFloor);

        GameObject systems = Find(tavern, "Systems");
        GameObject player = Find(tavern, "Player");
        GameObject storagePoint = Find(tavern, "EveDay1Conversation");
        GameObject characters = Find(tavern, "Characters");
        GameObject customerTemplate = Find(tavern, "Customer_Test");
        if (systems == null || player == null || storagePoint == null || characters == null || customerTemplate == null)
        {
            Debug.LogError("Stage 4.1 presentation setup is missing required tavern objects.");
            return;
        }

        Day1EveActor eve = BuildEve(characters.transform, storagePoint.transform, customerTemplate);
        Day1NarrativeController narrative = systems.GetComponent<Day1NarrativeController>();
        BusinessDayController businessDay = systems.GetComponent<BusinessDayController>();
        InkFile ink = AssetDatabase.LoadAssetAtPath<InkFile>(InkPath);
        narrative.Configure(
            ink,
            null,
            Find(tavern, "StorageStairArrival").transform,
            businessDay,
            eve,
            Find(tavern, "EveOpeningSwitchGuide")?.transform);

        InitialAdditiveSceneLoader loader = systems.GetComponent<InitialAdditiveSceneLoader>();
        if (loader == null)
            loader = systems.AddComponent<InitialAdditiveSceneLoader>();
        loader.Configure("SealRoom_B1", new Vector3(81f, 0f, 20f), new Vector3(0f, -90f, 0f));

        PrototypeCameraOrbit orbit = Find(tavern, "CameraRig").GetComponent<PrototypeCameraOrbit>();
        orbit.FollowTarget = null;

        Scene b1 = EditorSceneManager.OpenScene(B1Path, OpenSceneMode.Additive);
        player.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(player, b1);
        player.transform.SetParent(null, true);
        Object.DestroyImmediate(player.GetComponent<InitialAdditiveSceneLoader>());
        if (player.GetComponent<PersistentPlayerRoot>() == null)
            player.AddComponent<PersistentPlayerRoot>();
        player.transform.SetPositionAndRotation(new Vector3(81f, 0f, 20f), Quaternion.Euler(0f, -90f, 0f));

        EditorSceneManager.MarkSceneDirty(tavern);
        EditorSceneManager.MarkSceneDirty(b1);
        EditorSceneManager.SaveScene(b1);
        EditorSceneManager.SaveScene(tavern);
        SceneManager.SetActiveScene(tavern);
        EditorSceneManager.CloseScene(b1, true);
        AssetDatabase.SaveAssets();
        Debug.Log("Stage 4.1 presentation fixed: duplicate floor removed, Player authored in B1, Eve and bubble/cinematic placeholders wired.");
    }

    [MenuItem("Tools/Dungeon Tavern/Fix B1 Persistent Player Root")]
    public static void FixPersistentPlayerRoot()
    {
        Scene tavern = EditorSceneManager.GetActiveScene();
        Scene b1 = EditorSceneManager.OpenScene(B1Path, OpenSceneMode.Additive);
        GameObject player = Find(b1, "Player");
        if (player == null)
        {
            Debug.LogError("B1 player root repair could not find Player.");
            EditorSceneManager.CloseScene(b1, true);
            return;
        }

        player.transform.SetParent(null, true);
        if (player.GetComponent<PersistentPlayerRoot>() == null)
            player.AddComponent<PersistentPlayerRoot>();
        EditorSceneManager.MarkSceneDirty(b1);
        EditorSceneManager.SaveScene(b1);
        SceneManager.SetActiveScene(tavern);
        EditorSceneManager.CloseScene(b1, true);
        Debug.Log("B1 Player is now a scene root, so DontDestroyOnLoad survives B1 unload.");
    }

    private static Day1EveActor BuildEve(Transform parent, Transform destination, GameObject customerTemplate)
    {
        GameObject existing = Find(EditorSceneManager.GetActiveScene(), "Eve_Day1");
        GameObject eveObject = existing ?? new GameObject("Eve_Day1");
        eveObject.transform.SetParent(parent, true);
        eveObject.transform.position = new Vector3(34.5f, 0f, 20f);

        WorldSpeechBubble bubble = eveObject.GetComponent<WorldSpeechBubble>() ?? eveObject.AddComponent<WorldSpeechBubble>();
        Day1EveActor eve = eveObject.GetComponent<Day1EveActor>() ?? eveObject.AddComponent<Day1EveActor>();
        eve.Configure(destination);

        if (eveObject.transform.childCount == 0)
        {
            SpriteRenderer source = customerTemplate.GetComponentInChildren<SpriteRenderer>(true);
            GameObject visual = new("EvePlaceholderSprite");
            visual.transform.SetParent(eveObject.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.color = new Color(0.78f, 0.66f, 1f, 1f);
            visual.AddComponent<PrototypeBillboard>();
        }
        return eve;
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            if (transform.name == name)
                return transform.gameObject;
        return null;
    }
}

using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D.Narrative;
using Ink.UnityIntegration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Stage41Day1Setup
{
    private const string ScenePath = "Assets/Scenes/Tavern/Tavern_Main.unity";
    private const string ChapterPath = "Assets/DungeonTavern/Tavern25D/Narrative/Chapter01/Chapter01.ink";

    [MenuItem("Tools/Dungeon Tavern/Build Stage 4.1 Day 1 Flow")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError($"Stage 4.1 setup requires {ScenePath} to be active.");
            return;
        }

        GameObject systems = Require("Systems");
        GameObject playerObject = Require("Player");
        GameObject storageArrivalObject = Require("StorageStairArrival");
        GameObject businessSwitchObject = Require("BusinessSwitch");
        GameObject settlementObject = Require("BranSettlement");
        if (systems == null
            || playerObject == null
            || storageArrivalObject == null
            || businessSwitchObject == null
            || settlementObject == null)
        {
            return;
        }

        InkFile chapter = AssetDatabase.LoadAssetAtPath<InkFile>(ChapterPath);
        if (chapter == null)
        {
            Debug.LogError($"Stage 4.1 requires an InkFile at {ChapterPath}.");
            return;
        }

        PrototypePlayerMover player = playerObject.GetComponent<PrototypePlayerMover>();
        BusinessDayController businessDay = systems.GetComponent<BusinessDayController>();
        if (player == null || businessDay == null)
        {
            Debug.LogError("Stage 4.1 requires PrototypePlayerMover and BusinessDayController.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Stage 4.1 Day 1 Flow");

        Undo.RecordObject(businessDay, "Configure Day 1 customer schedule");
        businessDay.ConfigureDayOne(settlementObject.transform);

        Day1NarrativeController narrative = GetOrAdd<Day1NarrativeController>(systems);
        Undo.RecordObject(narrative, "Configure Day 1 narrative");
        narrative.Configure(chapter, player, storageArrivalObject.transform, businessDay);

        Day1SwitchPoint businessSwitch = GetOrAdd<Day1SwitchPoint>(businessSwitchObject);
        Undo.RecordObject(businessSwitch, "Configure Day 1 business switch");
        businessSwitch.Configure(narrative);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Stage 4.1 Day 1 flow built: Ink narrative, storage return, business switch, Bran service, settlement, and closing are wired.");
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(gameObject);
    }

    private static GameObject Require(string objectName)
    {
        GameObject gameObject = FindInActiveScene(objectName);
        if (gameObject == null)
            Debug.LogError($"Stage 4.1 setup could not find required object: {objectName}");
        return gameObject;
    }

    private static GameObject FindInActiveScene(string objectName)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                    return candidate.gameObject;
            }
        }

        return null;
    }
}

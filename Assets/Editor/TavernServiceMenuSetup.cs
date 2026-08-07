using System.Collections.Generic;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Tavern25D;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TavernServiceMenuSetup
{
    private const string ScenePath = "Assets/Scenes/Tavern/Tavern_Main.unity";
    private const string MaterialPath = "Assets/DungeonTavern/Tavern25D/Gameplay/Materials/MAT_Bar_Unified.mat";
    private const string PrefabPath = "Assets/DungeonTavern/Tavern25D/Gameplay/Prefabs/Bar_Unified.prefab";

    [MenuItem("Tools/Dungeon Tavern/Build Unified Bar And Menu")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError($"Open {ScenePath} before building the unified bar and menu.");
            return;
        }

        Transform bar = GameObject.Find("Tavern_Main/Environment/Greybox/Bar_Greybox")?.transform;
        Transform triggerRoot = GameObject.Find("Tavern_Main/Environment/Stage40_Foundation/AutomaticDoorTriggers")?.transform;
        Transform interactionPoints = GameObject.Find("Gameplay/InteractionPoints")?.transform;
        Transform customerPoints = GameObject.Find("Gameplay/CustomerPoints")?.transform;
        GameObject systems = GameObject.Find("Systems");
        if (bar == null || triggerRoot == null || interactionPoints == null || customerPoints == null || systems == null)
        {
            Debug.LogError("Unified bar setup is missing its bar, trigger, gameplay, or Systems roots.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Unified Bar And Menu");

        Material material = EnsureMaterial();
        GameObject prefabContents = new("Bar_Greybox");
        prefabContents.transform.position = bar.position;
        BuildBarContents(prefabContents.transform, material);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabContents, PrefabPath);
        Object.DestroyImmediate(prefabContents);
        if (prefab == null)
            throw new System.InvalidOperationException($"Could not save unified bar prefab at {PrefabPath}.");

        Transform barParent = bar.parent;
        int siblingIndex = bar.GetSiblingIndex();
        Object.DestroyImmediate(bar.gameObject);
        GameObject barInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, barParent);
        barInstance.name = "Bar_Greybox";
        barInstance.transform.SetSiblingIndex(siblingIndex);
        bar = barInstance.transform;
        CreateDoorSensor("Bar_ServiceGate_West", bar, triggerRoot, new Vector3(19.38f, 1f, 18.6f), new Vector3(4.4f, 2f, 3.4f));
        CreateDoorSensor("Bar_ServiceGate_East", bar, triggerRoot, new Vector3(34.5f, 1f, 16.86f), new Vector3(3.4f, 2f, 4.4f));

        Transform menuPoint = EnsureEmpty("DrinkPickup", interactionPoints);
        menuPoint.position = new Vector3(25.6f, 0f, 15.35f);
        if (menuPoint.GetComponent<TestDrinkPoint>() == null)
            Undo.AddComponent<TestDrinkPoint>(menuPoint.gameObject);

        Transform menuApproach = EnsureEmpty("MenuApproach", customerPoints);
        menuApproach.position = new Vector3(25.6f, 0f, 14.95f);
        Transform queueRoot = EnsureEmpty("ServiceOrderQueue", customerPoints);
        ServiceOrderQueue queue = queueRoot.GetComponent<ServiceOrderQueue>();
        if (queue == null)
            queue = queueRoot.gameObject.AddComponent<ServiceOrderQueue>();
        Vector3[] positions =
        {
            new(27.2f, 0f, 14.95f), new(28.65f, 0f, 14.95f), new(30.1f, 0f, 14.95f),
            new(31.55f, 0f, 14.95f), new(32.9f, 0f, 14.75f), new(32.9f, 0f, 13.35f),
            new(32.9f, 0f, 11.95f), new(32.9f, 0f, 10.55f)
        };
        List<Transform> queuePoints = new();
        for (int index = 0; index < positions.Length; index++)
        {
            Transform point = EnsureEmpty($"Queue_{index + 1:00}", queueRoot);
            point.position = positions[index];
            queuePoints.Add(point);
        }
        Undo.RecordObject(queue, "Configure service order queue");
        queue.Configure(queuePoints);

        if (systems.GetComponent<TavernMenuSystem>() == null)
            systems.AddComponent<TavernMenuSystem>();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        TavernNavMeshSetup.Build();
        Debug.Log("Unified bar prefab, dual service gates, menu/order board, bent customer queue, and tavern wallet were built.");
    }

    private static void BuildBarContents(Transform bar, Material material)
    {
        CreateCube("Counter_Long_West", bar, new Vector3(26.64f, 0.55f, 16.86f), new Vector3(13.52f, 1.1f, 1f), material);
        CreateCube("Counter_Long_East", bar, new Vector3(36.24f, 0.55f, 16.86f), new Vector3(1.28f, 1.1f, 1f), material);
        CreateCube("Counter_Short_South", bar, new Vector3(19.38f, 0.55f, 16.93f), new Vector3(1f, 1.1f, 1.14f), material);
        CreateCube("Counter_Short_North", bar, new Vector3(19.38f, 0.55f, 21.98f), new Vector3(1f, 1.1f, 3.76f), material);
        CreateGate("Bar_ServiceGate_West", bar, new Vector3(19.38f, 0f, 17.5f), Vector3.forward, material);
        CreateGate("Bar_ServiceGate_East", bar, new Vector3(33.4f, 0f, 16.86f), Vector3.right, material);
    }

    private static void CreateGate(string name, Transform parent, Vector3 hingePosition, Vector3 axis, Material material)
    {
        Transform gate = EnsureEmpty(name, parent);
        gate.SetPositionAndRotation(hingePosition, Quaternion.identity);
        BoxCollider blocker = gate.GetComponent<BoxCollider>();
        if (blocker == null)
            blocker = gate.gameObject.AddComponent<BoxCollider>();
        blocker.center = axis * 1.1f + Vector3.up * 0.55f;
        blocker.size = new Vector3(axis.z == 0f ? 2.2f : 1f, 1.1f, axis.x == 0f ? 2.2f : 1f);

        GameObject leaf = CreateCube("GateLeaf", gate, hingePosition + axis * 1.1f + Vector3.up * 0.55f, blocker.size, material);
        Collider leafCollider = leaf.GetComponent<Collider>();
        if (leafCollider != null)
            Object.DestroyImmediate(leafCollider);

        DoorStateController door = gate.GetComponent<DoorStateController>();
        if (door == null)
            door = gate.gameObject.AddComponent<DoorStateController>();
        door.ConfigureHinged(gate, null, blocker, axis == Vector3.forward ? -90f : 90f, 0f, false);
        NavMeshModifier modifier = gate.GetComponent<NavMeshModifier>();
        if (modifier == null)
            modifier = gate.gameObject.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;
        modifier.applyToChildren = false;

    }

    private static void CreateDoorSensor(string gateName, Transform bar, Transform triggerRoot, Vector3 position, Vector3 size)
    {
        DoorStateController door = bar.Find(gateName)?.GetComponent<DoorStateController>();
        if (door == null)
            throw new System.InvalidOperationException($"Unified bar is missing {gateName}.");
        Transform sensor = EnsureEmpty($"{gateName}_AutoDoorTrigger", triggerRoot);
        sensor.position = position;
        BoxCollider sensorCollider = sensor.GetComponent<BoxCollider>();
        if (sensorCollider == null)
            sensorCollider = sensor.gameObject.AddComponent<BoxCollider>();
        sensorCollider.isTrigger = true;
        sensorCollider.size = size;
        Rigidbody body = sensor.GetComponent<Rigidbody>();
        if (body == null)
            body = sensor.gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        AutomaticDoorTrigger trigger = sensor.GetComponent<AutomaticDoorTrigger>();
        if (trigger == null)
            trigger = sensor.gameObject.AddComponent<AutomaticDoorTrigger>();
        trigger.Configure(door, 0.45f);
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(cube, $"Create {name}");
        cube.name = name;
        cube.transform.SetParent(parent, true);
        cube.transform.SetPositionAndRotation(position, Quaternion.identity);
        cube.transform.localScale = scale;
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        return cube;
    }

    private static Transform EnsureEmpty(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;
        GameObject created = new(name);
        Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
        created.transform.SetParent(parent, false);
        return created.transform;
    }

    private static Material EnsureMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = "MAT_Bar_Unified" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.color = new Color(0.26f, 0.17f, 0.10f, 1f);
        EditorUtility.SetDirty(material);
        return material;
    }
}

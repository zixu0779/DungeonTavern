using System.Collections.Generic;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
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
    private const string MenuPrefabPath = "Assets/DungeonTavern/Tavern25D/Gameplay/Prefabs/TavernMenuBoard.prefab";
    private const string BarrelPrefabPath = "Assets/DungeonTavern/Tavern25D/Gameplay/Prefabs/OakDrinkBarrel.prefab";

    [MenuItem("Tools/Dungeon Tavern/Build Unified Bar And Menu")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play Mode before rebuilding tavern prefabs.");
            return;
        }
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
        Transform obsoleteWestSensor = triggerRoot.Find("Bar_ServiceGate_West_AutoDoorTrigger");
        if (obsoleteWestSensor != null)
            Object.DestroyImmediate(obsoleteWestSensor.gameObject);
        CreateDoorSensor("Bar_ServiceGate_North", bar, triggerRoot, new Vector3(19.38f, 1f, 22.76f), new Vector3(4.4f, 2f, 3.4f));
        CreateDoorSensor("Bar_ServiceGate_East", bar, triggerRoot, new Vector3(35.78f, 1f, 16.86f), new Vector3(3.4f, 2f, 4.4f));

        Transform oldMenuPoint = interactionPoints.Find("DrinkPickup");
        if (oldMenuPoint != null)
            Object.DestroyImmediate(oldMenuPoint.gameObject);
        Transform oldBoard = interactionPoints.Find("WorldMenuBoard");
        if (oldBoard != null)
            Object.DestroyImmediate(oldBoard.gameObject);
        Transform oldBarrel = interactionPoints.Find("OakDrinkBarrel");
        if (oldBarrel != null)
            Object.DestroyImmediate(oldBarrel.gameObject);
        BuildPhysicalMenuBoard(interactionPoints, material);
        BuildDrinkBarrel(interactionPoints);

        Transform menuApproach = EnsureEmpty("MenuApproach", customerPoints);
        menuApproach.position = new Vector3(25.5f, 0f, 15.05f);
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
        CreateCounter("Counter_Long_Main", bar, new Vector3(27.28f, 0.55f, 16.86f), new Vector3(14.8f, 1.1f, 1f), material);
        CreateCounter("Counter_Short_Main", bar, new Vector3(19.38f, 0.55f, 19.51f), new Vector3(1f, 1.1f, 4.3f), material);
        CreateGate("Bar_ServiceGate_North", bar, new Vector3(19.38f, 0f, 21.66f), Vector3.forward, material);
        CreateGate("Bar_ServiceGate_East", bar, new Vector3(34.68f, 0f, 16.86f), Vector3.right, material);
        CreateCube("NorthWallConnector", bar, new Vector3(19.38f, 0.75f, 23.94f), new Vector3(1.16f, 1.5f, 0.16f), material);
        CreateCube("EastWallConnector", bar, new Vector3(36.96f, 0.75f, 16.86f), new Vector3(0.16f, 1.5f, 1.16f), material);
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
        door.ConfigureUpwardHinged(
            gate,
            blocker,
            axis == Vector3.forward ? new Vector3(-95f, 0f, 0f) : new Vector3(0f, 0f, 95f),
            false);
        NavMeshModifier modifier = gate.GetComponent<NavMeshModifier>();
        if (modifier == null)
            modifier = gate.gameObject.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;
        modifier.applyToChildren = false;

    }

    private static GameObject CreateCounter(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject counter = CreateCube(name, parent, position, scale, material);
        if (counter.GetComponent<CounterVaultObstacle>() == null)
            counter.AddComponent<CounterVaultObstacle>();
        return counter;
    }

    private static void BuildPhysicalMenuBoard(Transform parent, Material barMaterial)
    {
        Material brass = EnsureMaterialAt("Assets/DungeonTavern/Tavern25D/Gameplay/Materials/MAT_Menu_Brass.mat", new Color(0.48f, 0.31f, 0.12f));
        GameObject contents = new("TavernMenuBoard");
        CreateLocalCube("Base", contents.transform, new Vector3(0f, 0.08f, 0f), new Vector3(2.45f, 0.16f, 0.62f), barMaterial);
        CreateLocalCube("Post_Left", contents.transform, new Vector3(-1.02f, 0.64f, 0f), new Vector3(0.16f, 1.08f, 0.2f), brass);
        CreateLocalCube("Post_Right", contents.transform, new Vector3(1.02f, 0.64f, 0f), new Vector3(0.16f, 1.08f, 0.2f), brass);
        CreateLocalCube("CarvedPlaque", contents.transform, new Vector3(0f, 0.68f, 0f), new Vector3(2.12f, 1.02f, 0.16f), barMaterial);
        CreateLocalCube("RuneTrim_Top", contents.transform, new Vector3(0f, 1.16f, -0.1f), new Vector3(2.18f, 0.07f, 0.06f), brass);
        CreateLocalCube("RuneTrim_Bottom", contents.transform, new Vector3(0f, 0.2f, -0.1f), new Vector3(2.18f, 0.07f, 0.06f), brass);
        CreateLocalCylinder("PriceMedallion", contents.transform, new Vector3(0f, 0.72f, -0.18f), new Vector3(0.44f, 0.035f, 0.44f), new Vector3(90f, 0f, 0f), brass, false);

        Transform labelObject = EnsureEmpty("PriceLabel", contents.transform);
        labelObject.localPosition = new Vector3(0f, 0.72f, -0.235f);
        TextMesh label = labelObject.GetComponent<TextMesh>();
        if (label == null) label = labelObject.gameObject.AddComponent<TextMesh>();
        label.text = "8 G";
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 64;
        label.characterSize = 0.035f;
        label.color = new Color(1f, 0.88f, 0.58f);
        CreateLocalCube("TokenRail", contents.transform, new Vector3(0f, 0.12f, -0.27f), new Vector3(1.55f, 0.08f, 0.18f), brass);
        for (int index = 0; index < 3; index++)
            CreateLocalCylinder($"OrderToken_{index + 1}", contents.transform, new Vector3(-0.4f + index * 0.4f, 0.2f, -0.3f), new Vector3(0.13f, 0.025f, 0.13f), new Vector3(90f, 0f, 0f), brass, false);
        contents.AddComponent<TavernMenuPoint>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(contents, MenuPrefabPath);
        Object.DestroyImmediate(contents);
        if (prefab == null) throw new System.InvalidOperationException("Could not save tavern menu prefab.");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = "TavernMenuBoard";
        instance.transform.SetPositionAndRotation(new Vector3(25.5f, 1.14f, 16.35f), Quaternion.identity);
    }

    private static void BuildDrinkBarrel(Transform parent)
    {
        Material wood = EnsureMaterialAt("Assets/DungeonTavern/Tavern25D/Gameplay/Materials/MAT_OakBarrel_Wood.mat", new Color(0.31f, 0.16f, 0.065f));
        Material iron = EnsureMaterialAt("Assets/DungeonTavern/Tavern25D/Gameplay/Materials/MAT_OakBarrel_Iron.mat", new Color(0.095f, 0.105f, 0.12f));
        Material brass = EnsureMaterialAt("Assets/DungeonTavern/Tavern25D/Gameplay/Materials/MAT_OakBarrel_Brass.mat", new Color(0.5f, 0.32f, 0.1f));
        GameObject contents = new("OakDrinkBarrel");
        CreateLocalCylinder("OakBody", contents.transform, new Vector3(0f, 1.2f, 0f), new Vector3(1.05f, 1.34f, 1.05f), new Vector3(0f, 0f, 90f), wood, true);
        CreateLocalCylinder("EndCap_Left", contents.transform, new Vector3(-1.33f, 1.2f, 0f), new Vector3(1.08f, 0.07f, 1.08f), new Vector3(0f, 0f, 90f), wood, false);
        CreateLocalCylinder("EndCap_Right", contents.transform, new Vector3(1.33f, 1.2f, 0f), new Vector3(1.08f, 0.07f, 1.08f), new Vector3(0f, 0f, 90f), wood, false);
        foreach (float x in new[] { -1.05f, -0.38f, 0.38f, 1.05f })
            CreateLocalCylinder($"IronHoop_{x:0.00}", contents.transform, new Vector3(x, 1.2f, 0f), new Vector3(1.1f, 0.055f, 1.1f), new Vector3(0f, 0f, 90f), iron, false);
        CreateLocalCube("Cradle_Left", contents.transform, new Vector3(-0.85f, 0.28f, 0f), new Vector3(0.5f, 0.55f, 1.35f), iron);
        CreateLocalCube("Cradle_Right", contents.transform, new Vector3(0.85f, 0.28f, 0f), new Vector3(0.5f, 0.55f, 1.35f), iron);
        CreateLocalCube("TapStem", contents.transform, new Vector3(0f, 1.0f, -1.18f), new Vector3(0.18f, 0.18f, 0.5f), brass);
        CreateLocalCube("TapHandle", contents.transform, new Vector3(0f, 1.23f, -1.38f), new Vector3(0.12f, 0.48f, 0.12f), brass);
        CreateLocalCube("CupShelf", contents.transform, new Vector3(1.65f, 0.62f, -0.55f), new Vector3(0.85f, 0.12f, 0.8f), wood);
        for (int index = 0; index < 3; index++)
            CreateLocalCylinder($"WoodenCup_{index + 1}", contents.transform, new Vector3(1.4f + index * 0.25f, 0.8f, -0.55f), new Vector3(0.15f, 0.2f, 0.15f), Vector3.zero, wood, false);
        Transform point = EnsureEmpty("BarrelDrinkPickup", contents.transform);
        point.localPosition = new Vector3(0f, 0f, -1.65f);
        point.gameObject.AddComponent<DrinkBarrelPoint>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(contents, BarrelPrefabPath);
        Object.DestroyImmediate(contents);
        if (prefab == null) throw new System.InvalidOperationException("Could not save oak barrel prefab.");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = "OakDrinkBarrel";
        instance.transform.SetPositionAndRotation(new Vector3(31.5f, 0f, 24f), Quaternion.identity);
    }

    private static GameObject CreateLocalCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        return cube;
    }

    private static GameObject CreateLocalCylinder(string name, Transform parent, Vector3 position, Vector3 scale,
        Vector3 rotation, Material material, bool keepCollider)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = position;
        cylinder.transform.localEulerAngles = rotation;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!keepCollider) Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        return cylinder;
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

    private static Material EnsureMaterialAt(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }
}

using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Tavern25D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Stage40SceneSetup
{
    private const string ScenePath = "Assets/Scenes/Tavern/Tavern_Main.unity";
    private const string MaterialFolder = "Assets/DungeonTavern/Tavern25D/Gameplay/Materials";

    [MenuItem("Tools/Dungeon Tavern/Build Stage 4.0 Foundation")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError($"Stage 4.0 setup requires {ScenePath} to be the active scene.");
            return;
        }

        GameObject tavern = Require("Tavern_Main");
        GameObject gameplay = Require("Gameplay");
        GameObject environment = Require("Tavern_Main/Environment");
        GameObject doors = Require("Tavern_Main/Environment/Walls/Doors");
        GameObject bar = Require("Tavern_Main/Environment/Greybox/Bar_Greybox");
        if (tavern == null || gameplay == null || environment == null || doors == null || bar == null)
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Stage 4.0 Foundation");

        Material floorMaterial = EnsureMaterial("MAT_Stage40_Floor", new Color(0.18f, 0.2f, 0.24f));
        Material wallMaterial = EnsureMaterial("MAT_Stage40_Wall", new Color(0.29f, 0.31f, 0.36f));
        Material stairMaterial = EnsureMaterial("MAT_Stage40_Stair", new Color(0.26f, 0.17f, 0.1f));
        Material coreMaterial = EnsureMaterial("MAT_Stage40_Core", new Color(0.15f, 0.28f, 0.34f));
        Material walkablePreviewMaterial = EnsureWalkablePreviewMaterial();

        Transform stageRoot = EnsureEmpty("Stage40_Foundation", environment.transform);
        Transform automaticDoorRoot = EnsureEmpty("AutomaticDoorTriggers", stageRoot);
        Transform storageAccessRoot = EnsureEmpty("StorageSealAccess", stageRoot);

        ConfigureNamedDoor(
            "Tavern_Main/Environment/Walls/Doors/Door_Small_Stone",
            "Kitchen_AutoDoorTrigger",
            new Vector3(37f, 1f, 14f),
            new Vector3(4f, 2f, 3f),
            automaticDoorRoot);
        ConfigureNamedDoor(
            "Tavern_Main/Environment/Walls/Doors/Door_Small_Stone_2",
            "Storage_AutoDoorTrigger",
            new Vector3(37f, 1f, 20f),
            new Vector3(4f, 2f, 3f),
            automaticDoorRoot);

        BuildBarGate(bar.transform, automaticDoorRoot, stairMaterial);
        BuildTavernWalkableFloor(stageRoot, storageAccessRoot, walkablePreviewMaterial);
        BuildStorageStairs(storageAccessRoot, stairMaterial);
        ConfigureStorageScenePortal(stageRoot);
        BuildGameplayContract(gameplay.transform);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Stage 4.0 foundation built: automatic doors, bar gate, storage stairs, B1 scene portal, and Day 1 markers.");
    }

    private static void BuildBarGate(Transform bar, Transform triggerRoot, Material material)
    {
        Transform oldShort = bar.Find("Counter_Short");
        if (oldShort != null)
        {
            Undo.RecordObject(oldShort.gameObject, "Disable solid short counter");
            oldShort.gameObject.SetActive(false);
        }

        CreateCube("Counter_Short_South", bar, new Vector3(19.38f, 0.55f, 16.93f), new Vector3(1f, 1.1f, 1.14f), material);
        CreateCube("Counter_Short_North", bar, new Vector3(19.38f, 0.55f, 21.78f), new Vector3(1f, 1.1f, 4.16f), material);

        Transform gate = EnsureEmpty("Bar_ServiceGate", bar);
        gate.position = new Vector3(19.38f, 0f, 17.9f);
        BoxCollider blocker = GetOrAdd<BoxCollider>(gate.gameObject);
        blocker.center = new Vector3(0f, 0.55f, 1.1f);
        blocker.size = new Vector3(1f, 1.1f, 2.2f);

        GameObject leaf = CreateCube("GateLeaf", gate, new Vector3(19.38f, 0.55f, 19f), new Vector3(1f, 1.1f, 2.2f), material);
        Collider leafCollider = leaf.GetComponent<Collider>();
        if (leafCollider != null)
            Undo.DestroyObjectImmediate(leafCollider);

        DoorStateController door = GetOrAdd<DoorStateController>(gate.gameObject);
        door.ConfigureHinged(gate, null, blocker, -90f, 0f, false);
        CreateDoorSensor("BarGate_AutoDoorTrigger", new Vector3(19.38f, 1f, 19f), new Vector3(4.4f, 2f, 3.4f), triggerRoot, door);
    }

    private static void BuildStorageStairs(Transform root, Material material)
    {
        for (int index = 0; index < 5; index++)
        {
            float x = 40.8f + index * 0.42f;
            float y = 0.02f - index * 0.08f;
            CreateCube($"StorageDownStep_{index + 1:00}", root, new Vector3(x, y, 22.5f), new Vector3(0.48f, 0.14f, 1.6f), material);
        }
    }

    private static void BuildTavernWalkableFloor(
        Transform stageRoot,
        Transform storageAccessRoot,
        Material previewMaterial)
    {
        Transform legacyFloor = storageAccessRoot.Find("StorageSafetyFloor");
        Transform floor;
        if (legacyFloor != null)
        {
            Undo.RecordObject(legacyFloor.gameObject, "Expand storage floor collider");
            legacyFloor.name = "TavernWalkableFloorCollider";
            legacyFloor.SetParent(stageRoot, true);
            floor = legacyFloor;
        }
        else
        {
            floor = EnsureEmpty("TavernWalkableFloorCollider", stageRoot);
        }

        floor.position = new Vector3(20.5f, -0.15f, 16f);
        GameObject preview = CreateCube(
            "WalkableFloorPreview",
            floor,
            floor.position,
            Vector3.one,
            previewMaterial);
        preview.tag = "EditorOnly";
        Collider previewCollider = preview.GetComponent<Collider>();
        if (previewCollider != null)
            Undo.DestroyObjectImmediate(previewCollider);

        WalkableFloorArea area = GetOrAdd<WalkableFloorArea>(floor.gameObject);
        area.Initialize(new Vector3(49f, 0.3f, 32f), preview.GetComponent<MeshRenderer>());
    }

    private static void ConfigureStorageScenePortal(Transform stageRoot)
    {
        Transform storageArrival = EnsureEmpty("StorageStairArrival", stageRoot);
        storageArrival.SetPositionAndRotation(new Vector3(40.2f, 0f, 22.5f), Quaternion.Euler(0f, -90f, 0f));

        Transform portal = EnsureEmpty("StorageToSealRoom", stageRoot);
        portal.position = new Vector3(42.75f, 0.8f, 22.5f);
        BoxCollider collider = GetOrAdd<BoxCollider>(portal.gameObject);
        collider.isTrigger = true;
        collider.size = new Vector3(0.8f, 1.6f, 2f);
        Rigidbody body = GetOrAdd<Rigidbody>(portal.gameObject);
        body.isKinematic = true;
        body.useGravity = false;
        StairPortalTrigger oldPortal = portal.GetComponent<StairPortalTrigger>();
        if (oldPortal != null)
            Undo.DestroyObjectImmediate(oldPortal);
        AdditiveScenePortal scenePortal = GetOrAdd<AdditiveScenePortal>(portal.gameObject);
        scenePortal.Configure("SealRoom_B1", string.Empty, new Vector3(76.8f, 0f, 20f), new Vector3(0f, 90f, 0f));
    }

    private static void BuildGameplayContract(Transform gameplay)
    {
        Transform spawnPoints = Require("Gameplay/SpawnPoints").transform;
        Transform interactionPoints = Require("Gameplay/InteractionPoints").transform;
        Transform customerPoints = Require("Gameplay/CustomerPoints").transform;
        Transform narrativePoints = EnsureEmpty("NarrativePoints", gameplay);

        Transform drink = interactionPoints.Find("BarInteraction");
        if (drink != null)
        {
            Undo.RecordObject(drink.gameObject, "Rename drink pickup point");
            drink.name = "DrinkPickup";
        }

        Transform seat = customerPoints.Find("Seat_01");
        if (seat != null)
        {
            Undo.RecordObject(seat.gameObject, "Name Bran Day 1 seat");
            seat.name = "BranDay1Seat";
        }

        SetMarker("StorageStairEntry", spawnPoints, new Vector3(41.2f, 0f, 22.5f));
        SetMarker("BusinessSwitch", interactionPoints, new Vector3(20.5f, 0f, 16.8f));
        SetMarker("EveDay1Conversation", narrativePoints, new Vector3(38.8f, 0f, 20f));
        SetMarker("BranSettlement", narrativePoints, new Vector3(20.2f, 0f, 9.5f));
        SetMarker("Day1Closing", narrativePoints, new Vector3(20.5f, 0f, 16.8f));

        GameObject player = Require("Tavern_Main/Characters/Player");
        if (player != null)
        {
            Undo.RecordObject(player.transform, "Place player at Day 1 start");
            player.transform.SetPositionAndRotation(new Vector3(40.2f, 0f, 22.5f), Quaternion.Euler(0f, -90f, 0f));
        }
    }

    private static void ConfigureNamedDoor(string path, string triggerName, Vector3 position, Vector3 size, Transform root)
    {
        GameObject doorObject = Require(path);
        if (doorObject == null)
            return;
        DoorStateController door = doorObject.GetComponent<DoorStateController>();
        ConfigureHingeJamb(doorObject.transform);
        CreateDoorSensor(triggerName, position, size, root, door);
    }

    private static void ConfigureHingeJamb(Transform doorRoot)
    {
        Transform hinge = doorRoot.Find("DoorHinge");
        if (hinge == null)
            return;

        Transform jamb = EnsureEmpty("HingeJambCollider", doorRoot);
        jamb.SetPositionAndRotation(hinge.position - doorRoot.right * 0.3f, doorRoot.rotation);
        BoxCollider collider = GetOrAdd<BoxCollider>(jamb.gameObject);
        collider.center = new Vector3(0f, 1f, 0f);
        collider.size = new Vector3(0.7f, 2f, 1.2f);
        collider.isTrigger = false;
    }

    private static void CreateDoorSensor(string name, Vector3 position, Vector3 size, Transform parent, DoorStateController door)
    {
        Transform sensor = EnsureEmpty(name, parent);
        sensor.position = position;
        BoxCollider collider = GetOrAdd<BoxCollider>(sensor.gameObject);
        collider.isTrigger = true;
        collider.size = size;
        Rigidbody body = GetOrAdd<Rigidbody>(sensor.gameObject);
        body.isKinematic = true;
        body.useGravity = false;
        AutomaticDoorTrigger trigger = GetOrAdd<AutomaticDoorTrigger>(sensor.gameObject);
        trigger.Configure(door, 0.45f);
    }

    private static void CreatePortal(string name, Vector3 position, Vector3 size, Transform parent, Transform destination)
    {
        Transform portal = EnsureEmpty(name, parent);
        portal.position = position;
        BoxCollider collider = GetOrAdd<BoxCollider>(portal.gameObject);
        collider.isTrigger = true;
        collider.size = size;
        Rigidbody body = GetOrAdd<Rigidbody>(portal.gameObject);
        body.isKinematic = true;
        body.useGravity = false;
        StairPortalTrigger trigger = GetOrAdd<StairPortalTrigger>(portal.gameObject);
        trigger.Configure(destination);
    }

    private static void SetMarker(string name, Transform parent, Vector3 position)
    {
        Transform marker = EnsureEmpty(name, parent);
        marker.position = position;
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        return CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale, material);
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        Transform existing = parent.Find(name);
        GameObject gameObject = existing != null ? existing.gameObject : GameObject.CreatePrimitive(type);
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
            gameObject.name = name;
            gameObject.transform.SetParent(parent, true);
        }
        Undo.RecordObject(gameObject.transform, $"Configure {name}");
        gameObject.transform.position = position;
        gameObject.transform.rotation = Quaternion.identity;
        gameObject.transform.localScale = scale;
        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        return gameObject;
    }

    private static Transform EnsureEmpty(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;
        GameObject gameObject = new(name);
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
        gameObject.transform.SetParent(parent, false);
        return gameObject.transform;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(gameObject);
    }

    private static GameObject Require(string path)
    {
        GameObject gameObject = GameObject.Find(path);
        if (gameObject == null)
            Debug.LogError($"Stage 4.0 setup could not find required object: {path}");
        return gameObject;
    }

    private static Material EnsureMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/DungeonTavern/Tavern25D/Gameplay"))
                AssetDatabase.CreateFolder("Assets/DungeonTavern/Tavern25D", "Gameplay");
            AssetDatabase.CreateFolder("Assets/DungeonTavern/Tavern25D/Gameplay", "Materials");
        }

        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        material = new Material(shader) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material EnsureWalkablePreviewMaterial()
    {
        const string name = "MAT_WalkableFloorPreview";
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", new Color(0.1f, 0.95f, 0.35f, 0.3f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.SetShaderPassEnabled("ShadowCaster", false);
        EditorUtility.SetDirty(material);
        return material;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildRotation25DPrototype
{
    private const string PrototypeRoot = "Assets/DungeonTavern/Prototypes/Rotation25D";
    private const string PrototypeScene = "Assets/Scenes/Prototypes/Tavern_25D_RotationPrototype.unity";
    private const string WoodAtlas =
        "Assets/DungeonTavern/Art/Environment/Ground/Ground_AgedWood_Seamless.png";
    private const string WallAtlas =
        "Assets/DungeonTavern/Art/Environment/Walls/Walls_interior.png";
    private const string FurnitureAtlas =
        "Assets/DungeonTavern/Art/Environment/Furniture/Interior_1st_floor.png";
    private const string CharacterAtlas =
        "Assets/DungeonTavern/Art/Characters/Patrons/Animation_LutePlayer.png";

    [MenuItem("Tools/Dungeon Tavern/Build 2.5D Rotation Prototype")]
    public static void Build()
    {
        Scene previousScene = SceneManager.GetActiveScene();
        string previousPath = previousScene.path;
        if (previousScene.isDirty)
            throw new InvalidOperationException(
                "The active scene has unsaved changes. Save it before building the isolated prototype.");

        EnsureFolder("Assets/Scenes", "Prototypes");
        EnsureFolder("Assets/DungeonTavern", "Prototypes");
        EnsureFolder("Assets/DungeonTavern/Prototypes", "Rotation25D");

        Sprite[] woodSprites = LoadSprites(WoodAtlas)
            .Where(sprite => Approximately(sprite.rect.width, 32f) && Approximately(sprite.rect.height, 32f))
            .ToArray();
        Sprite[] wallSprites = LoadSprites(WallAtlas)
            .Where(sprite => Approximately(sprite.rect.width, 16f) && sprite.rect.height >= 45f)
            .ToArray();
        Sprite[] furnitureSprites = LoadSprites(FurnitureAtlas)
            .Where(sprite => sprite.rect.width >= 20f && sprite.rect.height >= 14f)
            .ToArray();
        Sprite[] characterSprites = LoadSprites(CharacterAtlas);

        if (woodSprites.Length == 0)
            throw new InvalidOperationException($"No 32x32 floor sprites found in {WoodAtlas}.");
        if (wallSprites.Length == 0)
            throw new InvalidOperationException($"No 16x45+ wall sprites found in {WallAtlas}.");
        if (furnitureSprites.Length == 0)
            throw new InvalidOperationException($"No usable furniture sprites found in {FurnitureAtlas}.");
        if (characterSprites.Length == 0)
            throw new InvalidOperationException($"No character sprites found in {CharacterAtlas}.");

        Scene prototype = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new("Tavern_25D_RotationPrototype");
        GameObject environment = Child(root, "Environment");
        GameObject floorRoot = Child(environment, "PixelFloor_XZ");
        GameObject wallRoot = Child(environment, "SpriteWalls_3D");
        GameObject furnitureRoot = Child(environment, "FurnitureBlockout_3D");
        GameObject characters = Child(root, "Characters");
        GameObject cameraRigObject = Child(root, "CameraRig");
        GameObject systems = Child(root, "PrototypeSystems");

        Camera camera = BuildCamera(cameraRigObject.transform);
        BuildFloor(floorRoot.transform, woodSprites, camera.transform);
        BuildWalls(wallRoot.transform, wallSprites, camera.transform);
        BuildFurniture(furnitureRoot.transform, furnitureSprites, camera.transform);
        GameObject player = BuildPlayer(characters.transform, characterSprites[0], camera.transform);

        PrototypeCameraOrbit orbit = cameraRigObject.AddComponent<PrototypeCameraOrbit>();
        orbit.FollowTarget = player.transform;

        PrototypePlayerMover mover = player.AddComponent<PrototypePlayerMover>();
        mover.CameraTransform = camera.transform;

        PrototypeHud hud = systems.AddComponent<PrototypeHud>();
        SerializedObject hudObject = new(hud);
        hudObject.FindProperty("orbit").objectReferenceValue = orbit;
        hudObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject label = Child(systems, "PrototypeOnly_DO_NOT_MERGE");
        label.transform.position = new Vector3(0f, -100f, 0f);

        EditorSceneManager.MarkSceneDirty(prototype);
        if (!EditorSceneManager.SaveScene(prototype, PrototypeScene))
            throw new InvalidOperationException($"Could not save prototype scene at {PrototypeScene}.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(previousPath))
            EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);

        Debug.Log(
            $"Built isolated 2.5D rotation prototype at {PrototypeScene}. " +
            "Open it and press Play: WASD moves, Q/E rotates the camera by 90 degrees.");
    }

    private static void BuildFloor(
        Transform parent,
        IReadOnlyList<Sprite> sprites,
        Transform cameraTransform)
    {
        const int width = 10;
        const int depth = 8;
        for (int z = 0; z < depth; z++)
        for (int x = 0; x < width; x++)
        {
            GameObject tile = Child(parent.gameObject, $"Floor_{x:00}_{z:00}");
            tile.transform.localPosition = new Vector3(x - (width - 1) * 0.5f, 0f, z - (depth - 1) * 0.5f);
            tile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites[(x * 3 + z * 5) % sprites.Count];
            AddDepthSort(tile, cameraTransform, 0);
        }
    }

    private static void BuildWalls(
        Transform parent,
        IReadOnlyList<Sprite> sprites,
        Transform cameraTransform)
    {
        const int width = 10;
        const int depth = 8;
        const float halfWidth = width * 0.5f;
        const float halfDepth = depth * 0.5f;

        int wallIndex = 0;
        for (int x = 0; x < width; x++)
        {
            float worldX = x - (width - 1) * 0.5f;
            BuildWallPanel(parent, new Vector3(worldX, 0f, halfDepth), 180f, sprites[wallIndex++ % sprites.Count], cameraTransform);

            bool isDoor = x >= 4 && x <= 6;
            if (!isDoor)
                BuildWallPanel(parent, new Vector3(worldX, 0f, -halfDepth), 0f, sprites[wallIndex++ % sprites.Count], cameraTransform);
        }

        for (int z = 0; z < depth; z++)
        {
            float worldZ = z - (depth - 1) * 0.5f;
            BuildWallPanel(parent, new Vector3(-halfWidth, 0f, worldZ), 90f, sprites[wallIndex++ % sprites.Count], cameraTransform);
            BuildWallPanel(parent, new Vector3(halfWidth, 0f, worldZ), -90f, sprites[wallIndex++ % sprites.Count], cameraTransform);
        }

        CreateCollisionBox(parent, "Entrance_LeftPillar", new Vector3(-1.75f, 1.25f, -halfDepth), new Vector3(0.35f, 2.5f, 0.35f));
        CreateCollisionBox(parent, "Entrance_RightPillar", new Vector3(1.75f, 1.25f, -halfDepth), new Vector3(0.35f, 2.5f, 0.35f));
    }

    private static void BuildWallPanel(
        Transform parent,
        Vector3 position,
        float yaw,
        Sprite sprite,
        Transform cameraTransform)
    {
        GameObject panel = Child(parent.gameObject, $"Wall_{position.x:0.0}_{position.z:0.0}");
        panel.transform.localPosition = position;
        panel.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        CreateCollisionBox(panel.transform, "Collision", new Vector3(0f, 1.35f, 0.08f), new Vector3(1f, 2.7f, 0.18f));

        GameObject face = Child(panel, "PixelFace");
        SpriteRenderer renderer = face.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        face.transform.localPosition = new Vector3(0f, -sprite.bounds.min.y, -0.02f);
        AddDepthSort(face, cameraTransform, 20);
    }

    private static void BuildFurniture(
        Transform parent,
        IReadOnlyList<Sprite> sprites,
        Transform cameraTransform)
    {
        CreateCardinalFurniture(parent, "BarCounter", new Vector3(2.7f, 0f, 1.8f), sprites[2 % sprites.Count], cameraTransform, 35);
        CreateCardinalFurniture(parent, "BackBar", new Vector3(3.8f, 0f, 2.9f), sprites[5 % sprites.Count], cameraTransform, 34);
        CreateCardinalFurniture(parent, "MixedTable_A", new Vector3(-2.4f, 0f, 0.8f), sprites[0], cameraTransform, 30);
        CreateCardinalFurniture(parent, "MixedTable_B", new Vector3(-0.2f, 0f, 2.1f), sprites[1 % sprites.Count], cameraTransform, 30);
        CreateCardinalFurniture(parent, "LargeCreatureTable", new Vector3(-2.5f, 0f, -1.8f), sprites[6 % sprites.Count], cameraTransform, 30, 1.25f);
    }

    private static void CreateCardinalFurniture(
        Transform parent,
        string name,
        Vector3 position,
        Sprite sprite,
        Transform cameraTransform,
        int sortBias,
        float scale = 1f)
    {
        GameObject item = Child(parent.gameObject, name);
        item.transform.localPosition = position;
        item.transform.localScale = Vector3.one * scale;

        GameObject northSouth = Child(item, "NorthSouthFace");
        SpriteRenderer northSouthRenderer = northSouth.AddComponent<SpriteRenderer>();
        northSouthRenderer.sprite = sprite;
        northSouth.transform.localPosition = new Vector3(0f, -sprite.bounds.min.y, 0f);
        AddDepthSort(northSouth, cameraTransform, sortBias);

        GameObject eastWest = Child(item, "EastWestFace");
        SpriteRenderer eastWestRenderer = eastWest.AddComponent<SpriteRenderer>();
        eastWestRenderer.sprite = sprite;
        eastWest.transform.localPosition = new Vector3(0f, -sprite.bounds.min.y, 0f);
        eastWest.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        AddDepthSort(eastWest, cameraTransform, sortBias);

        PrototypeCardinalSprite cardinalSprite = item.AddComponent<PrototypeCardinalSprite>();
        cardinalSprite.Configure(cameraTransform, northSouthRenderer, eastWestRenderer);
    }

    private static GameObject BuildPlayer(
        Transform parent,
        Sprite sprite,
        Transform cameraTransform)
    {
        GameObject player = Child(parent.gameObject, "Player_PrototypeMarker");
        player.transform.localPosition = new Vector3(0f, 0f, -2.2f);

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.4f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0f, 0.7f, 0f);

        GameObject visual = Child(player, "CharacterSprite");
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        visual.transform.localPosition = new Vector3(0f, -sprite.bounds.min.y, 0f);
        PrototypeBillboard billboard = visual.AddComponent<PrototypeBillboard>();
        billboard.CameraTransform = cameraTransform;
        AddDepthSort(visual, cameraTransform, 50);
        return player;
    }

    private static Camera BuildCamera(Transform rig)
    {
        rig.position = new Vector3(0f, 0f, -2.2f);

        GameObject cameraObject = Child(rig.gameObject, "Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.localPosition = new Vector3(0f, 8f, -8f);
        cameraObject.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f);
        camera.allowMSAA = false;
        camera.allowHDR = false;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 60f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<PrototypePixelOutput>();
        return camera;
    }

    private static void CreateCollisionBox(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 size)
    {
        GameObject box = Child(parent.gameObject, name);
        box.transform.localPosition = localPosition;
        BoxCollider collider = box.AddComponent<BoxCollider>();
        collider.size = size;
    }

    private static void AddDepthSort(GameObject target, Transform cameraTransform, int bias)
    {
        PrototypeSpriteDepthSort sort = target.AddComponent<PrototypeSpriteDepthSort>();
        sort.CameraTransform = cameraTransform;
        sort.Bias = bias;
    }

    private static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => SpriteNumber(sprite.name))
            .ToArray();
    }

    private static int SpriteNumber(string name)
    {
        int underscore = name.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(name[(underscore + 1)..], out int number)
            ? number
            : int.MaxValue;
    }

    private static bool Approximately(float a, float b) => Mathf.Abs(a - b) < 0.01f;

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

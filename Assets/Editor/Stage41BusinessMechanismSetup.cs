using DungeonTavern.Tavern25D.Narrative;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Stage41BusinessMechanismSetup
{
    private const string TavernPath = "Assets/Scenes/Tavern/Tavern_Main.unity";
    private const string MaterialPath = "Assets/DungeonTavern/Tavern25D/Gameplay/Materials/MAT_BusinessRope.mat";

    [MenuItem("Tools/Dungeon Tavern/Build Day 1 Business Rope")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().path != TavernPath)
        {
            Debug.LogError($"Open {TavernPath} before building the business rope.");
            return;
        }

        GameObject switchObject = GameObject.Find("BusinessSwitch");
        GameObject narrativePoints = GameObject.Find("Gameplay/NarrativePoints");
        GameObject entranceDoor = GameObject.Find("Tavern_Main/Environment/Walls/Doors/Door_Large_Double");
        if (switchObject == null || narrativePoints == null || entranceDoor == null)
        {
            Debug.LogError("Business rope setup is missing BusinessSwitch, NarrativePoints, or the public entrance door.");
            return;
        }

        switchObject.transform.position = new Vector3(35.5f, 0f, 22f);
        Transform guide = EnsureEmpty("EveOpeningSwitchGuide", narrativePoints.transform);
        guide.position = new Vector3(34.5f, 0f, 22f);

        Material ropeMaterial = EnsureMaterial();
        Transform ropeRoot = EnsureEmpty("BusinessRopeVisual", switchObject.transform);
        ropeRoot.localPosition = Vector3.zero;
        GameObject rope = EnsurePrimitive("Rope", PrimitiveType.Cylinder, ropeRoot);
        rope.transform.localPosition = new Vector3(0f, 1.25f, -0.18f);
        rope.transform.localScale = new Vector3(0.045f, 0.55f, 0.045f);
        rope.GetComponent<Renderer>().sharedMaterial = ropeMaterial;
        Object.DestroyImmediate(rope.GetComponent<Collider>());

        GameObject handle = EnsurePrimitive("PullHandle", PrimitiveType.Cube, ropeRoot);
        handle.transform.localPosition = new Vector3(0f, 0.67f, -0.18f);
        handle.transform.localScale = new Vector3(0.34f, 0.09f, 0.09f);
        handle.GetComponent<Renderer>().sharedMaterial = ropeMaterial;
        Object.DestroyImmediate(handle.GetComponent<Collider>());

        Transform signRoot = EnsureEmpty("BusinessStatusSign", entranceDoor.transform);
        signRoot.localPosition = new Vector3(0f, 2.15f, 0f);
        GameObject openSign = EnsureTextSign("OPEN", signRoot, new Color(0.55f, 0.9f, 0.55f));
        GameObject closedSign = EnsureTextSign("CLOSED", signRoot, new Color(0.9f, 0.55f, 0.45f));

        BusinessRopeMechanism mechanism = switchObject.GetComponent<BusinessRopeMechanism>();
        if (mechanism == null)
            mechanism = Undo.AddComponent<BusinessRopeMechanism>(switchObject);
        mechanism.Configure(ropeRoot, openSign, closedSign);

        Day1SwitchPoint switchPoint = switchObject.GetComponent<Day1SwitchPoint>();
        if (switchPoint == null)
            switchPoint = Undo.AddComponent<Day1SwitchPoint>(switchObject);

        Day1NarrativeController narrative = Object.FindAnyObjectByType<Day1NarrativeController>(FindObjectsInactive.Include);
        if (narrative != null)
            narrative.SetOpeningGuidePoint(guide);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Day 1 business rope placed at (35.5, 0, 22); Eve guide placed at (34.5, 0, 22).");
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

    private static GameObject EnsurePrimitive(string name, PrimitiveType type, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;
        GameObject created = GameObject.CreatePrimitive(type);
        Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
        created.name = name;
        created.transform.SetParent(parent, false);
        return created;
    }

    private static GameObject EnsureTextSign(string text, Transform parent, Color color)
    {
        Transform existing = parent.Find($"Sign_{text}");
        GameObject sign = existing != null ? existing.gameObject : new GameObject($"Sign_{text}");
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(sign, $"Create {text} sign");
            sign.transform.SetParent(parent, false);
        }
        sign.transform.localPosition = Vector3.zero;
        sign.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        TextMesh label = sign.GetComponent<TextMesh>();
        if (label == null)
            label = sign.AddComponent<TextMesh>();
        label.text = text;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 64;
        label.characterSize = 0.09f;
        label.color = color;
        MeshRenderer renderer = sign.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 120;
        return sign;
    }

    private static Material EnsureMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null)
            return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader) { color = new Color(0.2f, 0.11f, 0.055f) };
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }
}

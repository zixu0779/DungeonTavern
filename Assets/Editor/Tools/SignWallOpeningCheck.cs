using System;
using System.IO;
using System.Linq;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class SignWallOpeningCheck
{
    [MenuItem("Tools/Dungeon Tavern/Sign Wall/Check Opening And State")]
    static void Check()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Tavern/Tavern_Main.unity");
        var objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var sign = objects.Single(t => t.name == "TavernSign");
        var walls = new[] { "Wall_Vertical_04_14", "Wall_Vertical_05_14" }
            .Select(n => objects.Single(t => t.name == n)).ToArray();
        var colliders = walls.Select(t => t.GetComponent<MeshCollider>()).ToArray();
        if (colliders.Any(c => !c || c.sharedMesh != c.GetComponent<MeshFilter>().sharedMesh))
            throw new Exception("Wall render and collision meshes differ.");
        if (objects.Any(t => t.name == "BusinessStatusSign" || t.name == "Sign_OPEN" || t.name == "Sign_CLOSED"))
            throw new Exception("Placeholder sign remains.");
        var mechanism = objects.Single(t => t.name == "BusinessSwitch").GetComponent<BusinessRopeMechanism>();
        if (new SerializedObject(mechanism).FindProperty("statusSign").objectReferenceValue != sign.GetComponent<TwoStateProp>())
            throw new Exception("Business switch is not linked to the model.");
        // Sample the fitted opening in both directions, then check solid wall above and below it.
        Physics.SyncTransforms();
        for (int x = 0; x < 9; x++)
        for (int y = 0; y < 7; y++)
        foreach (int direction in new[] { -1, 1 })
        {
            var origin = sign.TransformPoint(new Vector3(Mathf.Lerp(-.86f, .858f, x / 8f), Mathf.Lerp(.06f, 1.144f, y / 6f), direction * 2));
            var ray = new Ray(origin, -direction * sign.forward);
            if (colliders.Any(c => c.Raycast(ray, out _, 4))) throw new Exception("Opening is blocked by a wall collider.");
        }
        foreach (var wall in walls)
        foreach (float height in new[] { 1.48f, 2.73f })
            if (!wall.GetComponent<MeshCollider>().Raycast(new Ray(wall.TransformPoint(new Vector3(0, height, 1)), -wall.forward), out _, 2))
                throw new Exception("Solid wall lost collision.");
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var clone = UnityEngine.Object.Instantiate(sign.gameObject);
            SceneManager.MoveGameObjectToScene(clone, preview);
            var control = new GameObject("State check");
            SceneManager.MoveGameObjectToScene(control, preview);
            var testMechanism = control.AddComponent<BusinessRopeMechanism>();
            var prop = clone.GetComponent<TwoStateProp>();
            // Preview scenes do not run MonoBehaviour lifecycle; initialize the same OnEnable used in Play Mode.
            typeof(TwoStateProp).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(prop, null);
            testMechanism.Configure(null, prop);
            foreach (bool open in new[] { true, false })
            {
                testMechanism.PullAndSetOpen(open);
                if (prop.IsOpen != open || clone.GetComponent<Animator>().GetBool("Open") != open)
                    throw new Exception("Model does not receive business state.");
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        const string report = "ArtSource/Previews/SignWallOpening/check.txt";
        File.WriteAllText(report, "PASS: 126 aperture rays clear; solid wall still collides; visual and collision meshes match; old labels absent; business switch sets model Open true/false.\n");
        Debug.Log("Sign wall opening and business sign checks passed.");
    }
}

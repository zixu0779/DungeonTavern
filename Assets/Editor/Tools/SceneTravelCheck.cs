using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run after the opening while the player is still in B1. Changes are Play-mode only.
internal static class SceneTravelCheck
{
    private static readonly Type Runner = typeof(AdditiveScenePortal).Assembly.GetType("DungeonTavern.Tavern25D.SceneTransitionRunner");
    private static readonly MethodInfo Resolve = Runner.GetMethod("FindDestination", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo Travel = Runner.GetMethod("Transition", BindingFlags.Static | BindingFlags.Public);
    private static readonly FieldInfo Busy = Runner.GetField("transitioning", BindingFlags.Static | BindingFlags.NonPublic);

    [MenuItem("Tools/Dungeon Tavern/Check Scene Travel (Play Mode)")]
    private static void Run()
    {
        if (!Application.isPlaying || !SceneManager.GetSceneByName("SealRoom_B1").isLoaded)
            throw new InvalidOperationException("Start Tavern_Main and finish the opening in B1 first.");
        var player = UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();
        var playerRenderer = player.GetComponentInChildren<Renderer>();
        if (playerRenderer == null || !playerRenderer.enabled || !playerRenderer.gameObject.activeInHierarchy)
            throw new InvalidOperationException("The B1 player renderer is inactive.");
        player.StartCoroutine(Check(player));
    }

    private static IEnumerator Check(PrototypePlayerMover player)
    {
        var portals = UnityEngine.Object.FindObjectsByType<AdditiveScenePortal>(FindObjectsInactive.Include);
        var loader = UnityEngine.Object.FindAnyObjectByType<InitialAdditiveSceneLoader>();
        var loaderData = new SerializedObject(loader);
        var rootsProperty = loaderData.FindProperty("hostContentRoots");
        var hostRoots = Enumerable.Range(0, rootsProperty.arraySize)
            .Select(index => (GameObject)rootsProperty.GetArrayElementAtIndex(index).objectReferenceValue)
            .ToArray();
        Require(hostRoots.Length > 0 && hostRoots.All(root => root != null && !root.activeSelf),
            "Host content was not hidden for B1.");
        var outward = new SerializedObject(portals.Single(p => p.gameObject.scene.name == "SealRoom_B1"));
        var inward = new SerializedObject(portals.Single(p => p.gameObject.scene.name == "Tavern_Main"));
        string host = outward.FindProperty("sceneToLoad").stringValue;
        string hostPath = outward.FindProperty("destinationPath").stringValue;
        string content = inward.FindProperty("sceneToLoad").stringValue;
        string contentPath = inward.FindProperty("destinationPath").stringValue;
        Transform marker = Find(host, hostPath);
        Require(marker != null, "Host arrival is missing.");
        Require(Find(host, hostPath + "/missing") == null, "Missing paths must not resolve.");
        Vector3 originalPosition = marker.position;
        Quaternion originalRotation = marker.rotation;
        bool moverEnabled = player.enabled;
        Vector3 playerPosition = player.transform.position;
        Quaternion playerRotation = player.transform.rotation;
        player.enabled = false;
        try
        {
            // A changed Transform must take effect without updating either portal.
            marker.SetPositionAndRotation(originalPosition + Vector3.right * 0.3f,
                originalRotation * Quaternion.Euler(0f, 17f, 0f));
            yield return TravelAndCheck(player, host, content, hostPath);
            Require(hostRoots.All(root => root.activeSelf), "Host content was not shown in the tavern.");
            Require(!SceneManager.GetSceneByName(content).isLoaded, "B1 was not unloaded.");
            marker.SetPositionAndRotation(originalPosition, originalRotation);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return TravelAndCheck(player, content, "", contentPath);
            Require(hostRoots.All(root => !root.activeSelf), "Host content was not hidden after returning to B1.");
            Require(UnityEngine.Object.FindObjectsByType<PrototypePlayerMover>(FindObjectsInactive.Include).Length == 1,
                "Returning to B1 created a duplicate player.");
            Debug.Log("SceneTravelCheck PASS: moved/rotated arrival, B1 unload/reload, round trip, missing path and single persistent player.");
        }
        finally
        {
            if (marker != null) marker.SetPositionAndRotation(originalPosition, originalRotation);
            if (player != null)
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                bool controllerEnabled = controller.enabled;
                controller.enabled = false;
                player.transform.SetPositionAndRotation(playerPosition, playerRotation);
                Physics.SyncTransforms();
                controller.enabled = controllerEnabled;
                player.enabled = moverEnabled;
            }
        }
    }

    private static IEnumerator TravelAndCheck(PrototypePlayerMover player, string load, string unload, string path)
    {
        Travel.Invoke(null, new object[] { player, load, unload, path, 0.05f });
        float timeout = Time.realtimeSinceStartup + 15f;
        while ((bool)Busy.GetValue(null) && Time.realtimeSinceStartup < timeout)
            yield return null;
        Require(!(bool)Busy.GetValue(null), "Transition timed out.");
        Transform marker = Find(load, path);
        Require(marker != null && Vector3.Distance(player.transform.position, marker.position) < 0.01f,
            "Player did not reach the current arrival position.");
        Require(Quaternion.Angle(player.transform.rotation, marker.rotation) < 0.1f,
            "Player did not use the current arrival rotation.");
        Require(player.GetComponent<CharacterController>().enabled, "Player controller was left disabled.");
    }

    private static Transform Find(string scene, string path) =>
        (Transform)Resolve.Invoke(null, new object[] { SceneManager.GetSceneByName(scene), path });

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

using DungeonTavern.Tavern25D;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class TavernNavMeshSetup
{
    private const string TavernPath = "Assets/Scenes/Tavern/Tavern_Main.unity";

    [MenuItem("Tools/Dungeon Tavern/Build Tavern NPC Navigation")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != TavernPath)
        {
            Debug.LogError($"Open {TavernPath} before building tavern navigation.");
            return;
        }

        GameObject environment = GameObject.Find("Tavern_Main/Environment");
        if (environment == null)
        {
            Debug.LogError("Tavern navigation setup could not find Tavern_Main/Environment.");
            return;
        }

        NavMeshSurface surface = environment.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = Undo.AddComponent<NavMeshSurface>(environment);
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;

        foreach (DoorStateController door in Object.FindObjectsByType<DoorStateController>(FindObjectsInactive.Include))
        {
            Collider blocker = door.BlockingCollider;
            if (blocker == null)
                continue;
            NavMeshModifier modifier = blocker.GetComponent<NavMeshModifier>();
            if (modifier == null)
                modifier = Undo.AddComponent<NavMeshModifier>(blocker.gameObject);

            // The blocker often lives on the door root. Applying the modifier to
            // children would also remove the solid frame and hinge jamb from the bake,
            // producing a false shortcut through the wall corner.
            modifier.ignoreFromBuild = true;
            modifier.applyToChildren = false;

            Transform outline = blocker.transform.Find("OuterWallOutline");
            if (outline != null)
            {
                NavMeshModifier outlineModifier = outline.GetComponent<NavMeshModifier>();
                if (outlineModifier == null)
                    outlineModifier = Undo.AddComponent<NavMeshModifier>(outline.gameObject);
                outlineModifier.ignoreFromBuild = true;
                outlineModifier.applyToChildren = false;
            }
        }

        surface.RemoveData();
        surface.BuildNavMesh();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Tavern NPC NavMesh rebuilt with door frames preserved as navigation obstacles.");
    }
}

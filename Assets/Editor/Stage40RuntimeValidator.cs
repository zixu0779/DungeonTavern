using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using UnityEditor;
using UnityEngine;

public static class Stage40RuntimeValidator
{
    [MenuItem("Tools/Dungeon Tavern/Validate Stage 4.0 Runtime")]
    public static void Begin()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("Stage 4.0 runtime validation requires Play Mode.");
            return;
        }

        PrototypePlayerMover player = Object.FindAnyObjectByType<PrototypePlayerMover>();
        if (player == null)
        {
            Debug.LogError("Stage 4.0 runtime validation requires the player.");
            return;
        }

        Stage40RuntimeProbe previous = player.GetComponent<Stage40RuntimeProbe>();
        if (previous != null)
            Object.Destroy(previous);
        player.gameObject.AddComponent<Stage40RuntimeProbe>();
        Debug.Log("Stage 4.0 runtime validation started.");
    }
}

using DungeonTavern.Tavern25D.Narrative;
using UnityEditor;
using UnityEngine;

public static class Stage41RuntimeValidator
{
    [MenuItem("Tools/Dungeon Tavern/Validate Stage 4.1 Runtime")]
    public static void Begin()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("Stage 4.1 runtime validation requires Play Mode.");
            return;
        }

        Day1NarrativeController narrative = Object.FindAnyObjectByType<Day1NarrativeController>();
        if (narrative == null)
        {
            Debug.LogError("Stage 4.1 runtime validation requires Day1NarrativeController.");
            return;
        }

        Stage41RuntimeProbe previous = narrative.GetComponent<Stage41RuntimeProbe>();
        if (previous != null)
            Object.Destroy(previous);
        narrative.gameObject.AddComponent<Stage41RuntimeProbe>();
        Debug.Log("Stage 4.1 runtime validation started.");
    }
}

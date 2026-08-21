using DungeonTavern.Tavern25D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal static class ScenePortalGizmos
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawPortal(AdditiveScenePortal portal, GizmoType gizmoType)
    {
        var box = portal.GetComponent<BoxCollider>();
        if (box == null) return;

        var previousMatrix = Gizmos.matrix;
        var previousColor = Gizmos.color;
        Gizmos.matrix = box.transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 0.85f, 1f, 0.2f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;

        // Draw through scenery so a black wall or fog cannot hide the trigger outline.
        using (new Handles.DrawingScope(Color.cyan, box.transform.localToWorldMatrix))
        {
            var previousDepth = Handles.zTest;
            Handles.zTest = CompareFunction.Always;
            Handles.DrawWireCube(box.center, box.size);
            Handles.Label(box.center + Vector3.up * (box.size.y * 0.5f + 0.15f),
                box.enabled ? "Teleport trigger" : "Teleport trigger (disabled)");
            Handles.zTest = previousDepth;
        }
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class RebuildTavernLayoutGuide
{
    private const string MenuPath = "Tools/Dungeon Tavern/Clear Terrain And Draw Layout Guide";

    private static readonly Color Outer = new(0.92f, 0.80f, 0.30f, 1f);
    private static readonly Color Public = new(0.30f, 0.82f, 0.95f, 1f);
    private static readonly Color Staff = new(0.95f, 0.48f, 0.30f, 1f);
    private static readonly Color Locked = new(0.74f, 0.42f, 0.95f, 1f);

    [MenuItem(MenuPath)]
    public static void Execute()
    {
        var grid = GameObject.Find("Tavern_Main/World/Grid")?.transform;
        if (grid == null) throw new System.InvalidOperationException("Tavern_Main/World/Grid was not found.");

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Clear Tavern Terrain And Draw Layout Guide");

        ClearChildren(grid.Find("Ground"));
        ClearChildren(grid.Find("GroundDetails"));
        ClearChildren(grid.Find("Walls"));
        ClearChildren(grid.Find("FurnitureBlockout"));
        ClearChildren(grid.Find("ArtPrototype"));

        EnsureTilemap(grid.Find("Ground"), "GroundTiles", 0);
        EnsureTilemap(grid.Find("GroundDetails"), "GroundDetailTiles", 10);
        EnsureTilemap(grid.Find("Walls"), "WallTiles", 20);
        EnsureTilemap(grid.Find("FurnitureBlockout"), "FurnitureTiles", 30);

        var oldGuide = grid.Find("LayoutGuide");
        if (oldGuide != null) Undo.DestroyObjectImmediate(oldGuide.gameObject);

        var guide = new GameObject("LayoutGuide");
        Undo.RegisterCreatedObjectUndo(guide, "Create LayoutGuide");
        guide.transform.SetParent(grid, false);

        AddLoop(guide.transform, "00_TavernOuterBoundary", Outer,
            P(0,5), P(4,5), P(5,4), P(33,4), P(39,8), P(39,10), P(49,10),
            P(49,30), P(37,30), P(37,25), P(34,25), P(23,27), P(9,25),
            P(4,19), P(4,9), P(0,9));

        AddLoop(guide.transform, "01_PublicEntrance_3Wide", Public,
            P(0,5), P(6,5), P(6,9), P(0,9));

        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
        EditorSceneManager.SaveScene(grid.gameObject.scene);
        Selection.activeGameObject = guide;
        Debug.Log("Cleared tavern terrain and rebuilt the complete LayoutGuide outlines.");
    }

    private static Vector3 P(float x, float y) => new(x, y, -0.5f);

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (var i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
    }

    private static void EnsureTilemap(Transform parent, string name, int sortingOrder)
    {
        if (parent == null) return;
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
    }

    private static void AddLoop(Transform parent, string name, Color color, params Vector3[] points)
        => AddLine(parent, name, color, true, points);

    private static void AddSegment(Transform parent, string name, Color color, params Vector3[] points)
        => AddLine(parent, name, color, false, points);

    private static void AddLine(Transform parent, string name, Color color, bool loop, IReadOnlyList<Vector3> points)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = loop;
        line.positionCount = points.Count;
        line.startWidth = line.endWidth = 0.12f;
        line.numCapVertices = 0;
        line.numCornerVertices = 0;
        line.textureMode = LineTextureMode.Stretch;
        line.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        line.startColor = line.endColor = color;
        line.sortingOrder = 200;
        for (var i = 0; i < points.Count; i++) line.SetPosition(i, points[i]);
    }

    private static void AddLabel(Transform parent, string text, Vector2 position, Color color)
    {
        var go = new GameObject("Label_" + text.Replace(' ', '_'));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, -0.6f);
        var label = go.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 48;
        label.characterSize = 0.10f;
        label.color = color;
        label.anchor = TextAnchor.MiddleLeft;
        label.alignment = TextAlignment.Left;
        label.GetComponent<MeshRenderer>().sortingOrder = 201;
    }
}

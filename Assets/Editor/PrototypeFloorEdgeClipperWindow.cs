using System;
using System.Collections.Generic;
using System.Linq;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEditor;
using UnityEngine;

public sealed class PrototypeFloorEdgeClipperWindow : EditorWindow
{
    private const string AssetRoot =
        "Assets/DungeonTavern/Prototypes/Rotation25D/FloorClipping";
    private const string MeshRoot = AssetRoot + "/Meshes";
    private const string MaterialPath = AssetRoot + "/MAT_PrototypeFloorClip.mat";
    private const string ClipChildName = "FloorClipMesh";

    private enum Slope
    {
        OneToOne,
        TwoToOne
    }

    private enum Direction
    {
        Up,
        Down
    }

    private enum Phase
    {
        A,
        B
    }

    private enum KeepSide
    {
        Above,
        Below
    }

    private Slope slope = Slope.OneToOne;
    private Direction direction = Direction.Up;
    private Phase phase = Phase.A;
    private KeepSide keepSide = KeepSide.Above;

    [MenuItem("Tools/Dungeon Tavern/2.5D Floor Edge Clipper")]
    public static void Open()
    {
        PrototypeFloorEdgeClipperWindow window =
            GetWindow<PrototypeFloorEdgeClipperWindow>(
                utility: false,
                title: "Floor Edge Clipper",
                focus: true);
        window.minSize = new Vector2(360f, 260f);
        EnsureAssets();
    }

    [MenuItem("Tools/Dungeon Tavern/Generate 2.5D Floor Clipping Assets")]
    public static void GenerateAssets()
    {
        EnsureAssets();
        Debug.Log("Generated/validated the 2.5D floor clipping meshes and material.");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("2.5D Floor Edge Clipper", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select one or more Floor_xx_yy objects, or their Sprite children. " +
            "Apply disables the original SpriteRenderer and adds a reversible clipped mesh.",
            MessageType.Info);

        slope = (Slope)EditorGUILayout.EnumPopup("Slope", slope);
        direction = (Direction)EditorGUILayout.EnumPopup("Direction", direction);
        if (slope == Slope.TwoToOne)
        {
            phase = (Phase)EditorGUILayout.EnumPopup("2x1 Phase", phase);
            EditorGUILayout.HelpBox(
                "Along a 2x1 edge, alternate A then B as X increases. " +
                "After B, the next cell starts at A on the next Y row.",
                MessageType.None);
        }

        keepSide = (KeepSide)EditorGUILayout.EnumPopup("Keep Side", keepSide);

        GUILayout.Space(10f);
        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Apply To Selected Floor Tiles", GUILayout.Height(32f)))
                ApplyToSelection();

            if (GUILayout.Button("Restore Selected Full Tiles", GUILayout.Height(26f)))
                RestoreSelection();
        }

        GUILayout.Space(8f);
        EditorGUILayout.LabelField(
            $"Selected objects: {Selection.gameObjects.Length}",
            EditorStyles.miniLabel);
    }

    private void ApplyToSelection()
    {
        EnsureAssets();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(GetMeshPath(
            slope,
            direction,
            slope == Slope.OneToOne ? Phase.A : phase,
            keepSide));
        if (material == null || mesh == null)
            throw new InvalidOperationException("Floor clipping assets could not be loaded.");

        List<GameObject> visuals = ResolveSelectedVisuals();
        if (visuals.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "No floor visuals found",
                "Select Floor_xx_yy objects or their Sprite children.",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 2.5D Floor Edge Clip");

        foreach (GameObject visual in visuals)
        {
            SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null)
                continue;

            Undo.RecordObject(spriteRenderer, "Disable full floor Sprite");
            spriteRenderer.enabled = false;

            Transform clipTransform = visual.transform.Find(ClipChildName);
            GameObject clipObject;
            if (clipTransform == null)
            {
                clipObject = new GameObject(ClipChildName);
                Undo.RegisterCreatedObjectUndo(
                    clipObject,
                    "Create floor clip Mesh child");
                clipObject.transform.SetParent(visual.transform, false);
            }
            else
            {
                clipObject = clipTransform.gameObject;
                Undo.RecordObject(clipObject.transform, "Reset floor clip transform");
                clipObject.transform.localPosition = Vector3.zero;
                clipObject.transform.localRotation = Quaternion.identity;
                clipObject.transform.localScale = Vector3.one;
            }

            MeshFilter meshFilter = clipObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = Undo.AddComponent<MeshFilter>(clipObject);
            Undo.RecordObject(meshFilter, "Assign floor clip Mesh");
            meshFilter.sharedMesh = mesh;

            MeshRenderer meshRenderer = clipObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = Undo.AddComponent<MeshRenderer>(clipObject);
            Undo.RecordObject(meshRenderer, "Assign floor clip Material");
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            meshRenderer.sortingOrder = spriteRenderer.sortingOrder;

            PrototypeFloorClipVisual clipVisual =
                clipObject.GetComponent<PrototypeFloorClipVisual>();
            if (clipVisual == null)
                clipVisual = Undo.AddComponent<PrototypeFloorClipVisual>(clipObject);
            Undo.RecordObject(clipVisual, "Configure floor clip");
            clipVisual.SourceSprite = spriteRenderer.sprite;
            clipVisual.Tint = spriteRenderer.color;

            EditorUtility.SetDirty(visual);
            EditorUtility.SetDirty(clipObject);
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    private static void RestoreSelection()
    {
        List<GameObject> visuals = ResolveSelectedVisuals();
        if (visuals.Count == 0)
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restore full 2.5D floor Tiles");

        foreach (GameObject visual in visuals)
        {
            SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Undo.RecordObject(spriteRenderer, "Enable full floor Sprite");
                spriteRenderer.enabled = true;
            }

            Transform clipTransform = visual.transform.Find(ClipChildName);
            if (clipTransform != null)
                Undo.DestroyObjectImmediate(clipTransform.gameObject);

            // Clean up components left by an interrupted version of this tool.
            PrototypeFloorClipVisual legacyClip =
                visual.GetComponent<PrototypeFloorClipVisual>();
            MeshRenderer legacyRenderer = visual.GetComponent<MeshRenderer>();
            MeshFilter legacyFilter = visual.GetComponent<MeshFilter>();
            if (legacyClip != null)
                Undo.DestroyObjectImmediate(legacyClip);
            if (legacyRenderer != null)
                Undo.DestroyObjectImmediate(legacyRenderer);
            if (legacyFilter != null)
                Undo.DestroyObjectImmediate(legacyFilter);
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    private static List<GameObject> ResolveSelectedVisuals()
    {
        HashSet<GameObject> results = new();
        foreach (GameObject selected in Selection.gameObjects)
        {
            SpriteRenderer direct = selected.GetComponent<SpriteRenderer>();
            if (direct != null)
            {
                results.Add(selected);
                continue;
            }

            SpriteRenderer ancestor =
                selected.GetComponentInParent<SpriteRenderer>(true);
            if (ancestor != null)
            {
                results.Add(ancestor.gameObject);
                continue;
            }

            SpriteRenderer child = selected.GetComponentInChildren<SpriteRenderer>(true);
            if (child != null)
                results.Add(child.gameObject);
        }

        return results.ToList();
    }

    private static void EnsureAssets()
    {
        EnsureFolder(
            "Assets/DungeonTavern/Prototypes/Rotation25D",
            "FloorClipping");
        EnsureFolder(AssetRoot, "Meshes");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP Unlit shader was not found.");
            material = new Material(shader)
            {
                name = "MAT_PrototypeFloorClip",
                renderQueue = 2450
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_AlphaClip", 1f);
        material.SetFloat("_Cutoff", 0.5f);
        material.SetFloat("_Cull", 0f);
        material.EnableKeyword("_ALPHATEST_ON");
        EditorUtility.SetDirty(material);

        foreach (Slope slopeValue in Enum.GetValues(typeof(Slope)))
        {
            foreach (Direction directionValue in Enum.GetValues(typeof(Direction)))
            {
                IEnumerable<Phase> phases = slopeValue == Slope.OneToOne
                    ? new[] { Phase.A }
                    : Enum.GetValues(typeof(Phase)).Cast<Phase>();
                foreach (Phase phaseValue in phases)
                {
                    foreach (KeepSide sideValue in Enum.GetValues(typeof(KeepSide)))
                    {
                        string path = GetMeshPath(
                            slopeValue,
                            directionValue,
                            phaseValue,
                            sideValue);
                        CreateOrUpdateClipMesh(
                            path,
                            slopeValue,
                            directionValue,
                            phaseValue,
                            sideValue);
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
    }

    private static void CreateOrUpdateClipMesh(
        string path,
        Slope slopeValue,
        Direction directionValue,
        Phase phaseValue,
        KeepSide sideValue)
    {
        float lineSlope;
        float intercept;
        if (slopeValue == Slope.OneToOne)
        {
            lineSlope = directionValue == Direction.Up ? 1f : -1f;
            intercept = directionValue == Direction.Up ? 0f : 1f;
        }
        else if (directionValue == Direction.Up)
        {
            lineSlope = 0.5f;
            intercept = phaseValue == Phase.A ? 0f : 0.5f;
        }
        else
        {
            lineSlope = -0.5f;
            intercept = phaseValue == Phase.A ? 1f : 0.5f;
        }

        List<Vector2> polygon = ClipSquare(
            lineSlope,
            intercept,
            sideValue == KeepSide.Above);
        if (polygon.Count < 3)
            throw new InvalidOperationException($"Invalid clip polygon for {path}");

        Vector3[] vertices = polygon
            .Select(point => new Vector3(point.x - 0.5f, point.y - 0.5f, 0f))
            .ToArray();
        Vector2[] uv = polygon.ToArray();
        int[] triangles = new int[(polygon.Count - 2) * 3];
        for (int i = 0; i < polygon.Count - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, path);
        }

        mesh.Clear();
        mesh.name = System.IO.Path.GetFileNameWithoutExtension(path);
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
    }

    private static List<Vector2> ClipSquare(
        float slopeValue,
        float intercept,
        bool keepAbove)
    {
        List<Vector2> input = new()
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        List<Vector2> output = new();

        bool Inside(Vector2 point)
        {
            float delta = point.y - (slopeValue * point.x + intercept);
            return keepAbove ? delta >= -0.0001f : delta <= 0.0001f;
        }

        Vector2 Intersection(Vector2 from, Vector2 to)
        {
            Vector2 directionVector = to - from;
            float denominator = directionVector.y - slopeValue * directionVector.x;
            if (Mathf.Abs(denominator) < 0.00001f)
                return from;
            float numerator = slopeValue * from.x + intercept - from.y;
            float t = Mathf.Clamp01(numerator / denominator);
            return from + directionVector * t;
        }

        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            Vector2 previous = input[(i + input.Count - 1) % input.Count];
            bool currentInside = Inside(current);
            bool previousInside = Inside(previous);

            if (currentInside)
            {
                if (!previousInside)
                    output.Add(Intersection(previous, current));
                output.Add(current);
            }
            else if (previousInside)
            {
                output.Add(Intersection(previous, current));
            }
        }

        List<Vector2> cleaned = new();
        foreach (Vector2 point in output)
        {
            if (cleaned.Count == 0 ||
                (cleaned[^1] - point).sqrMagnitude > 0.0000001f)
            {
                cleaned.Add(point);
            }
        }

        if (cleaned.Count > 1 &&
            (cleaned[0] - cleaned[^1]).sqrMagnitude <= 0.0000001f)
        {
            cleaned.RemoveAt(cleaned.Count - 1);
        }

        return cleaned;
    }

    private static string GetMeshPath(
        Slope slopeValue,
        Direction directionValue,
        Phase phaseValue,
        KeepSide sideValue)
    {
        string slopeName = slopeValue == Slope.OneToOne ? "1x1" : "2x1";
        string phaseName = slopeValue == Slope.OneToOne ? string.Empty : $"_{phaseValue}";
        return $"{MeshRoot}/FloorClip_{slopeName}_{directionValue}" +
               $"{phaseName}_Keep{sideValue}.asset";
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }
}

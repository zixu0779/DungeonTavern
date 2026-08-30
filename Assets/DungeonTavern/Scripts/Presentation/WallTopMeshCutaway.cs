using System.Collections.Generic;
using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    // Clip only the rendered mesh; gate animation and collision retain their original geometry.
    [ExecuteAlways, RequireComponent(typeof(MeshFilter))]
    public sealed class WallTopMeshCutaway : MonoBehaviour
    {
        public Mesh source;
        public float height = 2.3f;
        Mesh clipped;
        Matrix4x4 lastMatrix;
        float lastHeight = float.NaN;
        struct Vertex { public Vector3 p, n; public Vector2 uv; }
        void OnEnable() => Rebuild();
        void LateUpdate()
        {
            if (transform.localToWorldMatrix != lastMatrix || height != lastHeight) Rebuild();
        }
        public void Rebuild()
        {
            if (!source) return;
            if (!clipped) clipped = new Mesh { name = source.name + "_WallTopVisible", hideFlags = HideFlags.HideAndDontSave };
            var positions = source.vertices; var normals = source.normals; var uvs = source.uv;
            var output = new List<Vector3>(); var ns = new List<Vector3>(); var tex = new List<Vector2>();
            var submeshes = new List<int[]>();
            for (int s = 0; s < source.subMeshCount; s++)
            {
                var cap = new List<Vector3>();
                var indices = new List<int>(); var triangles = source.GetTriangles(s);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var poly = new List<Vertex>();
                    for (int j = 0; j < 3; j++) { int k = triangles[i + j]; poly.Add(new Vertex { p = positions[k], n = normals.Length > k ? normals[k] : Vector3.up, uv = uvs.Length > k ? uvs[k] : Vector2.zero }); }
                    var result = new List<Vertex>();
                    for (int j = 0; j < 3; j++)
                    {
                        var a = poly[j]; var b = poly[(j + 1) % 3];
                        float ay = transform.TransformPoint(a.p).y, by = transform.TransformPoint(b.p).y;
                        if (ay <= height) result.Add(a);
                        if ((ay <= height) != (by <= height))
                        {
                            float t = (height - ay) / (by - ay);
                            var point = Vector3.Lerp(a.p, b.p, t);
                            if (!cap.Exists(v => (transform.TransformPoint(v) - transform.TransformPoint(point)).sqrMagnitude < 0.000001f)) cap.Add(point);
                            result.Add(new Vertex { p = Vector3.Lerp(a.p, b.p, t), n = Vector3.Lerp(a.n, b.n, t).normalized, uv = Vector2.Lerp(a.uv, b.uv, t) });
                        }
                    }
                    int start = output.Count;
                    foreach (var v in result) { output.Add(v.p); ns.Add(v.n); tex.Add(v.uv); }
                    for (int j = 1; j + 1 < result.Count; j++) { indices.Add(start); indices.Add(start + j); indices.Add(start + j + 1); }
                }
                // Current gate parts are convex boxes. Close their horizontal cut with the same material.
                if (cap.Count >= 3)
                {
                    Vector3 center = Vector3.zero;
                    foreach (var point in cap) center += point;
                    center /= cap.Count;
                    var worldCenter = transform.TransformPoint(center);
                    cap.Sort((a, b) => {
                        var wa = transform.TransformPoint(a) - worldCenter;
                        var wb = transform.TransformPoint(b) - worldCenter;
                        return Mathf.Atan2(wa.z, wa.x).CompareTo(Mathf.Atan2(wb.z, wb.x));
                    });
                    var normal = transform.localToWorldMatrix.transpose.MultiplyVector(Vector3.up).normalized;
                    int start = output.Count;
                    output.Add(center); ns.Add(normal); tex.Add(new Vector2(worldCenter.x, worldCenter.z) / 3f);
                    foreach (var point in cap) { var world = transform.TransformPoint(point); output.Add(point); ns.Add(normal); tex.Add(new Vector2(world.x, world.z) / 3f); }
                    for (int j = 0; j < cap.Count; j++) { indices.Add(start); indices.Add(start + 1 + (j + 1) % cap.Count); indices.Add(start + 1 + j); }
                }
                submeshes.Add(indices.ToArray());
            }
            clipped.Clear(); clipped.SetVertices(output); clipped.SetNormals(ns); clipped.SetUVs(0, tex); clipped.subMeshCount = submeshes.Count;
            for (int s = 0; s < submeshes.Count; s++) clipped.SetTriangles(submeshes[s], s);
            if (output.Count > 0) clipped.RecalculateTangents();
            clipped.RecalculateBounds(); GetComponent<MeshFilter>().sharedMesh = clipped;
            lastMatrix = transform.localToWorldMatrix; lastHeight = height;
        }
        void OnDisable()
        {
            if (source) GetComponent<MeshFilter>().sharedMesh = source;
            if (clipped) { if (Application.isPlaying) Destroy(clipped); else DestroyImmediate(clipped); }
            clipped = null;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

// Isolated pose sampling; output stays in the system temporary folder.
public static class EveRigCheck
{
    const string Folder = "Assets/DungeonTavern/Art/Characters/Eve/";
    [MenuItem("Tools/Characters/Check Eve")]
    public static void Check()
    {
        var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Eve.prefab"));
        var cg = new GameObject("EvePreviewCamera");
        var cam = cg.AddComponent<Camera>();
        var rt = new RenderTexture(720, 900, 24);
        var tex = new Texture2D(720, 900, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        var output = Path.Combine(Path.GetTempPath(), "eve-animation-preview");
        Directory.CreateDirectory(output);
        var meshes = new List<Mesh>();
        try
        {
            go.transform.position = Vector3.one * 1000;
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.layer = 31;
            var animator = go.GetComponent<Animator>();
            if (!animator.avatar || !animator.avatar.isValid || !animator.isHuman) throw new Exception("Invalid avatar");
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            var skins = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skin in skins)
            {
                if (skin.bones.Any(b => !b) || !skin.sharedMaterial || !skin.sharedMaterial.GetTexture("_BaseMap")) throw new Exception("Missing skin references");
                if (skin.sharedMesh.boneWeights.Any(w => Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1) > .001f)) throw new Exception("Unnormalized skin weights");
            }
            var baked = skins.Select(sk => {
                sk.updateWhenOffscreen = true;
                var child = new GameObject("BakedPreview"); child.layer = 31; child.transform.SetParent(sk.transform, false);
                var mf = child.AddComponent<MeshFilter>(); mf.sharedMesh = new Mesh(); meshes.Add(mf.sharedMesh);
                child.AddComponent<MeshRenderer>().sharedMaterials = sk.sharedMaterials; return mf;
            }).ToArray();
            cam.orthographic = true; cam.orthographicSize = 1.12f; cam.cullingMask = 1 << 31;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.17f, .2f, .24f); cam.targetTexture = rt;
            var report = new List<string>();
            foreach (var state in new[] { "Idle", "Walk" })
            {
                var clip = animator.runtimeAnimatorController.animationClips.Single(c => c.name == state);
                if (!clip.isHumanMotion) throw new Exception("Eve requires Humanoid clips: " + clip.name);
                var graph = PlayableGraph.Create("EvePreview"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                try
                {
                    var playable = AnimationClipPlayable.Create(graph, clip);
                    playable.SetApplyFootIK(state == "Idle");
                    AnimationPlayableOutput.Create(graph, "Preview", animator).SetSourcePlayable(playable); graph.Play();
                    for (int i = 0; i < 8; i++)
                    {
                        playable.SetTime(clip.length * i / 8.0); graph.Evaluate(.00001f);
                        for (int k = 0; k < skins.Length; k++)
                        {
                            skins[k].BakeMesh(baked[k].sharedMesh, false);
                            var scale = skins[k].transform.lossyScale;
                            baked[k].transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
                            skins[k].enabled = false;
                            if (baked[k].sharedMesh.vertices.Any(v => !float.IsFinite(v.x + v.y + v.z))) throw new Exception("Invalid deformed vertices");
                        }
                        foreach (int yaw in new[] { 180, 90, 0, 225 })
                        {
                            cam.transform.rotation = Quaternion.Euler(yaw == 225 ? 35 : 0, yaw, 0);
                            cam.transform.position = go.transform.position + Vector3.up * .9f - cam.transform.forward * 5;
                            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 720, 900), 0, 0); tex.Apply();
                            File.WriteAllBytes(Path.Combine(output, $"{state}-{i}-{yaw}.png"), tex.EncodeToPNG());
                        }
                        var points = baked.SelectMany(mf => mf.sharedMesh.vertices.Select(v => mf.transform.TransformPoint(v))).ToArray();
                        var bounds = new Bounds(points[0], Vector3.zero);
                        foreach (var point in points) bounds.Encapsulate(point);
                        if (Mathf.Abs(bounds.min.y - 1000 + .0445f) > .006f) throw new Exception("Foot grounding drift: " + state + " " + i + " min=" + (bounds.min.y - 1000));
                        if (bounds.size.y < 1.7f || bounds.size.y > 2.1f) throw new Exception("Unexpected deformed height");
                        report.Add($"{state} {i}: minY={bounds.min.y - 1000:F4} height={bounds.size.y:F4} width={bounds.size.x:F4}");
                    }
                }
                finally { graph.Destroy(); }
            }
            File.WriteAllLines(Path.Combine(output, "check.txt"), report);
            Debug.Log("Eve avatar/skin/Idle/Walk sampling passed: " + output);
        }
        finally
        {
            RenderTexture.active = previous; cam.targetTexture = null;
            foreach (var mesh in meshes) UnityEngine.Object.DestroyImmediate(mesh);
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(cg);
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}

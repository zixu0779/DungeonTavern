using System.Collections.Generic;
using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    // Sphere casting / world-space mask approach from art ofsully's public BG3 breakdown.
    // Runtime material copies only; disabling this component restores all authored materials.
    [DefaultExecutionOrder(100)]
    public sealed class DialogueOcclusionFader : MonoBehaviour
    {
        [SerializeField, Min(.1f)] float probeRadius = .55f;
        [SerializeField, Min(.1f)] float cutRadius = 1.8f;
        [SerializeField, Min(.02f)] float openingSeconds = .2f, closingSeconds = .3f;
        sealed class Wall
        {
            public Renderer renderer;
            public Material[] originals, copies;
            public MaterialPropertyBlock[] blocks;
        }
        sealed class Mask
        {
            public Transform actor;
            public Vector3 center, targetCenter, actorPoint;
            public float radius, targetRadius, transition, clearSince;
        }
        readonly Dictionary<Renderer, Wall> walls = new();
        readonly Mask[] masks = { new(), new() };
        MaterialPropertyBlock block;
        Camera view;
        PrototypeCameraOrbit orbit;
        Shader cutShader;
        Texture2D brickCore, stoneCore;
        [SerializeField] bool enableSections = true;
        float nextProbe, nextScan;
        static readonly int Cuttable = Shader.PropertyToID("_TavernCuttable");
        void Awake()
        {
            block = new MaterialPropertyBlock();
            orbit = GetComponent<PrototypeCameraOrbit>();
            view = GetComponentInChildren<Camera>();
            cutShader = Resources.Load<Shader>("TavernWallCutout");
            brickCore = Resources.Load<Texture2D>("WallSectionBrick");
            stoneCore = Resources.Load<Texture2D>("WallSectionStone");
        }
        public static bool IsWall(Transform item)
        {
            for (var t = item; t; t = t.parent)
            {
                // Moving leaves are props, even when their door frame lives under Walls.
                if (t.name == "DoorHinge" || t.name.EndsWith("Door_Hinge") ||
                    t.name == "DoorLeaf" || t.name.EndsWith("Door_Leaf")) return false;
                if (t.name == "Walls" || t.name == "Walls_Stone" || t.name == "StairRearEnclosure") return true;
            }
            return false;
        }
        void ScanWalls()
        {
            // Additive floor travel unloads meshes while this camera persists.
            var removed = new List<Renderer>();
            foreach (var pair in walls)
                if (!pair.Key)
                {
                    foreach (var material in pair.Value.copies) if (material) Destroy(material);
                    removed.Add(pair.Key);
                }
            foreach (var renderer in removed) walls.Remove(renderer);
            foreach (var renderer in FindObjectsByType<MeshRenderer>())
            {
                if (!IsWall(renderer.transform) || walls.ContainsKey(renderer)) continue;
                var originals = renderer.sharedMaterials;
                var state = new Wall { renderer = renderer, originals = originals,
                    copies = new Material[originals.Length], blocks = new MaterialPropertyBlock[originals.Length] };
                var assigned = (Material[])originals.Clone();
                for (int i = 0; i < originals.Length; i++)
                {
                    var material = originals[i];
                    state.blocks[i] = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(state.blocks[i], i);
                    if (!material) continue;
                    if (material.shader.name != "DungeonTavern/Native Pixel Face")
                    {
                        // The environment currently uses unlit colours; preserve its texture, tint and UV transform.
                        if (!cutShader || material.shader.name != "Universal Render Pipeline/Unlit") continue;
                        var copy = new Material(cutShader) { name = material.name + "_LocalCutout", hideFlags = HideFlags.DontSave };
                        copy.CopyPropertiesFromMaterial(material);
                        state.copies[i] = copy; assigned[i] = copy;
                    }
                    renderer.GetPropertyBlock(block, i);
                    block.SetFloat(Cuttable, 1);
                    var bounds = renderer.localBounds;
                    block.SetVector("_TavernWallMin", bounds.min);
                    block.SetVector("_TavernWallMax", bounds.max);
                    block.SetMatrix("_TavernWallWorldToLocal", renderer.transform.worldToLocalMatrix);
                    block.SetFloat("_TavernStoneSection", renderer.gameObject.scene.name == "SealRoom_B1" ? 1 : 0);
                    renderer.SetPropertyBlock(block, i);
                }
                renderer.sharedMaterials = assigned;
                walls.Add(renderer, state);
            }
        }
        void Probe(Mask mask, Transform actor)
        {
            if (!actor || !actor.gameObject.activeInHierarchy)
            {
                mask.actor = null;mask.targetRadius = 0;return;
            }
            bool changed = mask.actor != actor;
            mask.actor = actor;
            var bounds = new Bounds(actor.position + Vector3.up, new Vector3(.5f, 2, .5f));
            bool found = false;
            foreach (var r in actor.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                if (!found) { bounds = r.bounds;found = true; } else bounds.Encapsulate(r.bounds);
            }
            var point = bounds.center;
            mask.actorPoint = point;
            var direction = view.orthographic ? -view.transform.forward : (view.transform.position - point).normalized;
            var start = point + direction * (probeRadius + .05f);
            float distance = Mathf.Max(0, Vector3.Dot(view.transform.position - start, direction));
            float nearest = float.PositiveInfinity;
            Vector3 hitPoint = point;
            foreach (var c in Physics.OverlapCapsule(point, start, probeRadius, ~0, QueryTriggerInteraction.Ignore))
                if (IsWall(c.transform))
                {
                    var closest = c.ClosestPoint(point);
                    if (Vector3.Dot(closest - point, direction) < -.01f) continue;
                    if ((closest - start).magnitude >= nearest) continue;
                    nearest = (closest - start).magnitude;hitPoint = closest;
                }
            foreach (var hit in Physics.SphereCastAll(start, probeRadius, direction, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!IsWall(hit.transform) || hit.distance >= nearest) continue;
                // Initial overlaps are handled above; Unity supplies no usable hit point for them.
                if (hit.distance <= .0001f) continue;
                nearest = hit.distance;hitPoint = hit.point;
            }
            if (!float.IsInfinity(nearest))
            {
                mask.targetCenter = hitPoint;
                mask.targetRadius = Mathf.Max(cutRadius, bounds.size.y * .8f);
                mask.clearSince = Time.time;
            }
            else if (Time.time - mask.clearSince > .15f) mask.targetRadius = 0;
            if (changed || Vector3.Distance(mask.center, mask.targetCenter) > 8)
            { mask.center = mask.targetCenter;mask.radius = Mathf.Max(cutRadius, bounds.size.y * .8f);mask.transition = 0; }
        }
        void LateUpdate()
        {
            if (!orbit || !view) return;
            if (Time.time >= nextScan) { nextScan = Time.time + 1;ScanWalls(); }
            if (Time.time >= nextProbe)
            {
                nextProbe = Time.time + .1f;
                Probe(masks[0], orbit.OcclusionPrimary);
                Probe(masks[1], orbit.OcclusionSecondary);
            }
            Shader.SetGlobalTexture("_TavernBrickCore", brickCore);
            Shader.SetGlobalTexture("_TavernStoneCore", stoneCore);
            Shader.SetGlobalFloat("_TavernSectionsEnabled", enableSections && brickCore && stoneCore ? 1 : 0);
            Shader.SetGlobalVector("_TavernCutCamera", view.transform.position);
            Shader.SetGlobalVector("_TavernCutForward", view.transform.forward);
            Shader.SetGlobalFloat("_TavernCutOrthographic", view.orthographic ? 1 : 0);
            for (int i = 0; i < masks.Length; i++)
            {
                var mask = masks[i];
                mask.center = Vector3.Lerp(mask.center, mask.targetCenter, 1 - Mathf.Exp(-Time.deltaTime * 15));
                float target = mask.targetRadius > 0 ? 1 : 0;
                float duration = target > mask.transition ? openingSeconds : closingSeconds;
                mask.transition = Mathf.MoveTowards(mask.transition, target, Time.deltaTime / duration);
                if (mask.targetRadius > 0) mask.radius = mask.targetRadius;
                Shader.SetGlobalFloat(i == 0 ? "_TavernCutTransition0" : "_TavernCutTransition1", mask.transition);
                Shader.SetGlobalVector(i == 0 ? "_TavernCutSphere0" : "_TavernCutSphere1", new Vector4(mask.center.x, mask.center.y, mask.center.z, mask.transition > 0 ? mask.radius : 0));
                Shader.SetGlobalVector(i == 0 ? "_TavernCutActor0" : "_TavernCutActor1", mask.actorPoint);
            }
        }
        public void RestoreAll()
        {
            foreach (var wall in walls.Values)
            {
                if (wall.renderer)
                {
                    wall.renderer.sharedMaterials = wall.originals;
                    for (int i = 0; i < wall.blocks.Length; i++) wall.renderer.SetPropertyBlock(wall.blocks[i], i);
                }
                foreach (var material in wall.copies) if (material) Destroy(material);
            }
            walls.Clear();nextScan = nextProbe = 0;
            Shader.SetGlobalFloat("_TavernSectionsEnabled", 0);
            foreach (var mask in masks) { mask.actor = null;mask.radius = mask.targetRadius = mask.transition = 0; }
            Shader.SetGlobalFloat("_TavernCutTransition0", 0);
            Shader.SetGlobalFloat("_TavernCutTransition1", 0);
            Shader.SetGlobalVector("_TavernCutSphere0", Vector4.zero);
            Shader.SetGlobalVector("_TavernCutSphere1", Vector4.zero);
        }
        void OnDisable() => RestoreAll();
    }
}

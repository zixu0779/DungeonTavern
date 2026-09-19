using System.Collections.Generic;
using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    // Sphere casting / world-space mask approach from art ofsully's public BG3 breakdown.
    // Runtime material copies only; disabling this component restores all authored materials.
    [DefaultExecutionOrder(300)]
    public sealed class DialogueOcclusionFader : MonoBehaviour
    {
        [SerializeField, Min(0)] float silhouettePadding = .45f;
        [SerializeField, Min(.1f)] float cutRadius = 1.2f;
        [SerializeField, Min(.02f)] float probeRadius = .12f;
        [SerializeField, Min(.02f)] float openingSeconds = .6f, closingSeconds = .3f;
        sealed class Wall
        {
            public Renderer renderer;
            public Material[] originals, copies;
            public MaterialPropertyBlock[] blocks;
            public Matrix4x4 matrix;
            public Bounds bounds;
            public WallCutoutGroup group;
            public float appliedGroup = -1;
        }
        sealed class Mask
        {
            public Transform actor;
            public Vector3 center, targetCenter, actorPoint;
            public float radius, targetRadius, transition, clearSince, coverageRadius;
            public Bounds bounds;
            public Renderer[] renderers;
            public Bounds[] localBounds;
            public Mesh baked;
            public float nextBounds;
            public Vector3 previousPosition, lookAhead;
            public float lastOccluded = float.NegativeInfinity;
            public Vector3 centerVelocity;
            public float radiusVelocity;
            public int blockedSamples;
            public DungeonTavern.Tavern25D.CharacterModelMotion motion;
            public bool stable;
            public float settleAt, standingHeight, standingRadius;
        }
        sealed class GroupState { public float lastHit = float.NegativeInfinity, strength; }
        readonly Dictionary<WallCutoutGroup, GroupState> groups = new();
        readonly Dictionary<Renderer, Wall> walls = new();
        readonly Mask[] masks = { new(), new() };
        MaterialPropertyBlock block;
        Camera view;
        PrototypeCameraOrbit orbit;
        Shader cutShader;
        [SerializeField] bool enableSections = true;
        float nextProbe, nextScan, pairBlend;
        static readonly int Cuttable = Shader.PropertyToID("_TavernCuttable");
        void Awake()
        {
            block = new MaterialPropertyBlock();
            orbit = GetComponent<PrototypeCameraOrbit>();
            view = GetComponentInChildren<Camera>();
            cutShader = Resources.Load<Shader>("TavernWallCutout");
        }
        public static bool IsWall(Transform item)
        {
            var door = item.GetComponentInParent<DungeonTavern.Tavern25D.DoorStateController>();
            if (door && door.name.StartsWith("Door_Small_Stone") && door.IsLeaf(item)) return false;
            for (var t = item; t; t = t.parent)
            {
                if (t.name == "Signs" || t.name == "Stairs") return false;
                if (t.GetComponent<WallCutoutGroup>()) return true;
                if (t.name == "Walls" || t.name == "Walls_Stone" || t.name == "StairRearEnclosure" || t.name == "StoneGates") return true;
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
                var group=renderer.GetComponentInParent<WallCutoutGroup>();
                if(!group)continue; // Ungrouped props never inherit another wall's cut.
                if(!groups.ContainsKey(group))groups.Add(group,new GroupState());
                var originals = renderer.sharedMaterials;
                var state = new Wall { renderer = renderer, originals = originals, group = group,
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
                    block.SetFloat("_TavernCutGroup",0);
                    // Door frames and leaves retain their authored surfaces, without inferred volume caps.
                    block.SetFloat("_TavernSurfaceOnly", renderer.GetComponentInParent<DungeonTavern.Tavern25D.DoorStateController>()?.name.StartsWith("Door_Small_Stone") == true ? 1 : 0);
                    var bounds = renderer.localBounds;
                    block.SetVector("_TavernWallMin", bounds.min);
                    block.SetVector("_TavernWallMax", bounds.max);
                    
                    block.SetFloat("_TavernStoneSection", renderer.gameObject.scene.name == "SealRoom_B1" ? 1 : 0);
                    SetSectionSurface(renderer, originals, block);
                    renderer.SetPropertyBlock(block, i);
                }
                renderer.sharedMaterials = assigned;
                walls.Add(renderer, state);
            }
        }
        static void SetSectionSurface(Renderer renderer,Material[] materials,MaterialPropertyBlock target)
        {
            if(materials.Length==0 || !materials[0])return;
            var material=materials[0];var front=new MaterialPropertyBlock();renderer.GetPropertyBlock(front,0);
            target.SetTexture("_SectionMap",front.GetTexture("_BaseMap") ?? material.GetTexture("_BaseMap") ?? Texture2D.whiteTexture);
            target.SetColor("_SectionTint",front.HasColor("_BaseColor")?front.GetColor("_BaseColor"):material.GetColor("_BaseColor"));
            target.SetVector("_SectionST",front.HasVector("_BaseMap_ST")?front.GetVector("_BaseMap_ST"):new Vector4(material.mainTextureScale.x,material.mainTextureScale.y,material.mainTextureOffset.x,material.mainTextureOffset.y));
            bool native=material.shader.name=="DungeonTavern/Native Pixel Face";
            target.SetFloat("_SectionNative",native?1:0);
            foreach(var pair in new[]{("_SpriteRect","_SectionRect"),("_FaceUvScale","_SectionScale")})
                target.SetVector(pair.Item2,front.HasVector(pair.Item1)?front.GetVector(pair.Item1):native?material.GetVector(pair.Item1):Vector4.one);
            target.SetVector("_SectionOptions",new Vector4(front.GetFloat("_FaceRotation"),front.GetFloat("_FlipX"),front.GetFloat("_FlipY"),0));
            var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if(!mesh || !mesh.isReadable)return;
            var v=mesh.vertices;var uv=mesh.uv;var ids=mesh.GetTriangles(0);
            var triangles=mesh.triangles;
            if(triangles.Length<=288)
            {
                var positions=new Vector4[288];for(int i=0;i<triangles.Length;i++)positions[i]=v[triangles[i]];
                target.SetVectorArray("_SectionTriangles",positions);target.SetInt("_SectionTriangleCount",triangles.Length/3);
            }
            if(uv.Length!=v.Length || ids.Length<3)return;
            // Largest front-face triangle supplies the authored affine UV projection.
            float best=0;Vector4 u=Vector4.zero,w=Vector4.zero;
            for(int i=0;i<ids.Length;i+=3){int a=ids[i],b=ids[i+1],c=ids[i+2];var e=v[b]-v[a];var f=v[c]-v[a];float area=Vector3.Cross(e,f).sqrMagnitude;if(area<=best)continue;
                float ee=Vector3.Dot(e,e),ff=Vector3.Dot(f,f),ef=Vector3.Dot(e,f),den=ee*ff-ef*ef;if(den<.000001f)continue;
                var du=uv[b]-uv[a];var dv=uv[c]-uv[a];
                var gu=(e*(du.x*ff-dv.x*ef)+f*(dv.x*ee-du.x*ef))/den;
                var gv=(e*(du.y*ff-dv.y*ef)+f*(dv.y*ee-du.y*ef))/den;
                u=new Vector4(gu.x,gu.y,gu.z,uv[a].x-Vector3.Dot(gu,v[a]));w=new Vector4(gv.x,gv.y,gv.z,uv[a].y-Vector3.Dot(gv,v[a]));best=area;
            }
            target.SetVector("_SectionU",u);target.SetVector("_SectionV",w);
        }
        void UpdateWallTransforms()
        {
            foreach(var wall in walls.Values)
            {
                var r=wall.renderer;if(!r || !r.enabled || !r.gameObject.activeInHierarchy)continue;
                var matrix=r.transform.worldToLocalMatrix;var bounds=r.localBounds;
                if(wall.matrix==matrix && wall.bounds==bounds)continue;
                wall.matrix=matrix;wall.bounds=bounds;
                for(int i=0;i<wall.originals.Length;i++)
                {
                    r.GetPropertyBlock(block,i);
                    block.SetFloat(Cuttable,1);
                    block.SetMatrix("_TavernWallWorldToLocal",r.transform.worldToLocalMatrix);
                    block.SetVector("_TavernWallMin",r.localBounds.min);
                    block.SetVector("_TavernWallMax",r.localBounds.max);
                    r.SetPropertyBlock(block,i);
                }
            }
        }
        void Track(Mask mask, Transform actor)
        {
            if (!actor || !actor.gameObject.activeInHierarchy)
            { mask.actor = null;mask.targetRadius = 0;mask.blockedSamples = 0;return; }
            bool changed = mask.actor != actor;
            if (changed)
            {
                mask.renderers = actor.GetComponentsInChildren<Renderer>();
                mask.localBounds = new Bounds[mask.renderers.Length];mask.nextBounds=0;
                mask.motion=actor.GetComponentInChildren<DungeonTavern.Tavern25D.CharacterModelMotion>();
                mask.stable=false;mask.settleAt=Time.time+.6f;
            }
            if(mask.motion && mask.motion.IsFullBodyAction){mask.stable=false;mask.settleAt=Time.time+.6f;}
            if(mask.stable && !changed)
            {
                mask.targetCenter=actor.position+Vector3.up*mask.standingHeight;
                mask.actorPoint=actor.position;mask.coverageRadius=mask.standingRadius;
                return;
            }
            if (Time.time>=mask.nextBounds)
            {
                mask.nextBounds=Time.time+.1f;
                for(int i=0;i<mask.renderers.Length;i++)
                {
                    var r=mask.renderers[i];if(!r)continue;
                    if(r is SkinnedMeshRenderer skin && skin.enabled && skin.gameObject.activeInHierarchy)
                    {
                        if(!mask.baked)mask.baked=new Mesh {name="OcclusionPoseBounds",hideFlags=HideFlags.HideAndDontSave};
                        skin.BakeMesh(mask.baked, true);mask.baked.RecalculateBounds();mask.localBounds[i]=mask.baked.bounds;
                    }
                    else mask.localBounds[i]=r.localBounds;
                }
            }
            mask.actor = actor;
            var bounds = new Bounds(actor.position + Vector3.up, new Vector3(.5f, 2, .5f));
            bool found = false;
            for(int i=0;i<mask.renderers.Length;i++)
            {
                var r=mask.renderers[i];
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                var local=mask.localBounds[i];
                for(int k=0;k<8;k++)
                {
                    var corner=local.center+Vector3.Scale(local.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1));
                    var point=r.transform.TransformPoint(corner);
                    if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
                }
            }
            mask.bounds = bounds;
            mask.targetCenter = bounds.center;
            mask.actorPoint = actor.position;
            float radius=0;
            // Project each local box once, rather than projecting an inflated world AABB.
            for(int i=0;i<mask.renderers.Length;i++)
            {
                var r=mask.renderers[i];
                if(!r || !r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer)continue;
                var local=mask.localBounds[i];
                for(int k=0;k<8;k++)
                {
                    var corner=local.center+Vector3.Scale(local.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1));
                    radius=Mathf.Max(radius,Vector3.ProjectOnPlane(r.transform.TransformPoint(corner)-bounds.center,view.transform.forward).magnitude);
                }
            }
            mask.coverageRadius=Mathf.Max(cutRadius,radius+silhouettePadding);
            if(Time.time>=mask.settleAt)
            {
                mask.stable=true;mask.standingHeight=bounds.center.y-actor.position.y;
                mask.standingRadius=mask.coverageRadius;
            }
            if (changed || Vector3.Distance(mask.center, mask.targetCenter) > 8)
            { mask.center = mask.targetCenter;mask.radius = mask.coverageRadius;mask.transition = 0;mask.targetRadius = 0;mask.previousPosition=actor.position;mask.lookAhead=Vector3.zero;mask.lastOccluded=float.NegativeInfinity; }
        }
        bool MarkOccludingGroup(Collider collider)
        {
            if(!IsWall(collider.transform))return false;
            var group=collider.GetComponentInParent<WallCutoutGroup>();
            if(!group || !group.isActiveAndEnabled)return false;
            if(!groups.TryGetValue(group,out var state))groups.Add(group,state=new GroupState());
            state.lastHit=Time.time;
            return true;
        }
        bool ProbePoint(Vector3 point, float radius)
        {
            var direction=view.orthographic ? -view.transform.forward : (view.transform.position-point).normalized;
            var origin=point+direction*(radius+.01f);
            float distance=Mathf.Max(0,Vector3.Dot(view.transform.position-origin,direction));
            bool blocked=false;
            foreach(var c in Physics.OverlapSphere(origin,radius,~0,QueryTriggerInteraction.Ignore))
                if(Vector3.Dot(c.bounds.ClosestPoint(origin)-point,direction)>.01f)
                    blocked |= MarkOccludingGroup(c);
            foreach(var hit in Physics.SphereCastAll(origin,radius,direction,distance,~0,QueryTriggerInteraction.Ignore))
                if(Vector3.Dot(hit.point-point,direction)>.01f)blocked |= MarkOccludingGroup(hit.collider);
            return blocked;
        }
        void UpdateGroups()
        {
            foreach(var pair in groups)
            {
                var state=pair.Value;
                bool hit=pair.Key && pair.Key.isActiveAndEnabled && Time.time-state.lastHit<=.16f;
                // Group membership fades independently of the shared opening radius.
                float duration=hit?.22f:closingSeconds;
                state.strength=Mathf.MoveTowards(state.strength,hit?1:0,Time.deltaTime/duration);
            }
            foreach(var wall in walls.Values)
            {
                if(!wall.renderer || !wall.group)continue;
                float strength=groups[wall.group].strength;
                if(Mathf.Approximately(strength,wall.appliedGroup))continue;
                wall.appliedGroup=strength;
                for(int i=0;i<wall.originals.Length;i++)
                {
                    wall.renderer.GetPropertyBlock(block,i);
                    block.SetFloat("_TavernCutGroup",strength);
                    wall.renderer.SetPropertyBlock(block,i);
                }
            }
        }
        void Probe(Mask mask)
        {
            if (!mask.actor) return;
            // Stable ankle samples, not animated feet: only a few centimetres of anticipation.
            var feet=mask.actor.position+Vector3.up*.12f;
            var side=view.transform.right*.18f;
            bool blocked=(ProbePoint(mask.targetCenter,probeRadius) | ProbePoint(feet-side,.06f) | ProbePoint(feet+side,.06f));
            mask.blockedSamples=blocked?1:0;
            if(blocked)mask.lastOccluded=Time.time;
            bool continuing=Time.time-mask.lastOccluded<=.2f;
            mask.targetRadius=continuing?mask.coverageRadius:0;
            // Predict only while an actual obstruction is active, never from clear space.
            // The short swept path admits the next wall without admitting walls behind us.
            if(continuing && mask.lookAhead.sqrMagnitude>.0001f)
                for(int step=1;step<=3;step++)
                {
                    var offset=mask.lookAhead*(step/3f);
                    ProbePoint(mask.targetCenter+offset,probeRadius);
                    ProbePoint(feet-side+offset,.06f);
                    ProbePoint(feet+side+offset,.06f);
                }
        }

        void LateUpdate()
        {
            if (!orbit || !view) return;
            if (Time.time >= nextScan) { nextScan = Time.time + 1;ScanWalls(); }
            UpdateWallTransforms();
            Track(masks[0], orbit.OcclusionPrimary);
            Track(masks[1], orbit.OcclusionSecondary);
            foreach(var mask in masks)
            {
                if(!mask.actor)continue;
                var delta=Vector3.ProjectOnPlane(mask.actor.position-mask.previousPosition,Vector3.up);
                mask.previousPosition=mask.actor.position;
                var ahead=delta.sqrMagnitude>4?Vector3.zero:Vector3.ClampMagnitude(delta/Mathf.Max(Time.deltaTime,.001f)*.3f,.65f);
                mask.lookAhead=Vector3.Lerp(mask.lookAhead,ahead,1-Mathf.Exp(-Time.deltaTime*12));
            }
            if (orbit.IsRotating || Time.time >= nextProbe)
            {
                nextProbe = Time.time + .1f;
                Probe(masks[0]);Probe(masks[1]);
                // Dialogue is one composition: a blocked participant opens a shared silhouette.
                if(masks[0].actor && masks[1].actor && (masks[0].targetRadius>0 || masks[1].targetRadius>0))
                    foreach(var mask in masks) mask.targetRadius=mask.coverageRadius;
            }
            UpdateGroups();
            pairBlend=Mathf.MoveTowards(pairBlend, masks[1].actor ? 1 : 0, Time.deltaTime/closingSeconds);
            Shader.SetGlobalFloat("_TavernCutPair",pairBlend);
            Shader.SetGlobalFloat("_TavernSectionsEnabled", enableSections ? 1 : 0);
            Shader.SetGlobalVector("_TavernCutCamera", view.transform.position);
            Shader.SetGlobalVector("_TavernCutForward", view.transform.forward);
            Shader.SetGlobalFloat("_TavernCutOrthographic", view.orthographic ? 1 : 0);
            for (int i = 0; i < masks.Length; i++)
            {
                var mask = masks[i];
                mask.center = Vector3.SmoothDamp(mask.center, mask.targetCenter, ref mask.centerVelocity, .18f);
                float target = mask.targetRadius > 0 ? 1 : 0;
                float duration = target > mask.transition ? openingSeconds : closingSeconds;
                mask.transition = Mathf.MoveTowards(mask.transition, target, Time.deltaTime / duration);
                mask.radius = Mathf.SmoothDamp(mask.radius, mask.coverageRadius, ref mask.radiusVelocity, .35f);
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
            walls.Clear();groups.Clear();nextScan = nextProbe = 0;pairBlend=0;
            Shader.SetGlobalFloat("_TavernCutPair",0);
            Shader.SetGlobalFloat("_TavernSectionsEnabled", 0);
            foreach (var mask in masks) { mask.actor = null;mask.radius = mask.targetRadius = mask.transition = 0;if(mask.baked)Destroy(mask.baked);mask.baked=null; }
            Shader.SetGlobalFloat("_TavernCutTransition0", 0);
            Shader.SetGlobalFloat("_TavernCutTransition1", 0);
            Shader.SetGlobalVector("_TavernCutSphere0", Vector4.zero);
            Shader.SetGlobalVector("_TavernCutSphere1", Vector4.zero);
        }
        void OnDisable() => RestoreAll();
    }
}

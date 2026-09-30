using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace DungeonTavern.UI
{
    // Bake only renderer geometry for a still portrait; never duplicate gameplay components.
    public sealed class TavernPortrait : MonoBehaviour
    {
        RawImage image;Transform lastActor;RenderTexture texture;readonly List<Mesh> baked=new();
        public void Initialize(Transform parent)
        {
            var r=TavernUiTheme.Rect("Portrait",parent);TavernUiTheme.Place(r,-342,-292,330,480,new Vector2(1,1));
            image=r.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
        }
        public void Show(Transform actor,bool visible)
        {
            bool available=visible&&actor&&actor.gameObject.activeInHierarchy;
            image.gameObject.SetActive(available);if(!available){lastActor=null;return;}if(lastActor==actor)return;lastActor=actor;
            var root=new GameObject("PortraitRender");root.transform.position=new Vector3(10000,10000,10000);
            bool any=false,hasHead=false;Bounds bounds=default,headBounds=default;
            var animator=actor.GetComponentInChildren<Animator>();
            Transform head=animator&&animator.isHuman?animator.GetBoneTransform(HumanBodyBones.Head):null;
            foreach(var source in actor.GetComponentsInChildren<Renderer>(true))
            {
                if(!source.enabled||!ActiveUnder(source.transform,actor))continue;
                Mesh mesh=null;
                if(source is SkinnedMeshRenderer skin){mesh=new Mesh();skin.BakeMesh(mesh);baked.Add(mesh);}
                else if(source is MeshRenderer)mesh=source.GetComponent<MeshFilter>()?.sharedMesh;
                if(mesh==null)continue;
                var copy=new GameObject("PortraitMesh",typeof(MeshFilter),typeof(MeshRenderer));copy.layer=31;copy.transform.SetParent(root.transform,false);
                copy.transform.position=root.transform.position+Quaternion.Inverse(actor.rotation)*(source.transform.position-actor.position);
                copy.transform.rotation=Quaternion.Inverse(actor.rotation)*source.transform.rotation;// BakeMesh already includes the skinned renderer scale.
                copy.transform.localScale=source is SkinnedMeshRenderer?Vector3.one:source.transform.lossyScale;
                if(head&&source is SkinnedMeshRenderer headSkin)
                {
                    var bones=headSkin.bones;var weights=headSkin.sharedMesh.boneWeights;var vertices=mesh.vertices;
                    for(int i=0;i<weights.Length;i++)
                    {
                        var w=weights[i];float influence=HeadWeight(w.boneIndex0,w.weight0)+HeadWeight(w.boneIndex1,w.weight1)+HeadWeight(w.boneIndex2,w.weight2)+HeadWeight(w.boneIndex3,w.weight3);
                        if(influence<.5f)continue;
                        var point=copy.transform.TransformPoint(vertices[i]);
                        if(!hasHead){headBounds=new Bounds(point,Vector3.zero);hasHead=true;}else headBounds.Encapsulate(point);
                    }
                    float HeadWeight(int index,float weight)=>index<bones.Length&&bones[index]&&(bones[index]==head||bones[index].IsChildOf(head))?weight:0;
                }
                copy.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=copy.GetComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials;
                if(!any){bounds=renderer.bounds;any=true;}else bounds.Encapsulate(renderer.bounds);
            }
            if(any)
            {
                if(texture==null){texture=new RenderTexture(440,640,24,RenderTextureFormat.ARGB32);texture.Create();image.texture=texture;}
                var cameraObject=new GameObject("PortraitCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
                camera.orthographic=true;camera.aspect=440f/640;camera.orthographicSize=hasHead?headBounds.size.y*1.15f:Mathf.Max(bounds.size.y*.52f,bounds.size.x/(2*camera.aspect)*1.05f);float distance=Mathf.Max(10,bounds.extents.magnitude*3);camera.nearClipPlane=.01f;camera.farClipPlane=distance*3;
                // Frame a consistent bust; equipment and the full-body silhouette do not set the zoom.
                Vector3 focus=hasHead?headBounds.center-Vector3.up*headBounds.size.y*.48f:bounds.center;camera.transform.position=focus+new Vector3(.25f,.08f,1).normalized*distance;camera.transform.LookAt(focus);camera.targetTexture=texture;camera.Render();camera.targetTexture=null;Release(cameraObject);
            }
            image.enabled=any;
            if(!any)lastActor=null;
            root.SetActive(false);Release(root);foreach(var mesh in baked)Release(mesh);baked.Clear();
        }
        static void Release(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        static bool ActiveUnder(Transform child,Transform actor)
        {for(var t=child;t!=null&&t!=actor;t=t.parent)if(!t.gameObject.activeSelf)return false;return true;}
        void OnDestroy(){if(texture){texture.Release();Release(texture);}}
    }
}

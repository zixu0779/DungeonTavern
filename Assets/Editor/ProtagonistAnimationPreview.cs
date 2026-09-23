using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor.Animations;
using System.Linq;
// Captures existing controller states on an isolated instance; scene assets stay untouched.
public static class ProtagonistAnimationPreview {
 [MenuItem("Tools/Characters/Capture Protagonist Animations")]
 public static void Capture(){
 const string folder="Assets/DungeonTavern/Art/Characters/Protagonist/";
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"Protagonist.prefab"));
 var cg=new GameObject("ProtagonistPreviewCamera");var cam=cg.AddComponent<Camera>();
 var rt=new RenderTexture(640,640,24);var tex=new Texture2D(640,640,TextureFormat.RGB24,false);
 var previous=RenderTexture.active;string output=Path.Combine(Path.GetTempPath(),"protagonist-animation-preview");Directory.CreateDirectory(output);
 try{
 go.transform.position=new Vector3(1000,1000,1000);foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
 var animator=go.GetComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;animator.Rebind();
 cam.orthographic=true;cam.orthographicSize=1.12f;cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.21f,.28f);
 cam.transform.position=go.transform.position+new Vector3(2.5f,1.35f,4);cam.transform.LookAt(go.transform.position+Vector3.up*.88f);cam.targetTexture=rt;
 var skins=go.GetComponentsInChildren<SkinnedMeshRenderer>();
 var baked=skins.Select(sk=>{sk.updateWhenOffscreen=true;var child=new GameObject("BakedPreview");child.layer=31;child.transform.SetParent(sk.transform,false);var mf=child.AddComponent<MeshFilter>();mf.sharedMesh=new Mesh();child.AddComponent<MeshRenderer>().sharedMaterials=sk.sharedMaterials;return mf;}).ToArray();
 var controller=(AnimatorController)animator.runtimeAnimatorController;
 foreach(var state in new[]{"Idle","Walk","SitDown","SeatedIdle","StandUp","Prone","WakeUp","GetUp","Vault"}){
 var clip=(AnimationClip)controller.layers[0].stateMachine.states.Single(s=>s.state.name==state).state.motion;
 var graph=PlayableGraph.Create("ProtagonistPreview");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
 var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);
 var outputAnimation=AnimationPlayableOutput.Create(graph,"Preview",animator);outputAnimation.SetSourcePlayable(playable);graph.Play();
 try{
 for(int i=0;i<16;i++){
 playable.SetTime(clip.length*i/16.0);graph.Evaluate(.00001f);
 for(int k=0;k<skins.Length;k++){skins[k].BakeMesh(baked[k].sharedMesh,false);baked[k].transform.localScale=new Vector3(1/skins[k].transform.lossyScale.x,1/skins[k].transform.lossyScale.y,1/skins[k].transform.lossyScale.z);skins[k].enabled=false;}
 cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,state+"-"+i.ToString("00")+".png"),tex.EncodeToPNG());
 }
 }finally{graph.Destroy();}
 }
 foreach(var mesh in baked)UnityEngine.Object.DestroyImmediate(mesh.sharedMesh);
 Debug.Log("Protagonist animation frames: "+output);
 }finally{RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cg);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
}

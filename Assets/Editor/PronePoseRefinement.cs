using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
public static class PronePoseRefinement {
 [MenuItem("Tools/Characters/Ground Prone Pose")]
 static void Ground(){
 const string path="Assets/DungeonTavern/Gameplay/Animations/";
 var prone=AssetDatabase.LoadAssetAtPath<AnimationClip>(path+"Prone.anim");var wake=AssetDatabase.LoadAssetAtPath<AnimationClip>(path+"WakeUp.anim");
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Characters/Protagonist/Protagonist.prefab"));
 try{
 var animator=go.GetComponent<Animator>();
 var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,prone);playable.SetApplyFootIK(false);AnimationPlayableOutput.Create(graph,"ground",animator).SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);float min=float.PositiveInfinity;
 foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()){
 var mesh=new Mesh();skin.BakeMesh(mesh,false);var scale=skin.transform.lossyScale;
 foreach(var v in mesh.vertices)min=Mathf.Min(min,skin.transform.TransformPoint(new Vector3(v.x/scale.x,v.y/scale.y,v.z/scale.z)).y);
 UnityEngine.Object.DestroyImmediate(mesh);
 }
 graph.Destroy();float delta=(.003f-min)/animator.humanScale;
 var b=EditorCurveBinding.FloatCurve("",typeof(Animator),"RootT.y");
 foreach(var clip in new[]{prone,wake}){var c=AnimationUtility.GetEditorCurve(clip,b);var keys=c.keys;for(int i=0;i<keys.Length;i++){float t=clip==prone?0:keys[i].time/clip.length;keys[i].value+=delta*(1-t)*(1-t);if(clip==wake){keys[i].inTangent-=2*delta*(1-t)/clip.length;keys[i].outTangent-=2*delta*(1-t)/clip.length;}}c.keys=keys;AnimationUtility.SetEditorCurve(clip,b,c);EditorUtility.SetDirty(clip);}
 AssetDatabase.SaveAssets();Debug.Log("Prone ground clearance corrected by "+(.003f-min)+" m");
 }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [MenuItem("Tools/Characters/Refine Prone Pose")]
 static void Run(){
 const string folder="Assets/DungeonTavern/Gameplay/Animations/";
 var prone=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Prone.anim");
 var wake=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"WakeUp.anim");
 // Keep the end of WakeUp unchanged so its transition into GetUp stays intact.
 var values=new Dictionary<string,float>{{"Left Foot Up-Down",.8f},{"Right Foot Up-Down",.8f},{"Head Turn Left-Right",.9f},{"Head Nod Down-Up",.75f},{"Head Tilt Left-Right",.02f},{"Left Arm Down-Up",.42f},{"Left Arm Front-Back",.45f},{"Left Arm Twist In-Out",-.2f},{"Left Forearm Stretch",.35f},{"Right Arm Down-Up",.15f},{"Right Arm Front-Back",.4f},{"Right Arm Twist In-Out",.35f},{"Right Forearm Stretch",.2f},{"Left Upper Leg In-Out",.12f},{"Right Upper Leg In-Out",.07f}};
 foreach(var clip in new[]{prone,wake}){var backup=Path.Combine(Path.GetTempPath(),clip.name+"-before-refinement.anim");if(!File.Exists(backup))File.Copy(AssetDatabase.GetAssetPath(clip),backup);Undo.RecordObject(clip,"Refine grounded prone pose");}
 foreach(var pair in values){
 var b=EditorCurveBinding.FloatCurve("",typeof(Animator),pair.Key);
 var old=AnimationUtility.GetEditorCurve(prone,b);float delta=pair.Value-old.Evaluate(0);
 AnimationUtility.SetEditorCurve(prone,b,AnimationCurve.Constant(0,prone.length,pair.Value));
 var c=AnimationUtility.GetEditorCurve(wake,b);var keys=c.keys;
 for(int i=0;i<keys.Length;i++){float t=keys[i].time/wake.length;keys[i].value+=delta*(1-t)*(1-t);keys[i].inTangent+=-2*delta*(1-t)/wake.length;keys[i].outTangent+=-2*delta*(1-t)/wake.length;}
 c.keys=keys;AnimationUtility.SetEditorCurve(wake,b,c);
 if(Mathf.Abs(c.Evaluate(0)-pair.Value)>.001f)throw new Exception("Prone/WakeUp mismatch: "+pair.Key);
 }
 EditorUtility.SetDirty(prone);EditorUtility.SetDirty(wake);AssetDatabase.SaveAssets();Debug.Log("Prone refined; WakeUp start matched and final pose preserved.");
 }
}

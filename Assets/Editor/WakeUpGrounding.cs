using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class WakeUpGrounding {
 static bool correct;
 static bool entireGetUp;
 public static void CorrectEntireGetUp(){entireGetUp=true;correct=true;try{Check();correct=false;Check();}finally{correct=false;entireGetUp=false;}}
 const string Folder="Assets/DungeonTavern/Gameplay/Animations/";
 [MenuItem("Tools/Characters/Fix WakeUp Grounding")]
 public static void Run(){
 var prone=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Prone.anim");var wake=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"WakeUp.anim");var rise=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"GetUp.anim");
 foreach(var clip in new[]{wake,rise}){var backup=Path.Combine(Path.GetTempPath(),clip.name+"-before-grounded-wake.anim");if(!File.Exists(backup))File.Copy(AssetDatabase.GetAssetPath(clip),backup);Undo.RecordObject(clip,"Ground awakening legs");}
 foreach(var b in AnimationUtility.GetCurveBindings(prone)){
 string n=b.propertyName;if(!(n.StartsWith("Root")||n.Contains("Leg")||n.Contains("Foot")||n.Contains("Toes")))continue;
 var target=AnimationUtility.GetEditorCurve(prone,b).Evaluate(0);AnimationUtility.SetEditorCurve(wake,b,AnimationCurve.Constant(0,wake.length,target));
 var original=AnimationUtility.GetEditorCurve(rise,b);if(original==null)continue;
 float delta=target-original.Evaluate(0);var keys=new Keyframe[(int)Math.Ceiling(rise.length*60)+1];
 for(int i=0;i<keys.Length;i++){float t=Mathf.Min(i/60f,rise.length);float u=Mathf.Clamp01(t/.7f);float fade=1-u*u*(3-2*u);keys[i]=new Keyframe(t,original.Evaluate(t)+delta*fade);}
 var curve=new AnimationCurve(keys);for(int i=0;i<curve.length;i++)curve.SmoothTangents(i,0);AnimationUtility.SetEditorCurve(rise,b,curve);
 }
 EditorUtility.SetDirty(wake);EditorUtility.SetDirty(rise);AssetDatabase.SaveAssets();correct=true;Check();correct=false;Check();
 }
 [MenuItem("Tools/Characters/Check WakeUp Grounding")]
 public static void Check(){
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Characters/Protagonist/Protagonist.prefab"));var animator=go.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
 var mesh=new Mesh();var report="";
 try{foreach(var name in new[]{"Prone","WakeUp","GetUp"}){
 var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+name+".anim");var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(false);AnimationPlayableOutput.Create(graph,"check",animator).SetSourcePlayable(p);graph.Play();
 float min=float.PositiveInfinity,max=float.NegativeInfinity;var heightBinding=EditorCurveBinding.FloatCurve("",typeof(Animator),"RootT.y");var height=AnimationUtility.GetEditorCurve(clip,heightBinding);var corrected=new Keyframe[121];
 try{for(int frame=0;frame<=120;frame++){p.SetTime(clip.length*frame/120.0);graph.Evaluate(0);float low=float.PositiveInfinity;
 foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.BakeMesh(mesh,false);var vertices=mesh.vertices;var weights=skin.sharedMesh.boneWeights;var scale=skin.transform.lossyScale;
 for(int i=0;i<vertices.Length;i++){var w=weights[i];bool boot=(skin.bones[w.boneIndex0].name.StartsWith("Foot")&&w.weight0>.25f)||(skin.bones[w.boneIndex1].name.StartsWith("Foot")&&w.weight1>.25f);if(!boot)continue;var v=vertices[i];low=Mathf.Min(low,skin.transform.TransformPoint(new Vector3(v.x/scale.x,v.y/scale.y,v.z/scale.z)).y);}}
 min=Mathf.Min(min,low);max=Mathf.Max(max,low);float time=clip.length*frame/120f;float amount=name=="WakeUp"?Mathf.Max(0,-.00038f-low):name=="GetUp"?Mathf.Max(0,-.00038f-low)*(entireGetUp?1:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1.1f,time))):0;corrected[frame]=new Keyframe(time,height.Evaluate(time)+amount/animator.humanScale);
 }}finally{graph.Destroy();}if(name!="Prone"&&correct){var curve=new AnimationCurve(corrected);for(int i=0;i<curve.length;i++)curve.SmoothTangents(i,0);AnimationUtility.SetEditorCurve(clip,heightBinding,curve);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();}report+=name+" boot bottom min="+min+" max="+max+"\n";
 }File.WriteAllText("/tmp/wakeup-grounding.txt",report);Debug.Log(report);
 }finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(go);}
 }
}

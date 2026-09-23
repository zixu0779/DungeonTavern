using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class GetUpRefinement {
 [MenuItem("Tools/Characters/Refine GetUp")]
 public static void Run(){
 const string folder="Assets/DungeonTavern/Gameplay/Animations/";
 var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"GetUp.anim");var wake=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"WakeUp.anim");
 var backup=Path.Combine(Path.GetTempPath(),"GetUp-before-fluid-rise.anim");if(!File.Exists(backup))File.Copy(AssetDatabase.GetAssetPath(clip),backup);
 Undo.RecordObject(clip,"Soften get-up motion");
 // Keep duration and endpoints; slow the initial weight transfer and final settling.
 var timing=new AnimationCurve(new Keyframe(0,0,.65f,.65f),new Keyframe(.25f,.17f,.9f,.9f),new Keyframe(.65f,.72f,1.2f,1.2f),new Keyframe(1,1,.35f,.35f));
 foreach(var binding in AnimationUtility.GetCurveBindings(clip)){
 var source=AnimationUtility.GetEditorCurve(clip,binding);var start=AnimationUtility.GetEditorCurve(wake,binding);var keys=new Keyframe[166];
 for(int i=0;i<keys.Length;i++){
 float u=i/(float)(keys.Length-1),t=u*clip.length;float value=source.Evaluate(timing.Evaluate(u)*clip.length);
 if(start!=null){float fade=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.38f));value+=(start.Evaluate(wake.length)-source.Evaluate(0))*fade;}
 // A small torso/head lag makes extension less mechanically simultaneous.
 float envelope=Mathf.Sin(Mathf.PI*u);envelope*=envelope;
 if(binding.propertyName=="Chest Front-Back")value-=.06f*envelope*Mathf.Sin(Mathf.PI*u);
 if(binding.propertyName=="Head Nod Down-Up")value-=.04f*envelope;
 keys[i]=new Keyframe(t,value);
 }
 var curve=new AnimationCurve(keys);for(int i=0;i<curve.length;i++)curve.SmoothTangents(i,0);AnimationUtility.SetEditorCurve(clip,binding,curve);
 }
 EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();WakeUpGrounding.CorrectEntireGetUp();
 }
}

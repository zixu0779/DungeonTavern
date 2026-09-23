using UnityEngine;
using UnityEditor;
public static class ProneLegSpacing {
 [MenuItem("Tools/Characters/Separate Prone Left Leg")]
 public static void Run(){
 const string folder="Assets/DungeonTavern/Gameplay/Animations/";
 var prone=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Prone.anim");var wake=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"WakeUp.anim");var binding=EditorCurveBinding.FloatCurve("",typeof(Animator),"Left Upper Leg In-Out");
 var previous=AnimationUtility.GetEditorCurve(prone,binding).Evaluate(0);const float target=.2f;float delta=target-previous;
 Undo.RecordObject(prone,"Separate prone left leg");Undo.RecordObject(wake,"Match prone left leg");
 AnimationUtility.SetEditorCurve(prone,binding,AnimationCurve.Constant(0,prone.length,target));var curve=AnimationUtility.GetEditorCurve(wake,binding);var keys=curve.keys;for(int i=0;i<keys.Length;i++){float u=keys[i].time/wake.length;float fade=1-u*u*(3-2*u);keys[i].value+=delta*fade;keys[i].inTangent+=delta*(-6*u+6*u*u)/wake.length;keys[i].outTangent+=delta*(-6*u+6*u*u)/wake.length;}curve.keys=keys;AnimationUtility.SetEditorCurve(wake,binding,curve);EditorUtility.SetDirty(prone);EditorUtility.SetDirty(wake);AssetDatabase.SaveAssets();
 }
}

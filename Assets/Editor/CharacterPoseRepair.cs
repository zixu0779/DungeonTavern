using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections.Generic;
public static class CharacterPoseRepair {
 [MenuItem("Tools/Characters/Repair Locomotion and GetUp")]
 public static void Run(){
 const string art="Assets/DungeonTavern/Art/Characters/Protagonist/";const string actions="Assets/DungeonTavern/Gameplay/Animations/";
 var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(art+"Protagonist.controller");var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(art+"Idle.anim");
 var walkState=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Walk").state;var walk=AssetDatabase.LoadAssetAtPath<AnimationClip>(art+"Walk.anim");if(!walk){walk=Object.Instantiate((AnimationClip)walkState.motion);walk.name="Walk";AssetDatabase.CreateAsset(walk,art+"Walk.anim");walkState.motion=walk;EditorUtility.SetDirty(walkState);}
 foreach(var side in new[]{"Left","Right"}){
 var binding=EditorCurveBinding.FloatCurve("",typeof(Animator),side+" Arm Front-Back");float offset=AnimationUtility.GetEditorCurve(idle,binding).Evaluate(0)-.23f;
 foreach(var clip in new[]{idle,walk}){Undo.RecordObject(clip,"Correct arm plane");var curve=AnimationUtility.GetEditorCurve(clip,binding);var keys=curve.keys;for(int i=0;i<keys.Length;i++)keys[i].value-=offset;curve.keys=keys;AnimationUtility.SetEditorCurve(clip,binding,curve);EditorUtility.SetDirty(clip);}
 var stretch=EditorCurveBinding.FloatCurve("",typeof(Animator),side+" Forearm Stretch");
 AnimationUtility.SetEditorCurve(idle,stretch,AnimationCurve.Constant(0,idle.length,.9f));
 var wc=AnimationUtility.GetEditorCurve(walk,stretch);var wk=wc.keys;for(int i=0;i<wk.Length;i++)wk[i].value=Mathf.Min(1,wk[i].value+.4f);wc.keys=wk;AnimationUtility.SetEditorCurve(walk,stretch,wc);
 }
 var targetPose=new Dictionary<string,float>();
 var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(art+"Protagonist.prefab"));var animator=model.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
 var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,idle);playable.SetApplyFootIK(true);AnimationPlayableOutput.Create(graph,"idle",animator).SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
 using(var handler=new HumanPoseHandler(animator.avatar,model.transform)){var pose=new HumanPose();handler.GetHumanPose(ref pose);for(int i=0;i<HumanTrait.MuscleCount;i++)targetPose[HumanTrait.MuscleName[i]]=pose.muscles[i];targetPose["RootT.x"]=pose.bodyPosition.x;targetPose["RootT.y"]=pose.bodyPosition.y;targetPose["RootT.z"]=pose.bodyPosition.z;targetPose["RootQ.x"]=pose.bodyRotation.x;targetPose["RootQ.y"]=pose.bodyRotation.y;targetPose["RootQ.z"]=pose.bodyRotation.z;targetPose["RootQ.w"]=pose.bodyRotation.w;}graph.Destroy();Object.DestroyImmediate(model);
 var rise=AssetDatabase.LoadAssetAtPath<AnimationClip>(actions+"GetUp.anim");Undo.RecordObject(rise,"Settle into actual Idle pose");
 foreach(var binding in AnimationUtility.GetCurveBindings(rise)){
 var target=AnimationUtility.GetEditorCurve(idle,binding);if(target==null)continue;var source=AnimationUtility.GetEditorCurve(rise,binding);var keys=new Keyframe[166];
 for(int i=0;i<keys.Length;i++){float t=rise.length*i/(keys.Length-1);float u=Mathf.Clamp01((t-(rise.length-.85f))/.85f);float blend=u*u*u*(u*(u*6-15)+10);keys[i]=new Keyframe(t,Mathf.Lerp(source.Evaluate(t),targetPose.TryGetValue(binding.propertyName,out var sampled)?sampled:target.Evaluate(0),blend));}
 var curve=new AnimationCurve(keys);for(int i=0;i<curve.length;i++)curve.SmoothTangents(i,0);AnimationUtility.SetEditorCurve(rise,binding,curve);
 }
 EditorUtility.SetDirty(rise);
 var hold=AssetDatabase.LoadAssetAtPath<AnimationClip>(actions+"HoldCup.anim");Undo.RecordObject(hold,"Remove unrelated held-cup body motion");
 foreach(var binding in AnimationUtility.GetCurveBindings(hold)){
 var n=binding.propertyName;if(n.StartsWith("Right Arm")||n.StartsWith("Right Forearm")||n.StartsWith("Right Hand")||n.StartsWith("Right Shoulder")||n.StartsWith("RightHand"))continue;
 if(binding.type!=typeof(Animator))continue;var neutral=AnimationUtility.GetEditorCurve(idle,binding);if(neutral!=null)AnimationUtility.SetEditorCurve(hold,binding,AnimationCurve.Constant(0,hold.length,neutral.Evaluate(0)));else AnimationUtility.SetEditorCurve(hold,binding,null);
 }EditorUtility.SetDirty(hold);AssetDatabase.SaveAssets();CharacterPoseAudit.Run();
 }
}

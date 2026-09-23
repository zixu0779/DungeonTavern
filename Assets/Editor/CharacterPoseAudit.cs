using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
public static class CharacterPoseAudit {
 [MenuItem("Tools/Characters/Audit Arm Poses")]
 public static void Run(){
 var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Characters/Protagonist/Protagonist.prefab"));var a=go.GetComponent<Animator>();a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.Rebind();var controller=(AnimatorController)a.runtimeAnimatorController;string report="";
 foreach(var name in new[]{"Idle","Walk","GetUp"}){
 var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name==name).state;var clip=(AnimationClip)state.motion;var g=PlayableGraph.Create();g.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(g,clip);p.SetApplyFootIK(state.iKOnFeet);AnimationPlayableOutput.Create(g,"audit",a).SetSourcePlayable(p);g.Play();p.SetTime(name=="GetUp"?clip.length:0);g.Evaluate(0);
 foreach(var id in new[]{HumanBodyBones.Hips,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.LeftFoot})report+=name+" "+id+" "+a.GetBoneTransform(id).position.ToString("F4")+"\n";g.Destroy();
 }
 Object.DestroyImmediate(go);File.WriteAllText("/tmp/arm-pose-audit.txt",report);Debug.Log(report);
 }
}

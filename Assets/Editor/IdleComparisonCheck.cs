using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class IdleComparisonCheck {
 [MenuItem("Tools/Characters/Fix and Verify Idle Feet")]
 public static void Fix(){
 const string folder="Assets/DungeonTavern/Art/Characters/Protagonist/";
 var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"Protagonist.controller");
 var idle=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Idle").state;
 Undo.RecordObject(idle,"Enable Idle foot IK");idle.iKOnFeet=true;EditorUtility.SetDirty(idle);AssetDatabase.SaveAssets();
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"Protagonist.prefab"));
 var graph=PlayableGraph.Create("Idle controller verification");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
 try{
 var animator=go.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
 var playable=AnimatorControllerPlayable.Create(graph,controller);AnimationPlayableOutput.Create(graph,"Animator",animator).SetSourcePlayable(playable);graph.Play();
 var feet=new[]{animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot)};
 var min=new[]{float.PositiveInfinity,float.PositiveInfinity};var max=new[]{float.NegativeInfinity,float.NegativeInfinity};
 for(int frame=0;frame<180;frame++){
 playable.Play("Base Layer.Idle",0,frame/60f);graph.Evaluate(0);
 for(int j=0;j<2;j++){min[j]=Mathf.Min(min[j],feet[j].position.y);max[j]=Mathf.Max(max[j],feet[j].position.y);}
 }
 var drift=Mathf.Max(max[0]-min[0],max[1]-min[1]);
 if(drift>.001f)throw new Exception("Idle feet drift exceeds 1 mm: "+drift);
 Debug.Log("Idle controller verification PASS; maximum feet drift="+drift+" m over 3 cycles.");
 }finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(go);}
 }
 [MenuItem("Tools/Characters/Compare Idle Motion")]
 public static void Run(){
 var report=new StringBuilder();
 const string root="Assets/DungeonTavern/Art/Characters/";
 var mage=(AnimatorController)AssetDatabase.LoadAssetAtPath<AnimatorController>(root+"ThirdParty/Mage/Mage.controller");
 var original=(AnimationClip)mage.layers[0].stateMachine.states.Single(s=>s.state.name=="Idle").state.motion;
 var edited=AssetDatabase.LoadAssetAtPath<AnimationClip>(root+"Protagonist/Idle.anim");
 report.AppendLine($"Original {AssetDatabase.GetAssetPath(original)} / {original.name} length={original.length}; protagonist length={edited.length}");
 foreach(var b in AnimationUtility.GetCurveBindings(edited)){
 var a=AnimationUtility.GetEditorCurve(original,b); var c=AnimationUtility.GetEditorCurve(edited,b);
 if(a==null||a.keys.Length!=c.keys.Length||a.keys.Where((k,i)=>Mathf.Abs(k.value-c.keys[i].value)>0.00001f).Any())report.AppendLine("Changed curve: "+b.propertyName);
 }
 foreach(var path in new[]{"ThirdParty/Mage/Mage.prefab","Protagonist/Protagonist.prefab"})
 foreach(var clip in new[]{original,edited})
 foreach(var footIK in new[]{false,true}){
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(root+path));
 var animator=go.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
 var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
 try{
 var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(footIK);
 AnimationPlayableOutput.Create(graph,"check",animator).SetSourcePlayable(p);graph.Play();
 var ids=new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot};
 var min=ids.Select(_=>float.PositiveInfinity).ToArray();var max=ids.Select(_=>float.NegativeInfinity).ToArray();
 for(int i=0;i<=60;i++){p.SetTime(clip.length*i/60.0);graph.Evaluate(0);for(int j=0;j<ids.Length;j++){float y=animator.GetBoneTransform(ids[j]).position.y;min[j]=Mathf.Min(min[j],y);max[j]=Mathf.Max(max[j],y);}}
 report.AppendLine(path+" footIK="+footIK+" clip="+clip.name+" "+string.Join(", ",ids.Select((id,j)=>id+" deltaY="+(max[j]-min[j]).ToString("F5"))));
 }finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(go);}
 }
 File.WriteAllText("/tmp/idle-comparison.txt",report.ToString());Debug.Log(report.ToString());
 }
}

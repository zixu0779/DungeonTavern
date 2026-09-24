using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using DungeonTavern.Tavern25D;
public static class OpeningTransitionAudit
{
 [MenuItem("Tools/Characters/Audit Opening Transition")]
 public static void Run()
 {
  var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Characters/Protagonist/Protagonist.prefab"));
  try {
   go.transform.position=new Vector3(1000,1000,1000);
   var a=go.GetComponent<Animator>();var m=go.AddComponent<CharacterModelMotion>();
   typeof(CharacterModelMotion).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null);
   a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.Rebind();a.Update(0);m.BeginProne();a.Update(0);
   var sb=new StringBuilder();
   foreach(var side in new[]{"Left","Right"}){
    var upper=a.GetBoneTransform((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),side+"UpperArm"));var lower=a.GetBoneTransform((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),side+"LowerArm"));var hand=a.GetBoneTransform((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),side+"Hand"));
    sb.AppendLine(side+" bend="+Vector3.Angle(lower.position-upper.position,hand.position-lower.position)+" upperRot="+upper.localEulerAngles+" lowerRot="+lower.localEulerAngles+" handRot="+hand.localEulerAngles);
   }
   var rise=m.WakeAndStand();bool running=rise.MoveNext();bool completed=false;var head=a.GetBoneTransform(HumanBodyBones.Head);var previous=head.position;float maxStep=0;
   var bones=go.GetComponentsInChildren<Transform>(); var last=bones.Select(b=>b.position).ToArray();
   sb.AppendLine("frame,time,state,normalized,transition,next,fullBody,headStep");
   for(int i=0;i<330;i++){
    a.Update(1f/60);bool before=m.IsFullBodyAction;
    if(running)running=rise.MoveNext();
    var s=a.GetCurrentAnimatorStateInfo(0);var n=a.GetNextAnimatorStateInfo(0);float step=Vector3.Distance(previous,head.position);previous=head.position;
    float largest=0; string boneName=""; for(int j=0;j<bones.Length;j++){float d=Vector3.Distance(last[j],bones[j].position);if(d>largest){largest=d;boneName=bones[j].name;}last[j]=bones[j].position;}
    if(i>=237 && i<=255 && largest>.02f)throw new Exception($"End transition bone jump: {boneName} {largest}");
    if(i>=237 && i<=255)sb.AppendLine($"BONE {i} {boneName} {largest:F6}");
    if(completed && a.IsInTransition(0))throw new Exception("Idle restarted after completion");
    if(i>210)maxStep=Mathf.Max(maxStep,step);
    if(i>200)sb.AppendLine($"{i},{i/60f:F3},{(s.IsName("Idle")?"Idle":s.IsName("GetUp")?"GetUp":"Other")},{s.normalizedTime:F4},{a.IsInTransition(0)},{(n.IsName("Idle")?"Idle":"Other")},{m.IsFullBodyAction},{step:F5}");
    if(before&&!m.IsFullBodyAction){completed=true;sb.AppendLine("COMPLETION transition="+a.IsInTransition(0)+" layer="+a.GetLayerWeight(1));}
   }
   if(!completed)throw new Exception("WakeAndStand never finished");
   sb.AppendLine("MAX_END_HEAD_STEP="+maxStep);
   File.WriteAllText(Path.Combine(Path.GetTempPath(),"opening-transition-audit.csv"),sb.ToString());Debug.Log("Opening transition audit: "+Path.Combine(Path.GetTempPath(),"opening-transition-audit.csv"));
  } finally {UnityEngine.Object.DestroyImmediate(go);}
 }
}

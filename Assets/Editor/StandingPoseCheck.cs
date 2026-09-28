using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class StandingPoseCheck
{
 const string Root="Assets/DungeonTavern/Art/Characters/";
 [MenuItem("Tools/Characters/Check Standing Pose")]
 public static void Audit(){
 var log=new StringBuilder();
 foreach(var name in new[]{"Protagonist","Eve"}){
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+name+"/"+name+".prefab"));
 try{var a=go.GetComponent<Animator>();a.Rebind();a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
 Log(log,a,name+" Rest");
 float restWidth=Mathf.Abs(a.GetBoneTransform(HumanBodyBones.LeftFoot).position.x-a.GetBoneTransform(HumanBodyBones.RightFoot).position.x);
 float restHips=a.GetBoneTransform(HumanBodyBones.Hips).position.y;
 float idleHips=float.NaN; Quaternion chestStart=Quaternion.identity; float chestMotion=0;
 using(var handler=new HumanPoseHandler(a.avatar,a.transform)){var pose=new HumanPose();handler.GetHumanPose(ref pose);log.AppendLine("Rest body "+pose.bodyPosition.ToString("F5"));for(int i=0;i<HumanTrait.MuscleCount;i++)if(Leg(HumanTrait.MuscleName[i]))log.AppendLine(HumanTrait.MuscleName[i]+"="+pose.muscles[i]);}
 var c=(AnimatorController)a.runtimeAnimatorController;
 foreach(var state in c.layers[0].stateMachine.states.Select(s=>s.state)){
 if(!(state.motion is AnimationClip clip))continue;
 var g=PlayableGraph.Create();g.SetTimeUpdateMode(DirectorUpdateMode.Manual);
 try{var p=AnimationClipPlayable.Create(g,clip);p.SetApplyFootIK(state.iKOnFeet);AnimationPlayableOutput.Create(g,"pose",a).SetSourcePlayable(p);g.Play();
 foreach(var t in new[]{0f,.25f,.5f,.75f,1f}){p.SetTime(clip.length*t);g.Evaluate(.00001f);Log(log,a,name+" "+state.name+" "+t);
 bool standing=state.name=="Idle"||(state.name=="GetUp"||state.name=="StandUp")&&t==1||state.name=="SitDown"&&t==0;
 if(standing){float width=Mathf.Abs(a.GetBoneTransform(HumanBodyBones.LeftFoot).position.x-a.GetBoneTransform(HumanBodyBones.RightFoot).position.x);if(Mathf.Abs(width-restWidth)>.003f)throw new Exception(name+" "+state.name+" standing feet are too wide");}
 if(state.name=="Idle"){var chest=a.GetBoneTransform(HumanBodyBones.Chest); if(t==0)chestStart=chest.localRotation; else {float angle=Quaternion.Angle(chestStart,chest.localRotation);chestMotion=Mathf.Max(chestMotion,angle);if(t==1&&angle>.02f)throw new Exception(name+" Idle loop discontinuity");} float hips=a.GetBoneTransform(HumanBodyBones.Hips).position.y;if(float.IsNaN(idleHips)){idleHips=hips;if(Mathf.Abs(hips-restHips)>.0001f)throw new Exception(name+" Idle height differs from rest pose");}else if(Mathf.Abs(hips-idleHips)>.001f)throw new Exception(name+" Idle bobs vertically");}}
 }finally{g.Destroy();}
 }
 if(chestMotion<.05f||chestMotion>2.5f)throw new Exception(name+" breathing rotation out of range: "+chestMotion);log.AppendLine(name+" breathing chest rotation="+chestMotion.ToString("F4")+" degrees; loop continuous");
 }finally{UnityEngine.Object.DestroyImmediate(go);}}
 File.WriteAllText("/tmp/standing-pose-audit.txt",log.ToString());Debug.Log("Standing pose checks PASS; report /tmp/standing-pose-audit.txt");
 }
 static bool Leg(string n)=>n.Contains("Leg")||n.Contains("Foot")||n.Contains("Toes");
 static void Log(StringBuilder s,Animator a,string label){
 Vector3 P(HumanBodyBones b)=>a.transform.InverseTransformPoint(a.GetBoneTransform(b).position);
 float Knee(HumanBodyBones u,HumanBodyBones l,HumanBodyBones f)=>180-Vector3.Angle(P(u)-P(l),P(f)-P(l));
 s.AppendLine($"{label}: footWidth={Mathf.Abs(P(HumanBodyBones.LeftFoot).x-P(HumanBodyBones.RightFoot).x):F5} hipsY={P(HumanBodyBones.Hips).y:F5} headY={P(HumanBodyBones.Head).y:F5} kneeL={Knee(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot):F2} kneeR={Knee(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot):F2}");
 }
}

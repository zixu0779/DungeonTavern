using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
public static class IdleRuntimeAudit {
 [MenuItem("Tools/Characters/Audit Continuous Idle")]
 public static void Run(){var report=new StringBuilder();
 foreach(var name in new[]{"Protagonist","Eve"}){
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Characters/"+name+"/"+name+".prefab"));
 try{var a=go.GetComponent<Animator>();a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.Rebind();a.Play("Idle",0,0);a.Update(0);
 var foot=a.GetBoneTransform(HumanBodyBones.LeftFoot).position;float hips=a.GetBoneTransform(HumanBodyBones.Hips).position.y;float baseHead=a.GetBoneTransform(HumanBodyBones.Head).position.y,maxRise=0;
 for(int i=0;i<540;i++){a.Update(1f/60);maxRise=Mathf.Max(maxRise,a.GetBoneTransform(HumanBodyBones.Head).position.y-baseHead);if(Vector3.Distance(foot,a.GetBoneTransform(HumanBodyBones.LeftFoot).position)>.0001f||Mathf.Abs(a.GetBoneTransform(HumanBodyBones.Hips).position.y-hips)>.0001f)throw new Exception(name+" lower body moved during breathing");}
 if(maxRise<.03f||maxRise>.034f)throw new Exception(name+" breathing rise outside expected range");
 a.SetFloat("Speed",3.25f);foreach(var p in a.parameters)if(p.name=="WalkRate")a.SetFloat("WalkRate",1);
 for(int i=0;i<60;i++){a.Update(1f/60);}
 if(!a.GetCurrentAnimatorStateInfo(0).IsName("Walk"))throw new Exception(name+" Walk transition failed");
 var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DungeonTavern/Art/Characters/"+name+"/Idle.anim");
 a.SetFloat("Speed",0);a.Play("Idle",0,.5f);a.Update(0);var expectedHead=a.GetBoneTransform(HumanBodyBones.Head).position;
 var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);try{var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);AnimationPlayableOutput.Create(graph,"direct clip preview",a).SetSourcePlayable(playable);graph.Play();playable.SetTime(clip.length*.5);graph.Evaluate(0);if(Vector3.Distance(expectedHead,a.GetBoneTransform(HumanBodyBones.Head).position)>.0001f)throw new Exception(name+" direct clip preview differs from controller");}finally{graph.Destroy();}
 report.AppendLine(name+" PASS: two Idle cycles, rise="+maxRise+", stable hips/feet, Walk transition and standalone clip/controller pose match");
 }finally{UnityEngine.Object.DestroyImmediate(go);}}
 File.WriteAllText("/tmp/continuous-idle-check.txt",report.ToString());Debug.Log(report.ToString());}
}

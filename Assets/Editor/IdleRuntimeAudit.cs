using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class IdleRuntimeAudit {
 [MenuItem("Tools/Characters/Audit Continuous Idle")]
 public static void Run(){var s=new StringBuilder("name,frame,rootY,hipsY,footY,headY,chestAngle\n");
 foreach(var name in new[]{"Protagonist","Eve"}){
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Characters/"+name+"/"+name+".prefab"));
 var a=go.GetComponent<Animator>();a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.Rebind();
 var g=PlayableGraph.Create();g.SetTimeUpdateMode(DirectorUpdateMode.Manual);
 try{var p=AnimatorControllerPlayable.Create(g,a.runtimeAnimatorController);AnimationPlayableOutput.Create(g,"audit",a).SetSourcePlayable(p);g.Play();p.Play("Idle",0,0);Quaternion q=Quaternion.identity; float minH=float.PositiveInfinity,maxH=float.NegativeInfinity,minF=float.PositiveInfinity,maxF=float.NegativeInfinity,maxChest=0;
 for(int i=0;i<=540;i++){g.Evaluate(1f/60);var chest=a.GetBoneTransform(HumanBodyBones.Chest);if(i==0)q=chest.localRotation;float h=a.GetBoneTransform(HumanBodyBones.Hips).position.y,f=a.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;minH=Mathf.Min(minH,h);maxH=Mathf.Max(maxH,h);minF=Mathf.Min(minF,f);maxF=Mathf.Max(maxF,f);maxChest=Mathf.Max(maxChest,Quaternion.Angle(q,chest.localRotation));s.AppendLine($"{name},{i},{go.transform.position.y:F6},{a.GetBoneTransform(HumanBodyBones.Hips).position.y:F6},{a.GetBoneTransform(HumanBodyBones.LeftFoot).position.y:F6},{a.GetBoneTransform(HumanBodyBones.Head).position.y:F6},{Quaternion.Angle(q,chest.localRotation):F6}");}
 if(maxH-minH>.00002f||maxF-minF>.0005f||maxChest<1.5f||maxChest>2.5f)throw new Exception(name+" Idle body stability or breathing amplitude failed");
 }finally{g.Destroy();UnityEngine.Object.DestroyImmediate(go);}}
 File.WriteAllText("/tmp/continuous-idle.csv",s.ToString());Debug.Log("Continuous Idle stability and breathing checks PASS");}
}

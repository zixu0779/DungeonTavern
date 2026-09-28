using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class CharacterSizeCheck {
 const string Root="Assets/DungeonTavern/Art/Characters/";
 [MenuItem("Tools/Characters/Audit Character Sizes")]
 public static void Audit(){var s=new StringBuilder();
 foreach(var n in new[]{"Protagonist/Protagonist","Eve/Eve","ThirdParty/Mage/Mage","ThirdParty/Rogue/Rogue","ThirdParty/Barbarian/Barbarian"}){
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+n+".prefab"));try{var a=go.GetComponentInChildren<Animator>();a.Rebind();s.AppendLine(n+" scale="+go.transform.localScale+" rest="+Bounds(a));if(a.runtimeAnimatorController){var clip=a.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name=="Idle");if(clip){Sample(a,clip,0);s.AppendLine(" idle="+Bounds(a));}}}finally{UnityEngine.Object.DestroyImmediate(go);}}
 foreach(var path in new[]{"Assets/Scenes/Tavern/Tavern_Main.unity","Assets/Scenes/SealRoom/SealRoom_B1.unity"}){
 var scene=EditorSceneManager.OpenPreviewScene(path);try{foreach(var a in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Animator>(true))){if(!a.isHuman)continue;a.Rebind();var clip=a.runtimeAnimatorController.animationClips.First(c=>c.name=="Idle");Sample(a,clip,0);s.AppendLine("Scene visible height="+MeshBounds(a).size.y.ToString("F5"));s.AppendLine(path+" "+a.name+" controller="+AssetDatabase.GetAssetPath(a.runtimeAnimatorController)+" localScale="+a.transform.localScale+" worldScale="+a.transform.lossyScale+" localPos="+a.transform.localPosition);}}finally{EditorSceneManager.ClosePreviewScene(scene);}}
 File.WriteAllText("/tmp/character-size-audit.txt",s.ToString());Debug.Log("Size audit saved");}
 static string Bounds(Animator a){var hips=a.isHuman?a.GetBoneTransform(HumanBodyBones.Hips):a.transform;var head=a.isHuman?a.GetBoneTransform(HumanBodyBones.Head):a.transform;var bounds=MeshBounds(a);return $"hips={hips.position.y:F5} head={head.position.y:F5} min={bounds.min.y:F5} max={bounds.max.y:F5} height={bounds.size.y:F5}";}
 static Bounds MeshBounds(Animator a){var points=a.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(sk=>{var m=new Mesh();sk.BakeMesh(m);var scale=sk.transform.lossyScale;var vs=m.vertices.Select(v=>sk.transform.TransformPoint(Vector3.Scale(v,new Vector3(1/scale.x,1/scale.y,1/scale.z)))).ToArray();UnityEngine.Object.DestroyImmediate(m);return vs;}).ToArray();var b=new Bounds(points[0],Vector3.zero);foreach(var v in points)b.Encapsulate(v);return b;}
 static void Sample(Animator a,AnimationClip c,double t){a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;var g=PlayableGraph.Create();g.SetTimeUpdateMode(DirectorUpdateMode.Manual);try{var p=AnimationClipPlayable.Create(g,c);p.SetApplyFootIK(false);AnimationPlayableOutput.Create(g,"sample",a).SetSourcePlayable(p);g.Play();p.SetTime(t);g.Evaluate(0);}finally{g.Destroy();}}
}

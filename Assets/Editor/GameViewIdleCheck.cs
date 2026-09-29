using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class GameViewIdleCheck {
 const string Root="Assets/DungeonTavern/Art/Characters/";
 [MenuItem("Tools/Characters/Capture Game View Idle")]
 public static void Capture(){
 string folder=Path.Combine(Path.GetTempPath(),"game-view-idle");Directory.CreateDirectory(folder);
 var cameraGo=new GameObject("IdleGameViewCamera");var camera=cameraGo.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.5f;camera.transform.rotation=Quaternion.Euler(45,225,0);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.20f,.24f);
 var rt=new RenderTexture(1920,1080,24);var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);var previous=RenderTexture.active;camera.targetTexture=rt;string report="";
 try{foreach(var n in new[]{"Protagonist","Eve"}){
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+n+"/"+n+".prefab"));var a=go.GetComponent<Animator>();var g=PlayableGraph.Create();g.SetTimeUpdateMode(DirectorUpdateMode.Manual);var meshes=new System.Collections.Generic.List<Mesh>();
 try{go.transform.position=Vector3.zero;go.transform.localScale=Vector3.one*(n=="Protagonist"?1.10782f:1.154561f)*1.08f;foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=31;a.Rebind();a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.applyRootMotion=false;
 a.Play("Idle",0,0);a.Update(0);
 var skins=go.GetComponentsInChildren<SkinnedMeshRenderer>();var baked=skins.Select(sk=>{var child=new GameObject("baked");child.layer=31;child.transform.SetParent(sk.transform,false);var mf=child.AddComponent<MeshFilter>();mf.sharedMesh=new Mesh();meshes.Add(mf.sharedMesh);child.AddComponent<MeshRenderer>().sharedMaterials=sk.sharedMaterials;return mf;}).ToArray();
 camera.transform.position=go.transform.position+Vector3.up-camera.transform.forward*8;
 Vector3 startHead=Vector3.zero,startFoot=Vector3.zero;float maxHead=0,maxFoot=0;
 for(int i=0;i<32;i++){a.Play("Idle",0,i/32f);a.Update(0);
 var head=camera.WorldToScreenPoint(a.GetBoneTransform(HumanBodyBones.Head).position);var foot=a.GetBoneTransform(HumanBodyBones.LeftFoot).position;if(i==0){startHead=head;startFoot=foot;}maxHead=Mathf.Max(maxHead,Mathf.Abs(head.y-startHead.y));maxFoot=Mathf.Max(maxFoot,Vector3.Distance(foot,startFoot));if(i==0||i==16)report+=n+" frame="+i+" foot="+foot.ToString("F5")+" hips="+a.GetBoneTransform(HumanBodyBones.Hips).position.ToString("F5")+"\n";
 for(int k=0;k<skins.Length;k++){skins[k].BakeMesh(baked[k].sharedMesh);var scale=skins[k].transform.lossyScale;baked[k].transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);skins[k].enabled=false;}
 camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(folder+"/"+n+"-"+i.ToString("00")+".png",tex.EncodeToPNG());
 }
 if(maxHead<2.5f||maxHead>4.5f||maxFoot>.0001f)throw new Exception(n+" game-view Idle amplitude or feet failed: pixels="+maxHead+" feet="+maxFoot);report+=n+" head vertical pixels="+maxHead+" foot drift="+maxFoot+"\n";
 }finally{g.Destroy();foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);UnityEngine.Object.DestroyImmediate(go);}}
 File.WriteAllText(folder+"/check.txt",report);Debug.Log(report);
 }finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}}
}

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal static class PropAnimationAuthoring
{
 const string Models="Assets/DungeonTavern/Art/Models/";
 [MenuItem("Tools/Dungeon Tavern/Props/Check Chest And Sign Animations")]
 static void Check(){
 var report=new StringBuilder();
 foreach(var name in new[]{"Chest","TavernSign"}){
 var root=PrefabUtility.LoadPrefabContents(Models+name+"/"+name+".prefab");
 try{
 var pivot=root.transform.Find(name=="Chest"?"LidPivot":"BoardPivot");var stationary=root.transform.GetChild(0);if(stationary==pivot)stationary=root.transform.GetChild(1);
 var fixedPose=stationary.localToWorldMatrix;var resting=pivot.localPosition;
 var folder=Models+name+"/Animations/";var opening=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+name+"_Opening.anim");var closing=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+name+"_Closing.anim");
 opening.SampleAnimation(root,0);var initial=pivot.localRotation;opening.SampleAnimation(root,opening.length);var end=pivot.localRotation;var endP=pivot.localPosition;
 if(Mathf.Abs(Quaternion.Angle(initial,end)-(name=="Chest"?25:180))>.1f)throw new Exception("Incorrect opening angle: "+name);
 closing.SampleAnimation(root,0);if(Quaternion.Angle(pivot.localRotation,end)>.1f||Vector3.Distance(pivot.localPosition,endP)>.0001f)throw new Exception("Discontinuous open endpoint");
 closing.SampleAnimation(root,closing.length);if(Quaternion.Angle(pivot.localRotation,initial)>.1f||Vector3.Distance(pivot.localPosition,resting)>.0001f)throw new Exception("Closing endpoint drift");
 for(int k=0;k<16;k++)if(Mathf.Abs(fixedPose[k]-stationary.localToWorldMatrix[k])>.0001f)throw new Exception("Fixed part moved");
 var a=root.GetComponent<Animator>();a.Rebind();a.Update(0);a.SetBool("Open",true);for(int i=0;i<100;i++)a.Update(.02f);
 if(!a.GetCurrentAnimatorStateInfo(0).IsName("Open"))throw new Exception("Animator did not open: "+name);
 a.SetBool("Open",false);for(int i=0;i<100;i++)a.Update(.02f);if(!a.GetCurrentAnimatorStateInfo(0).IsName("Closed"))throw new Exception("Animator did not close: "+name);
 report.AppendLine("PASS "+name+": endpoints continuous; fixed part unchanged; Animator completed both directions.");
 Render(root,opening,0,name+"_Closed");Render(root,opening,opening.length,name+"_Open");if(name=="TavernSign"){Render(root,opening,opening.length*.5f,name+"_OpeningMid");Render(root,closing,closing.length*.5f,name+"_ClosingMid");}
 }finally{PrefabUtility.UnloadPrefabContents(root);}}
 File.WriteAllText("/tmp/dt-prop-animation-check.txt",report.ToString());
 }
 static void Render(GameObject root,AnimationClip clip,float time,string name){
 root.GetComponent<Animator>().enabled=false;clip.SampleAnimation(root,time);
 var scene=root.scene;var rs=root.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 var go=new GameObject("PreviewCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.24f,.27f);camera.orthographic=true;camera.orthographicSize=Mathf.Max(b.size.y,b.size.x)*.8f;camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.transform.position=b.center+new Vector3(3,2,-4).normalized*10;camera.transform.LookAt(b.center);go.AddComponent<UniversalAdditionalCameraData>().SetRenderer(1);
 var lg=new GameObject("PreviewLight");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lg,scene);var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;lg.transform.rotation=Quaternion.Euler(45,-35,0);
 var rt=new RenderTexture(768,768,24);rt.Create();var old=RenderTexture.active;var tex=new Texture2D(768,768,TextureFormat.RGB24,false);
 try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();Directory.CreateDirectory("ArtSource/Previews/PropAnimations");File.WriteAllBytes("ArtSource/Previews/PropAnimations/"+name+".png",tex.EncodeToPNG());}
 finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(lg);}
 }
}

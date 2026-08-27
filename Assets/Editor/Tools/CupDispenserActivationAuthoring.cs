using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonTavern.Tavern25D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal static class CupDispenserActivationAuthoring
{
 const string Root="Assets/DungeonTavern/Art/Models/CupDispenser/";
 const string Path=Root+"CupDispenser.prefab";
 const string Output="ArtSource/Previews/CupDispenserActivation/";
 static Bounds BoundsOf(GameObject root){var rs=root.GetComponentsInChildren<MeshRenderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}




 static void Invoke(CupDispenserActivation component,string method,params object[] args)=>typeof(CupDispenserActivation).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(component,args);
 [MenuItem("Tools/Dungeon Tavern/Cup Dispenser/Check And Preview")]
 static void Check(){var root=PrefabUtility.LoadPrefabContents(Path);try{var c=root.GetComponent<CupDispenserActivation>();var halo=root.transform.Find("CupDispenser_FloatingHalo");var rest=halo.localPosition;var scale=halo.localScale;Invoke(c,"OnEnable");Render(root,"Idle");c.SetActivated(true);for(int i=0;i<100;i++)Invoke(c,"Advance",.02f);float height=new SerializedObject(c).FindProperty("liftHeight").floatValue;if(Vector3.Distance(halo.localPosition,rest+Vector3.up*height)>.00001f||halo.localScale!=scale)throw new Exception("Active pose incorrect");var block=new MaterialPropertyBlock();halo.GetComponentInChildren<Renderer>().GetPropertyBlock(block);if(!halo.GetComponentInChildren<Renderer>().sharedMaterial.IsKeywordEnabled("_EMISSION") || block.GetColor("_EmissionColor").maxColorComponent<1)throw new Exception("Emission did not turn on");foreach(var ps in root.GetComponentsInChildren<ParticleSystem>()){if(ps.velocityOverLifetime.x.mode!=ps.velocityOverLifetime.z.mode)throw new Exception("Mismatched particle velocity modes");ps.Simulate(1.25f,true,true);var samples=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(samples);if(count==0 || !samples.Take(count).Any(p=>p.position.z>.1f))throw new Exception("Particles do not rise");}Render(root,"Active");c.SetActivated(false);for(int i=0;i<100;i++)Invoke(c,"Advance",.02f);if(Vector3.Distance(halo.localPosition,rest)>.00001f||root.GetComponentsInChildren<ParticleSystem>().Any(p=>p.particleCount!=0)||root.GetComponentInChildren<Light>().enabled)throw new Exception("Shutdown did not restore idle");c.SetActivated(true);for(int i=0;i<20;i++)Invoke(c,"Advance",.02f);c.SetActivated(false);for(int i=0;i<100;i++)Invoke(c,"Advance",.02f);if(halo.localPosition!=rest)throw new Exception("Interrupted transition drift");c.SetActivated(true);Invoke(c,"Advance",.5f);Invoke(c,"OnDisable");if(halo.localPosition!=rest)throw new Exception("Disable did not restore pose");Invoke(c,"OnEnable");c.SetActivated(false);for(int i=0;i<100;i++)Invoke(c,"Advance",.02f);File.WriteAllText(Output+"check.txt","PASS: original idle pose; lift height; unchanged scale; HDR emission; particles and light off at idle; activation/deactivation; interrupted transition; disable restoration. Preview-scene lifecycle tested, not scene gameplay.\n");Invoke(c,"OnDisable");}finally{PrefabUtility.UnloadPrefabContents(root);}}
 static void Render(GameObject root,string name){var scene=root.scene;var b=BoundsOf(root);var target=new Vector3(b.center.x,.7f,b.center.z);var go=new GameObject("Preview Camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.075f,.1f);camera.orthographic=true;camera.orthographicSize=1.15f;camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.transform.position=target+new Vector3(3,2,-4).normalized*10;camera.transform.LookAt(target);go.AddComponent<UniversalAdditionalCameraData>().SetRenderer(1);var lg=new GameObject("Preview Light");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lg,scene);var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;lg.transform.rotation=Quaternion.Euler(45,-35,0);var rt=new RenderTexture(768,768,24);rt.Create();var previous=RenderTexture.active;var tex=new Texture2D(768,768,TextureFormat.RGB24,false);try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();File.WriteAllBytes(Output+name+".png",tex.EncodeToPNG());}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(lg);}}
}

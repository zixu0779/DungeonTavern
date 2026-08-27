using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
internal static class B1WallSetup {
 const string Out="ArtSource/Previews/B1Walls/";
 static Scene Scene=>SceneManager.GetSceneByPath("Assets/Scenes/SealRoom/SealRoom_B1.unity");
 [MenuItem("Tools/Dungeon Tavern/Preview B1 Walls")]
 static void Preview(){var scene=Scene;var go=new GameObject("Temporary Wall Preview Camera");SceneManager.MoveGameObjectToScene(go,scene);var c=go.AddComponent<Camera>();c.enabled=false;c.scene=scene;c.orthographic=true;c.orthographicSize=13;c.nearClipPlane=.01f;c.farClipPlane=100;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.12f,.14f,.16f);var center=new Vector3(40.7f,1,18.8f);c.transform.position=center+new Vector3(0,24,-17);c.transform.LookAt(center);go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().SetRenderer(1);var rt=new RenderTexture(1200,1000,24);rt.Create();var old=RenderTexture.active;var tex=new Texture2D(1200,1000,TextureFormat.RGB24,false);try{UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(c,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,1000),0,0);tex.Apply();File.WriteAllBytes(Out+"Perimeter.png",tex.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}EditorSceneManager.SaveScene(scene);}
}

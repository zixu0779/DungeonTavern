using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
internal static class RubbleWallTrial {
 const string Root="Assets/DungeonTavern/Art/Environment/Architecture/Walls/StoneWall/Rubble/";
 const string Out="ArtSource/Previews/RubbleWallTrial/";
 static Scene Scene=>SceneManager.GetSceneByPath("Assets/Scenes/SealRoom/SealRoom_B1.unity");
 [MenuItem("Tools/Dungeon Tavern/Preview Rubble Arch Trial")]
 static void Preview(){var scene=Scene;var go=new GameObject("Temporary Sample Camera");SceneManager.MoveGameObjectToScene(go,scene);var cam=go.AddComponent<Camera>();cam.enabled=false;cam.scene=scene;cam.orthographic=true;cam.orthographicSize=6.6f;cam.nearClipPlane=.01f;cam.farClipPlane=100;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.14f,.16f);var center=new Vector3(34.8f,2.6f,22.6f);cam.transform.position=center+new Vector3(13,9,-11);cam.transform.LookAt(center);go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().SetRenderer(1);var lg=new GameObject("Temporary Review Light");SceneManager.MoveGameObjectToScene(lg,scene);var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;lg.transform.rotation=Quaternion.Euler(40,-60,0);var rt=new RenderTexture(1200,1000,24);rt.Create();var old=RenderTexture.active;var tex=new Texture2D(1200,1000,TextureFormat.RGB24,false);try{UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,1000),0,0);tex.Apply();File.WriteAllBytes(Out+"ArchTrial.png",tex.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(lg);}EditorSceneManager.SaveScene(scene);}

 [MenuItem("Tools/Dungeon Tavern/Check Rubble Arch Trial")]
 static void Check(){var group=Scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="StairArch");var collider=group.GetComponentInChildren<MeshCollider>();Physics.SyncTransforms();foreach(float y in new[]{2.08f,3.4f})if(collider.Raycast(new Ray(new Vector3(35.5f,y,24.1f),Vector3.left),out _,4))throw new Exception("Opening obstructed");if(!collider.Raycast(new Ray(new Vector3(35.5f,5.7f,24.1f),Vector3.left),out _,4))throw new Exception("Upper wall missing");Debug.Log("Rubble arch collision checks passed");}
}

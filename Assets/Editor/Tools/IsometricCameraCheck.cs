using System;
using System.IO;
using System.Linq;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
internal static class IsometricCameraCheck
{
    const string ScenePath="Assets/Scenes/Tavern/Tavern_Main.unity";
    const string Output="ArtSource/Previews/IsometricCamera/";
    [MenuItem("Tools/Dungeon Tavern/Apply And Check Isometric Camera")]
    static void Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first.");
        Directory.CreateDirectory(Output);
        var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;
        var active=SceneManager.GetActiveScene();
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            EditorSceneManager.SaveScene(scene);
            string backup="ArtSource/Backups/IsometricCamera_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(backup);File.Copy(ScenePath,backup+"/Tavern_Main.unity");
            var rig=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeCameraOrbit>(true)).Single();
            var camera=rig.GetComponentInChildren<Camera>(true);
            var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                .Where(t=>t!=camera.transform&&t!=rig.transform).ToDictionary(t=>t,t=>t.localToWorldMatrix);
            var oldPosition=camera.transform.localPosition;var oldRotation=camera.transform.localRotation;
            Undo.RecordObjects(new UnityEngine.Object[]{rig,rig.transform,camera,camera.transform},"Set isometric view");
            rig.ApplyIsometricProjection();
            var rotation=rig.transform.rotation;var report="Previous camera local position "+oldPosition+", rotation "+oldRotation.eulerAngles+"\n";
            foreach(float yaw in new[]{45f,135f,225f,315f})
            {
                rig.transform.rotation=Quaternion.Euler(0,yaw,0);
                var origin=camera.WorldToScreenPoint(Vector3.zero);
                var lengths=new[]{Vector3.right,Vector3.up,Vector3.forward}.Select(axis=>((Vector2)(camera.WorldToScreenPoint(axis)-origin)).magnitude).ToArray();
                if(lengths.Max()-lengths.Min()>.01f)throw new Exception("Axes are not equally foreshortened");
                report+=$"Yaw {yaw}: projected X/Y/Z unit lengths = {string.Join(", ",lengths)} PASS\n";
            }
            rig.transform.rotation=rotation;
            foreach(var pair in transforms)if(pair.Key.localToWorldMatrix!=pair.Value)throw new Exception("Unrelated transform moved: "+pair.Key.name);
            EditorUtility.SetDirty(rig);EditorUtility.SetDirty(camera);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            report+=$"New local position {camera.transform.localPosition}; pitch {camera.transform.localEulerAngles.x}; orthographic size {camera.orthographicSize}; backup {backup}\n";
            File.WriteAllText(Output+"check.txt",report);
            Render(camera,scene);
        }
        finally { if(opened)EditorSceneManager.CloseScene(scene,true);if(active.isLoaded)SceneManager.SetActiveScene(active); }
    }
    static void Render(Camera source,Scene scene)
    {
        var go=new GameObject("Isometric Preview");SceneManager.MoveGameObjectToScene(go,scene);
        var camera=go.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;camera.scene=scene;
        go.AddComponent<UniversalAdditionalCameraData>().SetRenderer(1);
        var center=new Vector3(24,0,16);go.transform.rotation=source.transform.rotation;go.transform.position=center-go.transform.forward*25;
        var rt=new RenderTexture(1280,720,24);rt.Create();var previous=RenderTexture.active;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try {RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Output+"Tavern.png",texture.EncodeToPNG());}
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}

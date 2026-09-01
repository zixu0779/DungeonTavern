using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonTavern.Tavern25D.Narrative;
using DungeonTavern.Prototypes.Rotation25D;
internal static class LightingParityReview
{
 const string Out="ArtSource/Previews/LightingParity/";
 [MenuItem("Tools/Dungeon Tavern/Inspect Lighting Parity")]
 static void Inspect(){Directory.CreateDirectory(Out);var sb=new StringBuilder();foreach(SceneView view in SceneView.sceneViews){sb.AppendLine("SceneView lighting="+view.sceneLighting+" draw="+view.cameraMode.drawMode+" effects="+view.sceneViewState.showImageEffects+" camera="+view.camera.transform.rotation);var f=typeof(SceneView).GetField("m_Light",BindingFlags.Instance|BindingFlags.NonPublic);foreach(var light in (Light[])f.GetValue(view))if(light)sb.AppendLine("Preview light "+light.color+" intensity="+light.intensity+" rotation="+light.transform.rotation+" mode="+light.renderMode);}
 sb.AppendLine("ColorSpace "+QualitySettings.activeColorSpace+" ambient="+RenderSettings.ambientMode+" color="+RenderSettings.ambientLight+" reflection="+RenderSettings.reflectionIntensity);
 foreach(var n in UnityEngine.Object.FindObjectsByType<Day1NarrativeController>(FindObjectsInactive.Include)){sb.AppendLine("Narrative enabled="+n.enabled+" active="+n.gameObject.activeInHierarchy+" state="+n.State);var so=new SerializedObject(n);foreach(var key in new[]{"chapterOne","player","storageArrival","businessDay","eve","eveOpeningGuidePoint"}){var v=so.FindProperty(key).objectReferenceValue;sb.AppendLine(key+"="+(v?v.name+" "+AssetDatabase.GetAssetPath(v):"NULL"));}}
 foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))sb.AppendLine("Light "+l.name+" active="+l.isActiveAndEnabled+" color="+l.color+" intensity="+l.intensity+" type="+l.type);
 foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>()){var rt=c.targetTexture;sb.AppendLine("Camera "+c.name+" ortho="+c.orthographic+" hdr="+c.allowHDR+" rt="+(rt?rt.graphicsFormat+" sRGB="+rt.sRGB:"none"));}
 File.WriteAllText(Out+(EditorApplication.isPlaying?"runtime-inspect.txt":"editor-inspect.txt"),sb.ToString());}
 [MenuItem("Tools/Dungeon Tavern/Capture Raw Lighting Output")]
 static void Raw(){var cam=UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>().GetComponentInChildren<Camera>();var rt=cam.targetTexture;if(!rt)throw new Exception("Play required");var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(Out+"raw-camera.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=previous;}
 [MenuItem("Tools/Dungeon Tavern/Apply Authored Color Materials")]
 static void ApplyColors()
 {
  if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
  const string b1="Assets/Scenes/SealRoom/SealRoom_B1.unity";
  var scene=SceneManager.GetSceneByPath(b1); bool opened=!scene.isLoaded;
  if(opened) scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(b1,UnityEditor.SceneManagement.OpenSceneMode.Additive);
  var materials=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include)
   .SelectMany(r=>r.sharedMaterials).Where(m=>m && m.shader.name=="Universal Render Pipeline/Lit")
   .Distinct().Where(m=>AssetDatabase.GetAssetPath(m).StartsWith("Assets/DungeonTavern/")).ToArray();
  var shader=Shader.Find("Universal Render Pipeline/Unlit");
  if(!shader) throw new InvalidOperationException("URP Unlit missing.");
  var backup="ArtSource/Backups/LightingParity/"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"/";
  var report=new StringBuilder("Backup: "+backup+"\n");
  try {
   foreach(var m in materials) {
    var path=AssetDatabase.GetAssetPath(m); Directory.CreateDirectory(Path.GetDirectoryName(backup+path));
    File.Copy(path,backup+path);File.Copy(path+".meta",backup+path+".meta");
    report.AppendLine(path+" baseColor="+m.GetColor("_BaseColor")+" texture="+AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")));
   }
   foreach(var m in materials) {
    Undo.RecordObject(m,"Match authored preview colors");
    int queue=m.renderQueue; bool alpha=m.IsKeywordEnabled("_ALPHATEST_ON"), transparent=m.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), premultiply=m.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
    m.shader=shader;m.shaderKeywords=Array.Empty<string>();
    if(alpha)m.EnableKeyword("_ALPHATEST_ON");if(transparent)m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");if(premultiply)m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
    m.renderQueue=queue;EditorUtility.SetDirty(m);
   }
   AssetDatabase.SaveAssets();
   Directory.CreateDirectory(Out);File.WriteAllText(Out+"changed-materials.txt",report.ToString());
   Debug.Log("Authored colors applied to "+materials.Length+" materials. Backup: "+backup);
  } finally {if(opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
 }
}

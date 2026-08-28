using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
internal static class B1Revision {
static Scene B=>SceneManager.GetSceneByPath("Assets/Scenes/SealRoom/SealRoom_B1.unity");
static Transform[] All(Scene s)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
[MenuItem("Tools/Dungeon Tavern/Inspect B1 Revision")]
static void Inspect(){var b=new StringBuilder(); b.AppendLine("Playing="+EditorApplication.isPlaying); foreach(var t in All(B).Where(t=>t.name.Contains("Side")||t.name=="RearEnd"||t.name.Contains("Ground_Cracked")||t.name.Contains("StairRear")||t.name=="RockBackdrop"||t.name=="Walls_Stone")){b.AppendLine(t.name+" pos="+t.position+" scale="+t.lossyScale+" rot="+t.eulerAngles);foreach(var r in t.GetComponentsInChildren<Renderer>())b.AppendLine("  "+r.name+" "+r.bounds);}foreach(var t in All(B).Where(t=>t.name=="Ground_Cracked").SelectMany(t=>t.GetComponentsInChildren<SpriteRenderer>()).Take(2)){b.AppendLine("TILE rot="+t.transform.eulerAngles+" scale="+t.transform.lossyScale+" sprite="+AssetDatabase.GetAssetPath(t.sprite)+" rect="+t.sprite.rect);b.AppendLine(string.Join(";",t.sprite.vertices.Select(v=>t.transform.TransformPoint(v).ToString("F4"))));}var main=SceneManager.GetSceneByPath("Assets/Scenes/Tavern/Tavern_Main.unity");if(main.isLoaded)foreach(var t in All(main).Where(t=>t.name.StartsWith("BlackFog")))b.AppendLine("FOG "+t.name+" pos="+t.position+" scale="+t.lossyScale+" prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t));File.WriteAllText("ArtSource/Previews/B1Backdrop/inspect.txt",b.ToString());}

}

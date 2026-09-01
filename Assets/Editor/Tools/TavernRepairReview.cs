using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonTavern.Tavern25D;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Gameplay.Interaction;
internal static class TavernRepairReview
{
 const string Out="ArtSource/Previews/TavernRepairReview/";
 static Scene Main=>SceneManager.GetSceneByName("Tavern_Main");
 static Transform[] All(Scene s)=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 [MenuItem("Tools/Dungeon Tavern/Inspect Tavern Repairs")]
 static void Inspect(){Directory.CreateDirectory(Out);Physics.SyncTransforms();var sb=new StringBuilder();foreach(var t in All(Main)){if(t.GetComponent<Light>()||t.GetComponent<Camera>()||t.GetComponent<AutomaticDoorTrigger>()||t.name=="Door_Small_Stone_3"||t.name=="TavernWalkableFloorCollider"||PathOf(t).Contains("Stair_Stone_Storage")||t.name.StartsWith("Chest_")||t.GetComponent<CupDispenserPoint>()||t.name=="FloatingCup") {sb.AppendLine(PathOf(t)+" pos="+t.position+" rot="+t.eulerAngles+" scale="+t.lossyScale);foreach(var c in t.GetComponents<Component>()){if(c is Collider col)sb.AppendLine("COL "+c.GetType().Name+" "+col.bounds+" "+EditorJsonUtility.ToJson(c));if(c is MonoBehaviour||c is Light||c is Camera)sb.AppendLine(EditorJsonUtility.ToJson(c));}foreach(var r in t.GetComponents<Renderer>())sb.AppendLine("RENDER "+r.bounds);}}
 foreach(float z in new[]{22.9f,23.3f,23.7f,24.1f,24.5f,24.9f,25.3f,25.7f,26.1f}){sb.AppendLine("RAY z="+z);foreach(var h in Physics.RaycastAll(new Vector3(47.48f,3,z),Vector3.down,8,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))sb.AppendLine(PathOf(h.transform)+" y="+h.point.y);}
 foreach(var p in UnityEngine.Object.FindObjectsByType<PrototypePlayerMover>(FindObjectsInactive.Include)){sb.AppendLine("PLAYER "+PathOf(p.transform));foreach(var r in p.GetComponentsInChildren<Renderer>())sb.AppendLine("VISUAL "+PathOf(r.transform)+" local="+r.transform.localPosition+" scale="+r.transform.localScale+" bounds="+r.bounds);}
 sb.AppendLine("Ambient "+RenderSettings.ambientMode+" sky="+RenderSettings.ambientSkyColor+" ground="+RenderSettings.ambientGroundColor);
 File.WriteAllText(Out+"inspect.txt",sb.ToString());}

 [MenuItem("Tools/Dungeon Tavern/Adjust Cup Grip")]
 static void Grip(){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var scene=SceneManager.GetSceneByPath("Assets/Scenes/SealRoom/SealRoom_B1.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/SealRoom/SealRoom_B1.unity",OpenSceneMode.Additive);var anchor=All(scene).Single(t=>t.name=="CupGripAnchor");Undo.RecordObject(anchor,"Fit cup to sprite hand");anchor.localPosition=new Vector3(.52f,.9f,.18f);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);}
}

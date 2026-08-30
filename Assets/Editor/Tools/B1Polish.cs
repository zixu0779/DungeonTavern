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
internal static class B1Polish {
const string Out="ArtSource/Previews/B1Polish/";
static Scene B=>SceneManager.GetSceneByPath("Assets/Scenes/SealRoom/SealRoom_B1.unity");
static Transform[] All(Scene s)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
[MenuItem("Tools/Dungeon Tavern/Inspect B1 Polish")]
static void Inspect(){Directory.CreateDirectory(Out);var sb=new StringBuilder();sb.AppendLine("Play="+EditorApplication.isPlaying);foreach(var t in All(B).Where(t=>t.name=="NorthSide"||t.name.StartsWith("RubbleWall_Arch")||t.name.StartsWith("RubbleWall_Return")||t.name.Contains("Teleport")||t.name=="Player")){sb.AppendLine(t.name+" pos="+t.position+" scale="+t.lossyScale);foreach(var c in t.GetComponents<Component>())if(c is MonoBehaviour)sb.AppendLine(EditorJsonUtility.ToJson(c));foreach(var r in t.GetComponentsInChildren<Renderer>())sb.AppendLine("RENDER "+r.name+" "+r.bounds+" scale="+r.transform.localScale);foreach(var c in t.GetComponents<Collider>())sb.AppendLine("COLLIDER "+c.bounds+" enabled="+c.enabled+" trigger="+c.isTrigger);}var main=SceneManager.GetSceneByPath("Assets/Scenes/Tavern/Tavern_Main.unity");if(main.isLoaded)foreach(var t in All(main).Where(t=>t.name.Contains("B1Return")||t.name=="Player"))sb.AppendLine("MAIN "+t.name+" parent="+t.parent?.name+" pos="+t.position);File.WriteAllText(Out+"inspect.txt",sb.ToString());}

[MenuItem("Tools/Dungeon Tavern/Test B1 Return Trigger")]
static void Return(){if(!EditorApplication.isPlaying)throw new Exception("Play required");var t=All(B).Single(t=>t.name=="TeleportTriggerWall_ToMain");var box=t.GetComponent<Collider>();if(!box.enabled||!box.isTrigger)throw new Exception("Inactive trigger");var player=UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=box.bounds.center;c.enabled=true;Physics.SyncTransforms();c.Move(Vector3.down*.01f);}
[MenuItem("Tools/Dungeon Tavern/Verify B1 Return")]
static void Verify(){var main=SceneManager.GetSceneByName("Tavern_Main");var ps=UnityEngine.Object.FindObjectsByType<PrototypePlayerMover>();if(!main.isLoaded||B.isLoaded||ps.Length!=1)throw new Exception("Return scene state incorrect");var arrival=All(main).Single(t=>t.name=="B1Return_StoneStairArrival");if(Vector3.Distance(ps[0].transform.position,arrival.position)>1)throw new Exception("Wrong arrival");File.WriteAllText(Out+"return.txt","Actual trigger entry returned to Tavern_Main, unloaded B1, retained exactly one player at "+ps[0].transform.position);}

}

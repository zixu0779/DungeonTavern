using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Tavern25D;
internal static class TavernInteractionRevision
{
 const string Output="ArtSource/Previews/TavernInteractionRevision/";
 static Transform[] All()=>SceneManager.GetSceneByName("Tavern_Main").GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 [MenuItem("Tools/Dungeon Tavern/Inspect Interaction Revision")]
 static void Inspect(){Directory.CreateDirectory(Output);var sb=new StringBuilder();foreach(var t in All()) {var path=PathOf(t);if(t.GetComponent<InteractionPoint>()||path.Contains("Door")||path.Contains("Gate")||path.Contains("Lever")||path.Contains("Switch")||path.Contains("Rope")||path.Contains("CupDispenser")||path.Contains("Chest")||t.name=="Gameplay") {sb.AppendLine(path+" pos="+t.position+" local="+t.localPosition+" rot="+t.eulerAngles+" scale="+t.lossyScale+" prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject));foreach(var c in t.GetComponents<Component>()){if(c is Transform)continue;sb.AppendLine("  "+c.GetType().Name+" "+(c is Collider col?"bounds="+col.bounds+" ":"")+EditorJsonUtility.ToJson(c));}}}File.WriteAllText(Output+"inspection.txt",sb.ToString());}
}

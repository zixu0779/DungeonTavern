using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DungeonTavern.Prototypes.Rotation25D;
internal static class B1GateCutaway {
static Scene B=>SceneManager.GetSceneByPath("Assets/Scenes/SealRoom/SealRoom_B1.unity");
static Transform[] All(Scene s)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
[MenuItem("Tools/Dungeon Tavern/Inspect Gate Cutaway")]
static void Inspect(){var sb=new StringBuilder();sb.AppendLine("Play="+EditorApplication.isPlaying);var main=SceneManager.GetSceneByPath("Assets/Scenes/Tavern/Tavern_Main.unity");foreach(var s in new[]{B,main})if(s.isLoaded)foreach(var t in All(s).Where(t=>t.name.Contains("Teleport")||t.name.Contains("Arrival")||t.name.Contains("Return")||t.name.Contains("Stair")||t.name.Contains("StoneGate")||t.name=="RockBackdrop")){sb.AppendLine(s.name+" "+t.name+" "+t.position+" forward="+t.forward);if(t.name.Contains("Stair"))foreach(var r in t.GetComponentsInChildren<Renderer>())sb.AppendLine("Bounds "+r.bounds);foreach(var c in t.GetComponents<Component>())if(c is MonoBehaviour)sb.AppendLine(EditorJsonUtility.ToJson(c));}foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>()){sb.AppendLine(c.name+" ortho="+c.orthographic+" rotation="+c.transform.eulerAngles+" matrix="+c.projectionMatrix);float[] spans=new[]{12f,18f,24f}.Select(z=>Vector3.Distance(c.WorldToScreenPoint(new Vector3(42,0,z)),c.WorldToScreenPoint(new Vector3(43,0,z)))).ToArray();sb.AppendLine("1m X projected pixels at Z12/18/24: "+string.Join(",",spans));}sb.AppendLine("SceneView ortho="+SceneView.lastActiveSceneView?.orthographic);File.WriteAllText("ArtSource/Previews/B1Backdrop/gate-inspect.txt",sb.ToString());}

[MenuItem("Tools/Dungeon Tavern/Check Gate Cutaway")]
static void Check(){var all=All(B);var gates=all.Where(t=>t.name=="StoneGate_01"||t.name=="StoneGate_02").ToArray();int count=0;foreach(var gate in gates){var panel=gate.Find("DoorPanel");var initial=panel.localPosition;try{foreach(float y in new[]{0f,1.475f,2.95f}){panel.localPosition=new Vector3(initial.x,y,initial.z);foreach(var c in gate.GetComponentsInChildren<WallTopMeshCutaway>()){c.Rebuild();foreach(var v in c.GetComponent<MeshFilter>().sharedMesh.vertices)if(c.transform.TransformPoint(v).y>2.301f)throw new Exception("Visible geometry above wall top");count++;}}}finally{panel.localPosition=initial;foreach(var c in gate.GetComponentsInChildren<WallTopMeshCutaway>())c.Rebuild();}}File.WriteAllText("ArtSource/Previews/B1Backdrop/gate-check.txt","Passed "+count+" renderer checks at closed, midpoint and open positions; original collider and source mesh retained.");}

[MenuItem("Tools/Dungeon Tavern/Restore Scene View Perspective")]
static void RestoreSceneView(){var view=SceneView.lastActiveSceneView;if(!view)throw new Exception("No Scene View");view.orthographic=false;view.Repaint();Debug.Log("Scene View perspective restored; scene objects and Game Camera unchanged.");}
}

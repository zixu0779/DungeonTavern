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
internal static class SceneCollisionPass {
const string Out="ArtSource/Previews/SceneCollisionPass/";
static string[] Paths={"Assets/Scenes/Tavern/Tavern_Main.unity","Assets/Scenes/SealRoom/SealRoom_B1.unity","Assets/Scenes/SampleScene.unity"};
static Transform[] All(Scene s)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
static bool Visual(Transform t){var p=PathOf(t).ToLowerInvariant();return p.Contains("fog")||p.Contains("rockbackdrop")||p.Contains("vfx")||p.Contains("particle")||p.Contains("glow")||p.Contains("halo")||p.Contains("shadow")||p.Contains("/material/")||p.Contains("character")||p.Contains("ui/");}
static bool Covered(Renderer r){var door=r.GetComponentInParent<DoorStateController>();if(door&&door.BlockingCollider)return true;for(var t=r.transform;t;t=t.parent)if(t.GetComponents<Collider>().Any(c=>c.enabled&&!c.isTrigger))return true;return false;}
[MenuItem("Tools/Dungeon Tavern/Audit Scene Collisions")]
static void Audit(){if(EditorApplication.isPlaying)throw new Exception("Edit required");Directory.CreateDirectory(Out);var sb=new StringBuilder();foreach(var path in Paths){var scene=SceneManager.GetSceneByPath(path);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);sb.AppendLine("SCENE "+scene.name);foreach(var t in All(scene)){var r=t.GetComponent<Renderer>();if(r&&r.enabled&&!Visual(t)&&!Covered(r)){var f=t.GetComponent<MeshFilter>();sb.AppendLine("MISSING "+PathOf(t)+" type="+r.GetType().Name+" mesh="+(f&&f.sharedMesh?f.sharedMesh.vertexCount:0)+" bounds="+r.bounds);}var portal=t.GetComponent<AdditiveScenePortal>();if(portal)sb.AppendLine("PORTAL "+PathOf(t)+" "+EditorJsonUtility.ToJson(portal)+" collider="+t.GetComponent<Collider>().enabled+" pos="+t.position);if(t.name=="SealRoomStairArrival")sb.AppendLine("ARRIVAL "+t.position);}}File.WriteAllText(Out+"audit.txt",sb.ToString());}

static bool FloorCovered(Renderer r,Collider[] colliders){var path=PathOf(r.transform);if(!path.Contains("/Floor/")&&!path.Contains("/Ground_Cracked/"))return false;var b=r.bounds;foreach(var p in new[]{b.center,new Vector3(b.min.x+.02f,b.center.y,b.min.z+.02f),new Vector3(b.max.x-.02f,b.center.y,b.max.z-.02f)}){var ray=new Ray(new Vector3(p.x,.2f,p.z),Vector3.down);if(!colliders.Any(c=>c.enabled&&!c.isTrigger&&c.Raycast(ray,out var hit,.4f)))return false;}return true;}
[MenuItem("Tools/Dungeon Tavern/Trigger F1 To B1")]
static void Enter(){if(!EditorApplication.isPlaying)throw new Exception("Play required");var scene=SceneManager.GetSceneByName("Tavern_Main");var p=All(scene).Select(t=>t.GetComponent<AdditiveScenePortal>()).Single(p=>p&&PathOf(p.transform).Contains("TransitionParts_B1"));var player=UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=p.GetComponent<Collider>().bounds.center;cc.enabled=true;Physics.SyncTransforms();cc.Move(Vector3.down*.01f);}
[MenuItem("Tools/Dungeon Tavern/Verify F1 To B1")]
static void Verify(){var b1=SceneManager.GetSceneByName("SealRoom_B1");var players=UnityEngine.Object.FindObjectsByType<PrototypePlayerMover>();if(!b1.isLoaded||players.Length!=1)throw new Exception("Invalid loaded scene or player count");var arrival=All(b1).Single(t=>t.name=="SealRoomStairArrival");if(Vector3.Distance(players[0].transform.position,arrival.position)>1)throw new Exception("Wrong B1 arrival");var main=SceneManager.GetSceneByName("Tavern_Main");var p=All(main).Select(t=>t.GetComponent<AdditiveScenePortal>()).Single(p=>p&&PathOf(p.transform).Contains("TransitionParts_L2"));if(p.enabled||!p.GetComponent<Collider>().enabled||p.GetComponent<Collider>().isTrigger)throw new Exception("F2 not blocked");File.WriteAllText(Out+"roundtrip.txt","Actual B1->F1->B1 trigger round trip passed. Exactly one player at "+players[0].transform.position+"; F2 portal disabled with solid collider.");}


[MenuItem("Tools/Dungeon Tavern/Check Scene Collision Coverage")]
static void Coverage(){if(EditorApplication.isPlaying)throw new Exception("Edit required");var missing=new StringBuilder();int count=0;foreach(var path in Paths){var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{Physics.SyncTransforms();var all=All(scene);var cs=all.SelectMany(t=>t.GetComponents<Collider>()).ToArray();foreach(var t in all){var r=t.GetComponent<Renderer>();if(!r||!r.enabled||r is ParticleSystemRenderer||r is LineRenderer||Visual(t)||Covered(r)||FloorCovered(r,cs))continue;if(t.GetComponent<MeshFilter>()||r is SpriteRenderer){count++;missing.AppendLine(PathOf(t));}}if(scene.name=="Tavern_Main"){var p=all.Select(t=>t.GetComponent<AdditiveScenePortal>()).Single(p=>p&&PathOf(p.transform).Contains("TransitionParts_L2"));var c=p.GetComponent<Collider>();if(!c.Raycast(new Ray(c.bounds.center-p.transform.forward*5,p.transform.forward),out _,10))throw new Exception("F2 barrier ray missed");}}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}}File.WriteAllText(Out+"coverage.txt","Missing eligible physical renderers="+count+"\n"+missing+"F2 solid barrier ray hit passed.");if(count>0)throw new Exception("Uncovered objects remain");}

}

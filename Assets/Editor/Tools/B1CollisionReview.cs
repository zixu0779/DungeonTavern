using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
internal static class B1CollisionReview {
static Scene B=>SceneManager.GetSceneByPath("Assets/Scenes/SealRoom/SealRoom_B1.unity");
static Transform[] All()=>B.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
const string Out="ArtSource/Previews/B1Collision/";
[MenuItem("Tools/Dungeon Tavern/Inspect B1 Collision")]
static void Inspect(){Directory.CreateDirectory(Out);var sb=new StringBuilder();sb.AppendLine("Play="+EditorApplication.isPlaying);foreach(var t in All().Where(t=>t.name.Contains("Stair")||t.name=="GatePlatform")){sb.AppendLine("OBJECT "+t.name+" "+t.position);foreach(var f in t.GetComponentsInChildren<MeshFilter>())sb.AppendLine("MESH "+f.name+" verts="+f.sharedMesh.vertexCount+" colliders="+f.GetComponents<Collider>().Length+" "+f.GetComponent<Renderer>().bounds);}foreach(var c in All().SelectMany(t=>t.GetComponents<Collider>()).Where(c=>c.enabled&&c.gameObject.activeInHierarchy)){if(c.bounds.SqrDistance(new Vector3(44.39f,1,18.54f))>49)continue;sb.AppendLine("COLLIDER "+c.name+" type="+c.GetType().Name+" trigger="+c.isTrigger+" bounds="+c.bounds+" parent="+c.transform.parent?.name+" renderer="+c.GetComponent<Renderer>()); }File.WriteAllText(Out+"inspect.txt",sb.ToString());}

[MenuItem("Tools/Dungeon Tavern/Check B1 Stair Support")]
static void Check(){var stair=All().Single(t=>t.name=="Stair_Stone_B1_Ascending");var c=stair.GetComponentInChildren<MeshCollider>();Physics.SyncTransforms();int hits=0;var sb=new StringBuilder();for(float z=19.4f;z<=24.8f;z+=.25f){var ray=new Ray(new Vector3(34.4f,4,z),Vector3.down);if(c.Raycast(ray,out var hit,5)){hits++;sb.AppendLine(z+" -> "+hit.point.y);}}if(hits<8)throw new Exception("Stair support coverage insufficient");File.WriteAllText(Out+"stairs-check.txt",hits+" stair surface samples hit\n"+sb);}
[MenuItem("Tools/Dungeon Tavern/Walk B1 Stair Test")]
static void Walk(){if(!EditorApplication.isPlaying)throw new Exception("Play required");var mover=UnityEngine.Object.FindAnyObjectByType<DungeonTavern.Prototypes.Rotation25D.PrototypePlayerMover>();var cc=mover.GetComponent<CharacterController>();var pos=mover.transform.position;var rot=mover.transform.rotation;try{cc.enabled=false;mover.transform.position=new Vector3(34.4f,.05f,18.85f);cc.enabled=true;Physics.SyncTransforms();for(int i=0;i<175;i++)cc.Move(new Vector3(0,-.05f,.03f));if(mover.transform.position.z<23.2f||mover.transform.position.y<1.9f)throw new Exception("Stair walk blocked at "+mover.transform.position);File.WriteAllText(Out+"walk-check.txt","CharacterController climbed actual stair from (34.4,.05,18.85) to "+mover.transform.position+" with 175 movement steps.");}finally{cc.enabled=false;mover.transform.SetPositionAndRotation(pos,rot);cc.enabled=true;}}
}

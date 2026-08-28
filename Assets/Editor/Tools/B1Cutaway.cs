using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DungeonTavern.Prototypes.Rotation25D;
internal static class B1Cutaway {
 const string Root="Assets/DungeonTavern/Art/Environment/Architecture/Cutaway/";
 const string Out="ArtSource/Previews/B1Cutaway/";
 static Scene Scene=>SceneManager.GetSceneByPath("Assets/Scenes/SealRoom/SealRoom_B1.unity");
 [MenuItem("Tools/Dungeon Tavern/Check Free Camera Rotation")]
 static void CheckCamera(){var g=new GameObject("Temporary Camera Check");try{g.SetActive(false);var c=g.AddComponent<PrototypeCameraOrbit>();c.RotateBy(12.5f);if(Mathf.Abs(Mathf.DeltaAngle(g.transform.eulerAngles.y,12.5f))>.001f)throw new Exception("Yaw snapped");c.RotateBy(-25);if(Mathf.Abs(Mathf.DeltaAngle(g.transform.eulerAngles.y,347.5f))>.001f)throw new Exception("Negative rotation failed");c.RotateBy(720);if(Mathf.Abs(Mathf.DeltaAngle(g.transform.eulerAngles.y,347.5f))>.001f)throw new Exception("Wrapping failed");File.WriteAllText(Out+"camera-check.txt","Arbitrary yaw, negative yaw, multi-turn wrapping passed; keyboard/mouse input not yet tested in Play Mode.");}finally{UnityEngine.Object.DestroyImmediate(g);}}

 [MenuItem("Tools/Dungeon Tavern/Check B1 Cutaway")]
 static void CheckScene(){var env=Scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Environment");var walls=env.Find("Walls_Stone");int count=0;foreach(Transform t in walls){var mf=t.GetComponent<MeshFilter>();if(!mf)continue;if(mf.sharedMesh.bounds.max.y>3.601f)throw new Exception("Ordinary wall still tall");count++;}if(count!=23)throw new Exception("Unexpected wall count");Physics.SyncTransforms();foreach(float z in new[]{15.65f,21.65f}){if(Physics.Raycast(new Vector3(50.85f,1,z),Vector3.right,3.2f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Rock blocks reserved gate passage");}var rock=env.Find("DungeonRockMass");if(rock.GetComponentsInChildren<Transform>().Count(t=>t.name=="UnfinishedPassageBoundary")!=3)throw new Exception("Missing passage boundaries");File.WriteAllText(Out+"scene-check.txt","23 ordinary walls lowered; both gate passages clear before fog; three temporary end boundaries present.\n");}
}

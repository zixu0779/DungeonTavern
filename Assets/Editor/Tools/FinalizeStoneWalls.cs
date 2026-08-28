using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
internal static class FinalizeStoneWalls {
 [MenuItem("Tools/Dungeon Tavern/Check Final Stone Walls")]
 static void Check(){var scene=SceneManager.GetSceneByPath("Assets/Scenes/SealRoom/SealRoom_B1.unity");var walls=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Walls_Stone");if(AssetDatabase.GetDependencies(scene.path,true).Any(p=>p.Contains("/StoneWall/Legacy/")))throw new Exception("Legacy wall dependency remains");if(walls.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterials.Any(m=>!m)))throw new Exception("Missing wall material");if(walls.GetComponentsInChildren<Collider>().Length!=25)throw new Exception("Unexpected wall collider count");Debug.Log("Final stone walls: 25 wall colliders, materials present, no legacy references");}
}

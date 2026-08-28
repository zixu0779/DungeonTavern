using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
internal static class B1GateLanding {
 [MenuItem("Tools/Dungeon Tavern/Check Gate Landing")]
 static void Check(){var scene=SceneManager.GetSceneByPath("Assets/Scenes/SealRoom/SealRoom_B1.unity");var ts=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var platform=ts.Single(t=>t.name=="GatePlatform");var core=ts.Single(t=>t.name=="SealControlCore").GetComponentInChildren<Renderer>().bounds;Physics.SyncTransforms();foreach(var c in platform.GetComponentsInChildren<Collider>()){if(c.bounds.Intersects(core))throw new Exception("Platform overlaps core");if(!c.Raycast(new Ray(c.bounds.center+Vector3.up*3,Vector3.down),out _,5))throw new Exception("Platform collision missing");}var steps=platform.Cast<Transform>().Where(t=>t.name.StartsWith("NorthStep")).OrderBy(t=>t.name).ToArray();if(steps.Length!=5)throw new Exception("Expected 5 steps");for(int i=0;i<steps.Length;i++)if(Mathf.Abs(steps[i].GetComponent<Collider>().bounds.max.y-(1-i*.2f))>.001f)throw new Exception("Step height incorrect");if(ts.Count(t=>t.name.StartsWith("EastExtension_"))!=14)throw new Exception("Expected 14 floor tiles");File.WriteAllText("ArtSource/Previews/B1GateLanding/check.txt","14 extension tiles; 5 steps at 0.2 rise; platform collision present; no core bounding-box overlap.\n");}
}

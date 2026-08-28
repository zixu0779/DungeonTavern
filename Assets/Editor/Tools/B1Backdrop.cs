using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonTavern.Prototypes.Rotation25D;
internal static class B1Backdrop {
 const string Out="ArtSource/Previews/B1Backdrop/";
 static Scene Scene=>SceneManager.GetSceneByPath("Assets/Scenes/SealRoom/SealRoom_B1.unity");
 [MenuItem("Tools/Dungeon Tavern/Runtime B1 View 0")]
 static void View0()=>View(0);
 [MenuItem("Tools/Dungeon Tavern/Runtime B1 View 45")]
 static void View45()=>View(45);
 [MenuItem("Tools/Dungeon Tavern/Runtime B1 View 135")]
 static void View135()=>View(135);
 [MenuItem("Tools/Dungeon Tavern/Runtime B1 View 225")]
 static void View225()=>View(225);
 static void View(float yaw){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");var rig=UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>();rig.RotateBy(Mathf.DeltaAngle(rig.CurrentCardinalYaw,yaw));}
}

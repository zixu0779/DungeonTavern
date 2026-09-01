using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DungeonTavern.Gameplay.Interaction;
internal static class DrinkBarrelCheck
{
 [MenuItem("Tools/Dungeon Tavern/Check Drink Barrel")]
 static void Run(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Tavern/Tavern_Main.unity");
 var model=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Barrel_Wood_Horizontal_Tap");
 var point=model.GetComponentInChildren<DrinkBarrelPoint>(true);
 if(point==null){var go=new GameObject("BarrelDrinkPickup");Undo.RegisterCreatedObjectUndo(go,"Restore drink pickup");go.transform.SetParent(model,false);var b=model.GetComponent<Renderer>().bounds;go.transform.position=new Vector3(b.center.x,b.min.y,b.center.z);point=go.AddComponent<DrinkBarrelPoint>();}
 var probe=new GameObject("DrinkPickupValidation");
 try{var hands=probe.AddComponent<PlayerHands>();if(point.Interact(hands)||!hands.TryHold(HeldItem.EmptyCup)||!point.Interact(hands)||hands.CurrentItem!=HeldItem.TestDrink||point.Interact(hands))throw new Exception("Drink pickup regression");}
 finally{UnityEngine.Object.DestroyImmediate(probe);}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText("/tmp/dt-barrel-replacement/result.txt","PASS: saved new barrel with one pickup; only an empty cup receives drink; full hands reject another. Bounds="+model.GetComponent<Renderer>().bounds);
 }
}

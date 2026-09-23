using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DungeonTavern.Tavern25D;
public static class ProtagonistRebindSetup {
 [MenuItem("Tools/Characters/Apply Rebound Protagonist")]
 public static void Run(){
 const string folder="Assets/DungeonTavern/Art/Characters/Protagonist/";
 var importer=(ModelImporter)AssetImporter.GetAtPath(folder+"Protagonist.fbx");
 var model=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"Protagonist.fbx");
 var hd=importer.humanDescription;
 var instance=UnityEngine.Object.Instantiate(model);
 try {
 var api=typeof(Editor).Assembly.GetType("UnityEditor.AvatarSetupTool");
 const BindingFlags flags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 var bones=instance.GetComponentsInChildren<Transform>().ToDictionary(t=>t,t=>true);
 var mapping=hd.human.ToDictionary(h=>h.humanName,h=>h.boneName);
 var getBones=api.GetMethods(flags).Single(m=>m.Name=="GetHumanBones"&&m.GetParameters()[0].ParameterType==typeof(Dictionary<string,string>));
 var wrappers=getBones.Invoke(null,new object[]{mapping,bones});
 api.GetMethod("MakePoseValid",flags).Invoke(null,new[]{wrappers});
 hd.skeleton=(SkeletonBone[])api.GetMethod("GetSkeletonBones",flags).Invoke(null,new object[]{instance.transform});
 Debug.Log("Humanoid reference pose error: "+api.GetMethod("GetPoseError",flags).Invoke(null,new[]{wrappers}));
 } finally { UnityEngine.Object.DestroyImmediate(instance); }
 importer.humanDescription=hd;importer.SaveAndReimport();
 var prefab=PrefabUtility.LoadPrefabContents(folder+"Protagonist.prefab");
 try { var cape=prefab.GetComponent<ProtagonistCapeMotion>();if(cape)cape.enabled=false;
 var anim=prefab.GetComponent<Animator>();if(!anim.avatar||!anim.avatar.isValid||!anim.isHuman)throw new Exception("Invalid avatar");
 PrefabUtility.SaveAsPrefabAsset(prefab,folder+"Protagonist.prefab");Debug.Log("Rebound protagonist avatar valid; cloth uses ordinary skin weights.");
 }finally{PrefabUtility.UnloadPrefabContents(prefab);}
 }
}

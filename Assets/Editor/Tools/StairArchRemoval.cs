using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class StairArchRemoval
{
 const string Folder="Assets/DungeonTavern/Art/Models/Stairs/Stair_Stone_B1_Ascending/";
 [MenuItem("Tools/Environment/Audit Stair Meshes")]
 static void Audit(){
  string report="";
  foreach(var name in new[]{"Stair_Stone_B1_Ascending","Stair_Stone_B1_NoArch"}){
   var go=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+name+".fbx");
   foreach(var f in go.GetComponentsInChildren<MeshFilter>(true))report+=$"{name} {f.name} mesh={f.sharedMesh.bounds} transform={f.transform.localToWorldMatrix}\n";
  }
  File.WriteAllText("/tmp/stair-mesh-audit.txt",report);
 }
 [MenuItem("Tools/Environment/Apply Stair Without Arch")]
 static void Apply(){
  string path=Folder+"Stair_Stone_B1_Ascending.prefab";
  File.Copy(path,"/tmp/Stair_Stone_B1_Ascending-before-arch-removal.prefab",true);
  var original=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Stair_Stone_B1_Ascending.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
  var replacement=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Stair_Stone_B1_NoArch.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
  if(Mathf.Abs(original.bounds.size.x-replacement.bounds.size.x)>.0003f || Mathf.Abs(original.bounds.size.y-replacement.bounds.size.y)>.0003f || replacement.bounds.size.z>=original.bounds.size.z)throw new System.Exception("Stair footprint or vertical axis mismatch");
  var root=PrefabUtility.LoadPrefabContents(path);
  try{
   foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh==original)f.sharedMesh=replacement;
   foreach(var c in root.GetComponentsInChildren<MeshCollider>(true))if(c.sharedMesh==original)c.sharedMesh=replacement;
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  File.WriteAllText("/tmp/stair-removal-result.txt","Applied replacement mesh; original FBX retained; footprint verified.\n"+replacement.bounds);
 }
}

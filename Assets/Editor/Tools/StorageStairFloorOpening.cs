using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
internal static class StorageStairFloorOpening
{
 struct V {public Vector3 p;public Vector2 uv;public float z;}
 [MenuItem("Tools/Dungeon Tavern/Trim Storage Stair Floor Lip")]
 static void Apply()
 {
  const string folder="Assets/DungeonTavern/Art/Environment/Architecture/FloorClipping/StorageStairOpening";
  if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder),"StorageStairOpening");
  var scene=SceneManager.GetSceneByName("Tavern_Main");var tiles=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>new[]{"Floor_46_23","Floor_47_23","Floor_48_23"}.Contains(t.name));
  foreach(var tile in tiles){var sr=tile.GetComponentInChildren<SpriteRenderer>(true);if(!sr.enabled)continue;var sprite=sr.sprite;var vertices=sprite.vertices;var uv=sprite.uv;var triangles=sprite.triangles;var result=new List<V>();var indices=new List<int>();
   for(int i=0;i<triangles.Length;i+=3){var poly=new List<V>();for(int j=0;j<3;j++){int k=triangles[i+j];Vector3 p=vertices[k];poly.Add(new V{p=p,uv=uv[k],z=sr.transform.TransformPoint(p).z});}var clipped=new List<V>();for(int j=0;j<3;j++){var a=poly[j];var b=poly[(j+1)%3];bool ai=a.z<=23.65f,bi=b.z<=23.65f;if(ai)clipped.Add(a);if(ai!=bi){float t=(23.65f-a.z)/(b.z-a.z);clipped.Add(new V{p=Vector3.Lerp(a.p,b.p,t),uv=Vector2.Lerp(a.uv,b.uv,t),z=23.65f});}}int start=result.Count;result.AddRange(clipped);for(int j=1;j<clipped.Count-1;j++)indices.AddRange(new[]{start,start+j,start+j+1});}
   var mesh=new Mesh{name=tile.name+"_StairLip"};mesh.vertices=result.Select(v=>v.p).ToArray();mesh.uv=result.Select(v=>v.uv).ToArray();mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"/"+mesh.name+".asset");
   string matPath=folder+"/MAT_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite.texture))+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetTexture("_BaseMap",sprite.texture);mat.SetColor("_BaseColor",sr.color);mat.SetFloat("_Cull",0);AssetDatabase.CreateAsset(mat,matPath);}
   var g=new GameObject("StairOpeningFloorLip");Undo.RegisterCreatedObjectUndo(g,"Trim stair floor lip");g.transform.SetParent(sr.transform,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;Undo.RecordObject(sr,"Preserve original floor sprite");sr.enabled=false;
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
}

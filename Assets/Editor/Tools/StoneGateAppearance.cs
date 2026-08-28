using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
internal static class StoneGateAppearance {
 const string Root="Assets/DungeonTavern/Art/Environment/Architecture/Doors/StoneGate/";
 [MenuItem("Tools/Dungeon Tavern/Update Stone Gate Appearance")]
 static void Apply(){var backup="ArtSource/Backups/StoneGateAppearance_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);foreach(var p in new[]{"StoneGate.prefab","MAT_StoneGate_Door.mat"})File.Copy(Root+p,backup+"/"+p);var wall=AssetDatabase.LoadAssetAtPath<Material>("Assets/DungeonTavern/Art/Environment/Architecture/Walls/RubbleWall/MAT_RubbleStone.mat");if(!wall)throw new Exception("Missing current wall material");var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"StoneSlab_Albedo.png");ti.npotScale=TextureImporterNPOTScale.None;ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.filterMode=FilterMode.Point;ti.wrapMode=TextureWrapMode.Repeat;ti.SaveAndReimport();var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"MAT_StoneGate_Door.mat");mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"StoneSlab_Albedo.png"));mat.SetTexture("_MainTex",mat.GetTexture("_BaseMap"));mat.SetColor("_BaseColor",new Color(.53f,.55f,.57f));mat.SetColor("_Color",mat.GetColor("_BaseColor"));EditorUtility.SetDirty(mat);
 var prefab=PrefabUtility.LoadPrefabContents(Root+"StoneGate.prefab");try{var panel=prefab.transform.Find("DoorPanel");foreach(var t in panel.Cast<Transform>().Where(t=>t.name=="FaceStone").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);foreach(var r in prefab.GetComponentsInChildren<MeshRenderer>())r.sharedMaterial=r.transform.IsChildOf(panel)?mat:wall;PrefabUtility.SaveAsPrefabAsset(prefab,Root+"StoneGate.prefab");}finally{PrefabUtility.UnloadPrefabContents(prefab);}AssetDatabase.SaveAssets();File.WriteAllText("ArtSource/Previews/B1StoneGates/appearance.txt","All housing/frame/guide renderers use exact wall material; door is a single continuous slab with new albedo. Existing animation and root transforms unchanged. Backup="+backup);}
}

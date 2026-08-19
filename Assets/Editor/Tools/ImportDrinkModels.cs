using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ImportDrinkModels
{
    [MenuItem("Tools/Dungeon Tavern/Import Barrel And Cup")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        const string root="Assets/DungeonTavern/Art/Models/";
        var report=new StringBuilder();
        foreach(var name in new[]{"Barrel_Wood_Horizontal_Tap","WoodenCup"})
        {
            string source="ArtSource/AIGenerated/"+name, dest=root+name, modelPath=dest+"/"+name+".fbx", prefabPath=dest+"/"+name+".prefab";
            bool existing=File.Exists(prefabPath);
            Directory.CreateDirectory(dest+"/Textures");
            File.Copy(source+"/base_basic_pbr.fbx",modelPath,true);
            foreach(var map in new[]{("diffuse","Diffuse"),("normal","Normal"),("metallic","Metallic")})
            {
                string path=dest+"/Textures/"+name+"_"+map.Item2+".png";
                bool hadSettings=File.Exists(path+".meta");
                File.Copy(source+"/texture_"+map.Item1+".png",path,true);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                if(!hadSettings)
                {
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    var reference=(TextureImporter)AssetImporter.GetAtPath(root+"SealControlCore/Textures/SealControlCore_"+map.Item2+".png");
                    var settings=new TextureImporterSettings();reference.ReadTextureSettings(settings);importer.SetTextureSettings(settings);
                    importer.maxTextureSize=reference.maxTextureSize;importer.textureCompression=reference.textureCompression;importer.SaveAndReimport();
                }
            }
            AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceSynchronousImport);
            if(!existing)
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
                var mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(root+"Barrel_Wood_Horizontal_Tap/MAT_Barrel_Wood_Horizontal_Tap.mat")){name="MAT_"+name};
                foreach(var map in new[]{("_BaseMap","Diffuse"),("_MainTex","Diffuse"),("_BumpMap","Normal"),("_MetallicGlossMap","Metallic")}) mat.SetTexture(map.Item1,AssetDatabase.LoadAssetAtPath<Texture2D>(dest+"/Textures/"+name+"_"+map.Item2+".png"));
                AssetDatabase.CreateAsset(mat,dest+"/MAT_"+name+".mat");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
                try {go.name=name;foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();PrefabUtility.SaveAsPrefabAsset(go,prefabPath);}
                finally{UnityEngine.Object.DestroyImmediate(go);}
            }
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var meshes=asset.GetComponentsInChildren<MeshFilter>();
            if(meshes.Length==0 || meshes.Any(m=>!m.sharedMesh) || asset.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterials.Any(m=>!m || !m.GetTexture("_BaseMap")))) throw new InvalidOperationException("Invalid model references: "+name);
            report.AppendLine(name+": PASS; meshes="+meshes.Length+"; "+(existing?"updated in place":"new Prefab"));
        }
        foreach(var t in Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.name=="Gate_East" && !EditorUtility.IsPersistent(t)))
        {
            report.AppendLine("Live long gate: "+t.name+" parent="+t.parent.name+" local="+t.localPosition+" rotation="+t.localEulerAngles+" scale="+t.localScale);
            foreach(Transform child in t) report.AppendLine("  "+child.name+" local="+child.localPosition+" rotation="+child.localEulerAngles+" scale="+child.localScale);
        }
        AssetDatabase.SaveAssets();File.WriteAllText("/tmp/dt-drinks-import.txt",report.ToString());Debug.Log(report.ToString());
    }
}

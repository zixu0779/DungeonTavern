using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DungeonTavern.UI;
public static class PortraitAndBarCheck {
 [MenuItem("Tools/Characters/Clean Portrait Test Objects")]
 public static void Clean(){foreach(var go in Resources.FindObjectsOfTypeAll<GameObject>().Where(g=>!EditorUtility.IsPersistent(g)&&(g.name=="PortraitRender"||g.name=="PortraitCamera")&&g.transform.position.x>9000).ToArray()){foreach(var mf in go.GetComponentsInChildren<MeshFilter>(true))if(mf.sharedMesh&&!EditorUtility.IsPersistent(mf.sharedMesh))UnityEngine.Object.DestroyImmediate(mf.sharedMesh);UnityEngine.Object.DestroyImmediate(go);}}
 [MenuItem("Tools/Characters/Check Dialogue Portraits")]
 public static void Check(){string folder=Path.Combine(Path.GetTempPath(),"dialogue-portraits");Directory.CreateDirectory(folder);foreach(var n in new[]{"Protagonist","Eve"}){var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Characters/"+n+"/"+n+".prefab"));var ui=new GameObject("PortraitCheck",typeof(RectTransform));try{var animator=actor.GetComponent<Animator>();animator.Rebind();animator.Play("Idle",0,0);animator.Update(0);var p=ui.AddComponent<TavernPortrait>();p.Initialize(ui.transform);p.Show(actor.transform,true);var raw=ui.GetComponentInChildren<RawImage>();var rt=(RenderTexture)raw.texture;if(!rt||!raw.enabled)throw new Exception("Portrait failed");var previous=RenderTexture.active;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);try{RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(folder+"/"+n+".png",tex.EncodeToPNG());}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(tex);}}finally{UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(ui);}}Debug.Log("Dialogue portraits rendered to "+folder);}
}

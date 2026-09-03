using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DungeonTavern.Gameplay.Interaction;
internal static class CustomerQueueReview
{
    public const string Output="ArtSource/Previews/CustomerQueue/";
    [MenuItem("Tools/Dungeon Tavern/Audit Customer Queue")]
    static void Audit()
    {
        Directory.CreateDirectory(Output);
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/Tavern/Tavern_Main.unity");
        if(!scene.isLoaded)throw new Exception("Open Tavern_Main first.");
        var report=new StringBuilder();
        foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
        {
            if(!(t.name.Contains("Menu")||t.name.Contains("Queue")||t.name.Contains("Seat")||t.name.Contains("Chair")||t.name.Contains("Stool")||t.name.Contains("Table")||t.name.Contains("GuestEntry")))continue;
            report.AppendLine(PathOf(t)+" pos="+t.position+" forward="+t.forward);
            if(t.name=="TavernMenuBoard")foreach(var r in t.GetComponentsInChildren<Renderer>(true))report.AppendLine("  Menu visual="+r.name+" "+r.bounds);
            var renderer=t.GetComponent<Renderer>();if(renderer!=null)report.AppendLine("  Bounds="+renderer.bounds);
            foreach(var c in t.GetComponents<Component>())if(c!=null&&c.GetType().Namespace!=null&&c.GetType().Namespace.StartsWith("DungeonTavern"))report.AppendLine("  "+c.GetType().Name+" "+EditorJsonUtility.ToJson(c));
        }
        foreach(var day in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BusinessDayController>(true)))
        {
            report.AppendLine("Business="+EditorJsonUtility.ToJson(day));
            var so=new SerializedObject(day);var prefab=so.FindProperty("customerPrefab").objectReferenceValue as GameObject;
            report.AppendLine("Customer prefab="+AssetDatabase.GetAssetPath(prefab)+" config="+EditorJsonUtility.ToJson(prefab.GetComponent<CustomerServicePoint>()));
        }
        File.WriteAllText(Output+"audit.txt",report.ToString());
    }
    [MenuItem("Tools/Dungeon Tavern/Align Customer Queue With Menu")]
    static void Align()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first.");
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/Tavern/Tavern_Main.unity");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var board=all.Single(t=>t.name=="TavernMenuBoard");
        var head=all.Single(t=>t.name=="MenuApproach");
        var queue=all.Select(t=>t.GetComponent<ServiceOrderQueue>()).Single(c=>c!=null);
        var points=queue.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Queue_")).OrderBy(t=>t.name).ToArray();
        var preserved=all.Where(t=>t.IsChildOf(board)||t.name.StartsWith("TableSet_")||t.name.StartsWith("Stool_")).ToDictionary(t=>t,t=>t.localToWorldMatrix);
        string backup="ArtSource/Backups/CustomerQueue_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);
        EditorSceneManager.SaveScene(scene);File.Copy(scene.path,backup+"/Tavern_Main.unity");
        var day=all.Select(t=>t.GetComponent<BusinessDayController>()).Single(c=>c!=null);
        string prefabPath=AssetDatabase.GetAssetPath(new SerializedObject(day).FindProperty("customerPrefab").objectReferenceValue);
        File.Copy(prefabPath,backup+"/Customer_Test.prefab");
        Undo.SetTransformParent(head,board,"Anchor ordering position to menu");
        Undo.RecordObject(head,"Set menu approach");head.position=new Vector3(board.position.x,0,board.position.z-1.25f);head.rotation=Quaternion.identity;
        Undo.SetTransformParent(queue.transform,board,"Anchor ordering queue to menu");
        Undo.RecordObject(queue.transform,"Set queue origin");queue.transform.position=head.position;queue.transform.rotation=Quaternion.identity;
        for(int i=0;i<points.Length;i++){Undo.RecordObject(points[i],"Set queue marker");points[i].position=head.position+Vector3.right*(i*1.2f);}
        Undo.RecordObject(queue,"Bind queue");queue.BindMenu(head);queue.Configure(points);EditorUtility.SetDirty(queue);
        var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try{var so=new SerializedObject(root.GetComponent<CustomerServicePoint>());so.FindProperty("orderingDuration").floatValue=3f;so.FindProperty("orderDisplayDuration").floatValue=1.5f;so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,prefabPath);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var menu=all.Select(t=>t.GetComponent<TavernMenuSystem>()).Single(c=>c!=null);
        var menuSo=new SerializedObject(menu);var dishes=menuSo.FindProperty("dishes");
        for(int i=0;i<dishes.arraySize;i++){var dish=dishes.GetArrayElementAtIndex(i);if(dish.FindPropertyRelative("item").enumValueIndex==(int)HeldItem.TestDrink)dish.FindPropertyRelative("eatingSeconds").floatValue=8f;}
        menuSo.ApplyModifiedProperties();
        foreach(var pair in preserved)if(pair.Key.localToWorldMatrix!=pair.Value)throw new Exception("Model moved: "+pair.Key.name);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Output+"alignment.txt",$"Menu {board.position}; ordering head {head.position}; {points.Length} queue markers spaced 1.2m; thinking 3s; order display 1.5s; drink 8s. Models unchanged. Backup {backup}");
        Selection.activeGameObject=queue.gameObject;
    }
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
}

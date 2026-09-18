using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonTavern.Prototypes.Rotation25D;

public static class WallCutoutGroupSetup
{
    const string Main="Assets/Scenes/Tavern/Tavern_Main.unity";
    const string B1="Assets/Scenes/SealRoom/SealRoom_B1.unity";
    [MenuItem("Tools/Demo Flow/Audit Wall Groups")]
    public static void Audit()
    {
        var report=new StringBuilder();
        foreach(var path in new[]{Main,B1})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(!DialogueOcclusionFader.IsWall(t))continue;
                var r=t.GetComponent<Renderer>();
                report.AppendLine($"{scene.name} {PathOf(t)} | pos={t.position:F3} rot={t.eulerAngles:F1} scale={t.lossyScale:F3} | mesh={(r?r.bounds.ToString():"-")} | col={t.GetComponents<Collider>().Length} prefabRoot={PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)}");
            }
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        File.WriteAllText("/tmp/wall-groups-audit.txt",report.ToString());
    }
    [MenuItem("Tools/Demo Flow/Build Wall Groups")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode before arranging wall groups");
        string backup="/tmp/wall-groups-backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
        var report=new StringBuilder();
        foreach(var path in new[]{Main,B1})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            // Save the user's existing edits before backing up the authoritative scene.
            EditorSceneManager.SaveScene(scene);File.Copy(path,backup+"/"+Path.GetFileName(path));
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var matrices=all.ToDictionary(t=>t,t=>t.localToWorldMatrix);
            var meshes=all.Select(t=>t.GetComponent<MeshFilter>()).Where(m=>m).ToDictionary(m=>m,m=>m.sharedMesh);
            int colliderCount=all.Sum(t=>t.GetComponents<Collider>().Length);
            var walls=all.Single(t=>t.name==(path==Main?"Walls":"Walls_Stone"));
            if(walls.GetComponentInChildren<WallCutoutGroup>(true))throw new Exception("Wall groups already exist; edit their hierarchy directly");
            if(path==Main)BuildMain(walls);else BuildBasement(walls,all);
            foreach(var pair in matrices)
            {
                if(!pair.Key)throw new Exception("Original object removed");
                var current=pair.Key.localToWorldMatrix;
                for(int i=0;i<16;i++)if(Mathf.Abs(current[i]-pair.Value[i])>.0001f)throw new Exception("World transform changed: "+PathOf(pair.Key));
            }
            foreach(var pair in meshes)if(pair.Key.sharedMesh!=pair.Value)throw new Exception("Mesh changed: "+pair.Key.name);
            if(all.Sum(t=>t.GetComponents<Collider>().Length)!=colliderCount)throw new Exception("Collision count changed");
            foreach(var t in all)
                if(t.GetComponent<Renderer>() && DialogueOcclusionFader.IsWall(t) && !t.GetComponentInParent<WallCutoutGroup>())
                    throw new Exception("Ungrouped renderer: "+PathOf(t));
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            report.AppendLine(scene.name+": world transforms, meshes and colliders preserved");
            foreach(var g in walls.GetComponentsInChildren<WallCutoutGroup>(true))
                report.AppendLine(PathOf(g.transform)+" : "+g.GetComponentsInChildren<Renderer>(true).Length+" renderers");
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        report.AppendLine("Backup: "+backup);File.WriteAllText("/tmp/wall-groups-build.txt",report.ToString());
    }
    static Transform Group(Transform parent,string name)
    {
        var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Group wall faces");
        go.transform.SetParent(parent,false);Undo.AddComponent<WallCutoutGroup>(go);return go.transform;
    }
    static void Move(Transform item,Transform parent)=>Undo.SetTransformParent(item,parent,"Group wall faces");
    sealed class Face
    {
        public Transform item;public Vector3 tangent,normal;public float offset,min,max;
    }
    static Face FaceOf(Transform t)
    {
        var tangent=Vector3.ProjectOnPlane(t.right,Vector3.up).normalized;
        if(tangent.x<-.001f || (Mathf.Abs(tangent.x)<.001f && tangent.z<0))tangent=-tangent;
        var normal=Vector3.Cross(Vector3.up,tangent);float min=float.PositiveInfinity,max=float.NegativeInfinity;
        foreach(var r in t.GetComponentsInChildren<Renderer>(true))
        {
            var b=r.bounds;float center=Vector3.Dot(b.center,tangent),extent=Vector3.Dot(b.extents,new Vector3(Mathf.Abs(tangent.x),0,Mathf.Abs(tangent.z)));
            min=Mathf.Min(min,center-extent);max=Mathf.Max(max,center+extent);
        }
        return new Face{item=t,tangent=tangent,normal=normal,offset=Vector3.Dot(t.position,normal),min=min,max=max};
    }
    static void BuildMain(Transform walls)
    {
        var containers=new[]{walls.Find("Exterior Walls"),walls.Find("Interior Walls"),walls.Find("Doors")};
        var faces=containers.SelectMany(t=>t.Cast<Transform>()).Where(t=>t.GetComponent<Renderer>() || t.GetComponent<DungeonTavern.Tavern25D.DoorStateController>()).Select(FaceOf).ToList();
        int index=0;
        while(faces.Count>0)
        {
            var seed=faces[0];
            var plane=faces.Where(f=>Vector3.Dot(f.tangent,seed.tangent)>.999f && Mathf.Abs(f.offset-seed.offset)<.08f).OrderBy(f=>f.min).ToArray();
            Transform group=null;float end=float.NegativeInfinity;
            foreach(var face in plane)
            {
                if(group==null || face.min>end+.3f)
                {
                    string direction=Mathf.Abs(seed.normal.x)>.99f?"X_"+face.item.position.x.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture):Mathf.Abs(seed.normal.z)>.99f?"Z_"+face.item.position.z.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture):"Diagonal";
                    group=Group(walls,$"Wall_{++index:00}_{direction}");end=face.max;
                }
                Move(face.item,group);end=Mathf.Max(end,face.max);faces.Remove(face);
            }
        }
    }
    static void BuildBasement(Transform walls,Transform[] all)
    {
        var south=Group(walls,"SouthWall");var east=Group(walls,"EastWall");var north=Group(walls,"NorthWall");var west=Group(walls,"WestWall");
        foreach(var t in walls.Cast<Transform>().ToArray())
        {
            if(t.name.StartsWith("South_"))Move(t,south);
            else if(t.name.StartsWith("East_"))Move(t,east);
            else if(t.name.StartsWith("North_"))Move(t,north);
            else if(t.name.StartsWith("West_"))Move(t,west);
            else if(t.name.StartsWith("Corner_"))Move(t,Group(walls,t.name.Split('_')[1]+"Corner"));
        }
        Move(all.Single(t=>t.name=="RubbleWall_Arch_6m"),west);
        Move(all.Single(t=>t.name=="RubbleWall_Return_6m"),north);
        Move(all.Single(t=>t.name=="NorthSide" && t.parent.name=="StairRearEnclosure"),north);
        Move(all.Single(t=>t.name=="RearEnd" && t.parent.name=="StairRearEnclosure"),Group(walls,"StairRearWall"));
        Move(all.Single(t=>t.name=="StoneGates"),east);
    }
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
}

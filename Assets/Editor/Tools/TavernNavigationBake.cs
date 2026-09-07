using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using DungeonTavern.Tavern25D;

static class TavernNavigationBake
{
    [MenuItem("Tools/Dungeon Tavern/Bake Tavern Navigation")]
    static void Bake()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode before baking");
        var surface=UnityEngine.Object.FindObjectsByType<NavMeshSurface>().Single();
        var settings=surface.GetBuildSettings();
        settings.agentClimb=.25f;
        settings.agentRadius=.34f;
        settings.agentHeight=1.5f;
        settings.overrideVoxelSize=true;settings.voxelSize=.06f;
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        // Use the installed navigation package's collection rules, including modifiers and excluded agents.
        // Automatic doors are traversable routes, even when their current visual state is closed.
        var blockers=UnityEngine.Object.FindObjectsByType<AutomaticDoorTrigger>()
            .Where(t=>t.isActiveAndEnabled&&t.TargetDoor!=null&&t.TargetDoor.BlockingCollider!=null)
            .Select(t=>t.TargetDoor.BlockingCollider).Distinct().ToArray();
        var enabled=blockers.Select(c=>c.enabled).ToArray();
        List<NavMeshBuildSource> sources;
        try
        {
            foreach(var collider in blockers)collider.enabled=false;
            sources=(List<NavMeshBuildSource>)typeof(NavMeshSurface).GetMethod("CollectSources",flags).Invoke(surface,null);
        }
        finally{for(int i=0;i<blockers.Length;i++)blockers[i].enabled=enabled[i];}
        // Thin frame meshes can lose their vertical jambs during voxelization. Keep their physical clearance.
        foreach(var door in UnityEngine.Object.FindObjectsByType<DoorStateController>().Where(d=>d.name.StartsWith("Door_Small_Stone")))
        {
            var frame=door.transform.Find("OuterWallOutline").GetComponent<MeshFilter>().sharedMesh.bounds;
            var hinge=door.transform.Find("DoorHinge");
            var leaf=hinge.Find("DoorLeaf").GetComponent<MeshFilter>().sharedMesh.bounds;
            float left=hinge.localPosition.x+leaf.min.x, right=hinge.localPosition.x+leaf.max.x;
            void Jamb(float min,float max)
            {
                sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,area=NavMesh.GetAreaFromName("Not Walkable"),
                    size=new Vector3(max-min,frame.size.y,frame.size.z),
                    transform=door.transform.localToWorldMatrix*Matrix4x4.Translate(new Vector3((min+max)*.5f,frame.center.y,frame.center.z))});
            }
            Jamb(frame.min.x,left);Jamb(right,frame.max.x);
        }
        var bounds=(Bounds)typeof(NavMeshSurface).GetMethod("CalculateWorldBounds",flags).Invoke(surface,new object[]{sources});
        var data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,surface.transform.position,surface.transform.rotation);
        if(data==null)throw new Exception("NavMesh bake failed");
        var old=surface.navMeshData;
        surface.RemoveData();
        EditorUtility.CopySerialized(data,old);UnityEngine.Object.DestroyImmediate(data);
        EditorUtility.SetDirty(old);surface.AddData();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);EditorSceneManager.SaveScene(surface.gameObject.scene);
        System.IO.File.WriteAllText("/tmp/demo-nav-bake.txt",$"Baked {sources.Count} sources, radius .34m / step .25m / height 1.5m, saved {AssetDatabase.GetAssetPath(old)}");
    }
}

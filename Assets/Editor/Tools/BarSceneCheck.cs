using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonTavern.Tavern25D;

[InitializeOnLoad]
public static class BarSceneCheck
{
    static GameObject testAgent;
    static DoorStateController[] testDoors;
    static int testStep;
    static float nextTime;
    static StringBuilder testReport;
    static BarSceneCheck(){EditorApplication.playModeStateChanged+=OnPlayState;}
    [MenuItem("Tools/Dungeon Tavern/Bar/Test Replaced Scene")]
    public static void TestScene()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Start in Edit Mode");
        SessionState.SetBool("TestReplacedBar",true);EditorApplication.EnterPlaymode();
    }
    static void OnPlayState(PlayModeStateChange state)
    {
        if(!SessionState.GetBool("TestReplacedBar",false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){testStep=-1;nextTime=Time.time+2;testReport=new StringBuilder();EditorApplication.update+=TestTick;}
        if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("TestReplacedBar",false);EditorApplication.update-=TestTick;}
    }
    static void TestTick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.isPaused || Time.time<nextTime)return;
        try
        {
            if(testStep==-1)
            {
                var scene=SceneManager.GetSceneByPath("Assets/Scenes/Tavern/Tavern_Main.unity");
                var bar=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Bar");
                for(var p=bar;p;p=p.parent)p.gameObject.SetActive(true);
                testDoors=bar.GetComponentsInChildren<DoorStateController>();
                testAgent=new GameObject("BarSceneValidationAgent");testAgent.transform.position=new Vector3(1000,0,1000);
                testAgent.AddComponent<DoorPassageAgent>();var capsule=testAgent.AddComponent<CapsuleCollider>();capsule.radius=.28f;capsule.height=1.6f;capsule.center=Vector3.up*.8f;
                var rb=testAgent.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
                testStep=0;nextTime=Time.time+1;return;
            }
            int index=testStep/4,step=testStep%4;
            if(index==2){FinishTest("PASS: both scene doors opened independently via physics, held open while occupied, closed after exit, and restored passage collision.\n"+testReport);return;}
            var door=testDoors[index];var other=testDoors[1-index];var pivot=door.transform.Find("Pivot");
            if(step==0)
            {
                if(door.IsOpen || !door.BlockingCollider.enabled)throw new InvalidOperationException("Initial gate state");
                var a=door.transform.TransformPoint(new Vector3(-.44f,0,1.6f));var b=door.transform.TransformPoint(new Vector3(-.44f,0,-1.6f));
                if(!UnityEngine.AI.NavMesh.SamplePosition(a,out var ah,1.2f,UnityEngine.AI.NavMesh.AllAreas) || !UnityEngine.AI.NavMesh.SamplePosition(b,out var bh,1.2f,UnityEngine.AI.NavMesh.AllAreas))throw new InvalidOperationException(door.name+" navigation sample failed");
                var path=new UnityEngine.AI.NavMeshPath();if(!UnityEngine.AI.NavMesh.CalculatePath(ah.position,bh.position,UnityEngine.AI.NavMesh.AllAreas,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)throw new InvalidOperationException(door.name+" navigation route incomplete");
                testReport.AppendLine(door.name+": navigation path complete, corners="+path.corners.Length);
                testAgent.transform.position=door.transform.TransformPoint(new Vector3(-.44f,0,0));Physics.SyncTransforms();
            }
            if(step==1 || step==2)
            {
                if(!door.IsOpen || door.BlockingCollider.enabled || Quaternion.Angle(pivot.localRotation,Quaternion.identity)<80 || other.IsOpen)throw new InvalidOperationException(door.name+" automatic opening failed");
                if(step==2){testAgent.transform.position=door.transform.TransformPoint(new Vector3(-.44f,0,3));Physics.SyncTransforms();}
            }
            if(step==3 && (door.IsOpen || !door.BlockingCollider.enabled || Quaternion.Angle(pivot.localRotation,Quaternion.identity)>.1f))throw new InvalidOperationException(door.name+" automatic closing failed");
            testStep++;nextTime=Time.time+1.2f;
        }
        catch(Exception e){FinishTest("FAIL: "+e+"\n"+testReport);}
    }
    static void FinishTest(string report)
    {
        EditorApplication.update-=TestTick;File.WriteAllText("/tmp/dt-bar-scene-test.txt",report);
        if(testAgent)UnityEngine.Object.Destroy(testAgent);EditorApplication.ExitPlaymode();
    }
}

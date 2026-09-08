using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

static class DemoServiceCheck
{
    const string Output="/tmp/demo-service-check.txt";
    static BusinessDayController day;
    static Day1EveActor eve;
    static CupDispenserPoint dispenser;
    static PlayerHands hands;
    static double started;
    static readonly List<CustomerServicePoint> guests=new();
    static readonly HashSet<CustomerSeatingKind> kinds=new();
    static bool cupChecked, reactivated, concurrentBran, facingChecked, eveChecked;
    static int orders;
    static float maximumOverlap;
    static object Get(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static void Log(string text)=>File.AppendAllText(Output,text+"\n");
    static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    [MenuItem("Tools/Demo Adjustment/Check Service In Play Mode")]
    static void Run()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play Mode and wait for B1 load first");
        File.WriteAllText(Output,"Live demo service check; narrative overlay temporarily disabled in this Play session.\n");
        var loader=UnityEngine.Object.FindAnyObjectByType<InitialAdditiveSceneLoader>();
        typeof(InitialAdditiveSceneLoader).GetMethod("SetHostContentVisible",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(loader,new object[]{true});
                var basement=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SealRoom_B1");
                if(basement.isLoaded)UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(basement);
        foreach(var story in UnityEngine.Object.FindObjectsByType<Day1NarrativeController>())story.enabled=false;
        var player=UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();
        player.MovementInputEnabled=false;player.GetComponentInChildren<CharacterModelMotion>().EndFullBodyAction();
        var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(29,0,14);cc.enabled=true;
        var camera=UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>();camera.EndDialogueFraming();
        hands=player.GetComponent<PlayerHands>();hands.Clear();
        day=UnityEngine.Object.FindAnyObjectByType<BusinessDayController>();
        dispenser=UnityEngine.Object.FindAnyObjectByType<CupDispenserPoint>();
        eve=UnityEngine.Object.FindObjectsByType<Day1EveActor>(FindObjectsInactive.Include).Single();
        eve.gameObject.SetActive(true);
        var point=GameObject.Find("EveOpeningSwitchGuide").transform;
        eve.BeginOpeningSwitchGuidance(point,"拉动拉杆打开酒馆。");
        guests.Clear();kinds.Clear();orders=0;maximumOverlap=0;cupChecked=reactivated=concurrentBran=facingChecked=eveChecked=false;
        day.CustomerSpawned+=Spawned;
        UnityEngine.Object.FindAnyObjectByType<TavernMenuSystem>().OrderRegistered+=Order;
        Assert(day.BeginDay(),"Could not start service");
        Assert(day.DemoService && (float)Get(day,"ordinaryArrivalInterval")==6,"Scene demo configuration");
        Assert(!dispenser.CupReady,"Dispenser should initially be off");
        Assert(UnityEngine.Object.FindObjectsByType<PrototypeHud>(FindObjectsInactive.Include).Length==0,"Debug HUD remains");
        started=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Update;
    }
    [MenuItem("Tools/Demo Adjustment/Capture Door In Play Mode")]
    static void CaptureDoor()
    {
        if(!EditorApplication.isPlaying)return;
        var player=UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();
        var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.position=new Vector3(35.7f,0,20);body.enabled=true;
        player.transform.rotation=Quaternion.LookRotation(Vector3.right);
        UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>().FollowTarget=player.transform;
        double time=EditorApplication.timeSinceStartup;
        void Capture(){if(EditorApplication.timeSinceStartup-time<1)return;ScreenCapture.CaptureScreenshot("/tmp/demo-door.png");EditorApplication.update-=Capture;}
        EditorApplication.update+=Capture;
    }
    static void Spawned(CustomerServicePoint c)
    {
        guests.Add(c);kinds.Add(c.SeatingKind);
        if(c.CustomerName!="Bran" && guests.Any(g=>g!=null&&g.CustomerName=="Bran"))concurrentBran=true;
        Log($"Spawn {c.CustomerName}: {c.SeatingKind}, {Time.time:F1}, {c.transform.position}");
    }
    static void Order(){orders++;}
    static void Update()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=Update;return;}
        try
        {
            if(orders>0&&!cupChecked&&dispenser.CupReady)
            {
                Assert(dispenser.CupReady,"New order did not activate dispenser");
                Assert(dispenser.Interact(hands)&&hands.CurrentItem==HeldItem.EmptyCup&&!dispenser.CupReady,"Take cup starts replacement/shutdown according to deficit");
                hands.Clear();cupChecked=true;Log("PASS drink demand activates; take cup preserves inventory");
            }
            if(cupChecked&&orders>1&&dispenser.CupReady)reactivated=true;
            foreach(var c in guests.Where(c=>c!=null))
            {
                var body=c.GetComponent<CharacterController>();
                Assert(body.enabled&&!body.isTrigger,"Customer physical controller disabled");
                Assert(!c.GetComponent<NavMeshAgent>().updatePosition,"Navigation bypasses physical movement");
                if(c.State==CustomerOrderState.WaitingForFood&&c.AssignedSeat?.Table!=null)
                {
                    var dir=c.AssignedSeat.Table.transform.position-c.transform.position;dir.y=0;
                    Assert(Vector3.Dot(c.transform.forward,dir.normalized)>.98f,$"Seated customer {c.CustomerName} faces away from table: pos={c.transform.position}, forward={c.transform.forward}, toward={dir.normalized}, agentRotation={c.GetComponent<NavMeshAgent>().updateRotation}");
                    facingChecked=true;
                }
            }
            for(int a=0;a<guests.Count;a++)
                for(int b=a+1;b<guests.Count;b++)
                {
                    var left=guests[a];var right=guests[b];if(left==null||right==null)continue;
                    if(Physics.ComputePenetration(left.GetComponent<CharacterController>(),left.transform.position,left.transform.rotation,
                        right.GetComponent<CharacterController>(),right.transform.position,right.transform.rotation,out _,out float depth))
                        maximumOverlap=Mathf.Max(maximumOverlap,depth);
                }
            Assert(maximumOverlap<.12f,$"Customers penetrate by {maximumOverlap:F3}m");
            if(eve.IsOpeningGuidanceReady)
            {
                var dir=UnityEngine.Object.FindAnyObjectByType<FloorLeverPoint>().transform.position-eve.transform.position;dir.y=0;
                Assert(Vector3.Dot(eve.transform.forward,dir.normalized)>.98f,"Eve faces away from lever");eveChecked=true;
            }
            double elapsed=EditorApplication.timeSinceStartup-started;
            if(elapsed>55)
            {
                Log($"Status guests={guests.Count}, orders={orders}, active={day.ActiveCustomers}, kinds={string.Join(",",kinds)}, seated={facingChecked}, eve={eveChecked}");
                foreach(var c in guests.Where(c=>c!=null))Log($"{c.CustomerName} {c.State} {c.transform.position}");
                Assert(guests.Count>5&&day.ActiveCustomers>5,"Five-customer cap remains");
                Assert(kinds.Count==3&&concurrentBran,"Missing guaranteed types or Bran blocks arrivals");
                Assert(cupChecked&&reactivated&&facingChecked&&eveChecked,"Incomplete service/orientation checks");
                ScreenCapture.CaptureScreenshot("/tmp/demo-service.png");
                day.StopAcceptingCustomers();
                Assert(day.WaitingCustomers==0,"Closing did not stop new arrivals");
                Log($"PASS all checks; maximum customer body overlap {maximumOverlap:F3}m; admission stopped and existing guests retained.");
                EditorApplication.update-=Update;
            }
        }
        catch(Exception e){Log("FAIL "+e);EditorApplication.update-=Update;}
    }
}

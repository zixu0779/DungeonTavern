using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

[InitializeOnLoad]
internal static class SeatingPlayCheck
{
    const string Key="DungeonTavern.SeatingPlayCheck";
    static readonly string Output=(Path.Combine(Path.GetTempPath(), "DungeonTavern/Seating") + Path.DirectorySeparatorChar);
    [Serializable] class SavedScene {public string path;public bool active;public bool loaded;}
    [Serializable] class SavedSetup {public SavedScene[] scenes;}
    static BusinessDayController day;
    static ServiceOrderQueue queue;
    static PlayerHands hands;
    static PrototypeCameraOrbit camera;
    static Transform focus;
    static double started;
    static bool configured;
    static int orders;
    static int finished;
    static int maxWaiting;
    static readonly List<CustomerServicePoint> spawnOrder = new();
    static SeatRegistry registry;
    static DungeonTavern.Tavern25D.Narrative.Day1EveActor eve;
    static readonly Dictionary<CustomerServicePoint,float> changed=new();
    static readonly HashSet<string> captures=new();
    static SeatingPlayCheck(){EditorApplication.playModeStateChanged+=Mode;}
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    [MenuItem("Tools/Dungeon Tavern/Seating/Test Mixed Customers")]
    static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first.");
        Directory.CreateDirectory(Output);EditorSceneManager.SaveOpenScenes();
        var setup=new SavedSetup{scenes=EditorSceneManager.GetSceneManagerSetup().Select(s=>new SavedScene{path=s.path,active=s.isActive,loaded=s.isLoaded}).ToArray()};
        if(setup.scenes.Any(s=>string.IsNullOrEmpty(s.path)))throw new Exception("Save untitled scenes first.");
        SessionState.SetString(Key+"Setup",JsonUtility.ToJson(setup));SessionState.SetBool(Key,true);
        File.WriteAllText(Output+"playcheck.txt","Mixed solo and party customers; real navigation; temporary schedule; automated serving/payment.\n");
        EditorSceneManager.OpenScene("Assets/Scenes/Tavern/Tavern_Main.unity",OpenSceneMode.Single);
        EditorApplication.isPlaying=true;
    }
    static void Mode(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;configured=false;day=null;orders=finished=maxWaiting=0;changed.Clear();captures.Clear();spawnOrder.Clear();EditorApplication.update+=Update;}
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update-=Update;SessionState.SetBool(Key,false);
            var setup=JsonUtility.FromJson<SavedSetup>(SessionState.GetString(Key+"Setup",""));
            EditorSceneManager.RestoreSceneManagerSetup(setup.scenes.Select(s=>new SceneSetup{path=s.path,isActive=s.active,isLoaded=s.loaded}).ToArray());
        }
    }
    static void Log(string line)=>File.AppendAllText(Output+"playcheck.txt",line+"\n");
    static void Stop(string message){Log(message);EditorApplication.update-=Update;EditorApplication.isPlaying=false;}
    static void Capture(string name)
    {
        if(captures.Add(name)){ScreenCapture.CaptureScreenshot(Output+name+".png");Log("Game View captured: "+name);}
    }
    static void OnState(CustomerServicePoint c,CustomerOrderState state)
    {
        float elapsed=changed.TryGetValue(c,out var last)?Time.time-last:0;changed[c]=Time.time;
        Log($"{Time.time:F2} {c.CustomerName}: {state}; previous duration {elapsed:F2}; position {c.transform.position}");
        if(state==CustomerOrderState.Ordering)
        {
            int expected=orders+1;
            if(c != spawnOrder[expected - 1])throw new Exception("FIFO violated: expected " + expected);
            if(Vector3.Distance(c.transform.position,queue.MenuAnchor.position)>.6f)throw new Exception("Ordering away from menu");
        }
        if(state==CustomerOrderState.ShowingOrder){if(elapsed<2.8f)throw new Exception("Thinking too short");orders++;}
        if(state==CustomerOrderState.FindingSeat&&elapsed<1.4f)throw new Exception("Order bubble skipped");
        if(state==CustomerOrderState.WaitingForFood && Vector3.Distance(c.transform.position,c.AssignedSeat.Position)>.6f)
            throw new Exception("Customer stopped too far from assigned seat");
        if(state==CustomerOrderState.Finished)finished++;
    }
    static void Update()
    {
        if(!EditorApplication.isPlaying)return;
        try
        {
            if(EditorApplication.timeSinceStartup-started>180){Stop("FAIL timeout");return;}
            if(!configured)
            {
                if(Time.time<2)return;
                var loader=UnityEngine.Object.FindAnyObjectByType<InitialAdditiveSceneLoader>();
                if(loader!=null)typeof(InitialAdditiveSceneLoader).GetMethod("SetHostContentVisible",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(loader,new object[]{true});
                var basement=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SealRoom_B1");
                if(basement.isLoaded)UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(basement);
                // Disable only the opening overlay in this temporary test run; production narrative is unchanged.
                foreach(var narrative in UnityEngine.Object.FindObjectsByType<DungeonTavern.Tavern25D.Narrative.Day1NarrativeController>()) narrative.enabled=false;
                UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>()?.EndDialogueFraming();
                day=UnityEngine.Object.FindAnyObjectByType<BusinessDayController>();queue=UnityEngine.Object.FindAnyObjectByType<ServiceOrderQueue>();
                if(day==null||queue==null)return;
                var schedule=new List<CustomerScheduleEntry>();
                for(int i=0;i<6;i++)
                {
                    var entry=new CustomerScheduleEntry();entry.Configure("SeatTest_"+(i+1),.5f+i*2f,HeldItem.TestDrink,Color.white);
                    entry.SetArrivalKind(i==2||i==3 ? CustomerArrivalKind.Party : i==1 ? CustomerArrivalKind.Sociable : CustomerArrivalKind.Solitary);
                    schedule.Add(entry);
                }
                Set(day,"ordinaryArrivals",0); Set(day,"demoService",true);
                registry=UnityEngine.Object.FindAnyObjectByType<SeatRegistry>();registry.RefreshSeats();
                if(registry.SeatCount!=42||registry.Tables.Count!=9)throw new Exception("Scene seats not bound");
                var path=new NavMeshPath();
                if(!NavMesh.SamplePosition(queue.MenuAnchor.position,out var origin,.5f,NavMesh.AllAreas))throw new Exception("Menu not on NavMesh");
                foreach(var point in registry.Seats)
                {
                    if(!NavMesh.SamplePosition(point.Position,out var hit,.6f,NavMesh.AllAreas)
                        ||!NavMesh.CalculatePath(origin.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                        throw new Exception("Unreachable seat: "+point.transform.parent.name+"/"+point.name+" at "+point.Position);
                    Log("Reachable: "+point.transform.parent.name+"/"+point.name+" sample offset="+Vector3.Distance(hit.position,point.Position).ToString("F2"));
                }
                Set(day,"customers",schedule);
                day.CustomerSpawned+=c=>
                {
                    // Exercise the three tight long-table middle approaches explicitly; rule selection is checked separately.
                    int tightTable = c.CustomerName == "SeatTest_1" ? 1 : c.CustomerName == "SeatTest_2" ? 2 : c.CustomerName == "SeatTest_5" ? 3 : 0;
                    if (tightTable > 0)
                    {
                        var tightSeat = registry.Seats.Single(s => s.Table != null && s.Table.name == "TableSet_Rectangular_Long_" + tightTable && s.name == "Seat_02");
                        registry.Release(c);
                        if (!tightSeat.TryReserve(c)) throw new Exception("Tight-seat reservation failed");
                        Set(c, "assignedSeat", tightSeat);
                    }
                    spawnOrder.Add(c);changed[c]=Time.time;c.StateChanged+=state=>OnState(c,state);
                    Log($"Spawn {c.CustomerName}: {c.SeatingKind}, party={c.PartyId}, seat={c.AssignedSeat.transform.parent.name}/{c.AssignedSeat.name}");
                };
                hands=new GameObject("QueueTestHands").AddComponent<PlayerHands>();
                camera=UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>();focus=new GameObject("QueueTestCameraFocus").transform;
                focus.position=queue.MenuAnchor.position+Vector3.right*1.2f;camera.FollowTarget=focus;
                EditorApplication.ExecuteMenuItem("Window/General/Game");
                if(!day.BeginDay())throw new Exception("Cannot start test business day");
                eve=UnityEngine.Object.FindObjectsByType<DungeonTavern.Tavern25D.Narrative.Day1EveActor>(FindObjectsInactive.Include).Single();
                eve.gameObject.SetActive(true);
                var eveStart=new Vector3(40,0,20);
                eve.GetComponent<NavMeshAgent>().Warp(eveStart);eve.transform.position=eveStart;
                var player=UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();
                player.MovementInputEnabled=false;player.GetComponentInChildren<CharacterModelMotion>().EndFullBodyAction();
                var playerBody=player.GetComponent<CharacterController>();playerBody.enabled=false;player.transform.position=new Vector3(37,0,20);playerBody.enabled=true;
                eve.BeginOpeningSwitchGuidance(GameObject.Find("EveOpeningSwitchGuide").transform,"去拉杆旁边。");
                Time.timeScale=2f;
                configured=true;return;
            }
            var customers=UnityEngine.Object.FindObjectsByType<CustomerServicePoint>();
            foreach(var table in registry.Tables)
            {
                var occupants=table.Seats.Where(s=>s.Occupant!=null).Select(s=>s.Occupant).ToArray();
                if(table.TableType==SeatingTableType.LargeRound&&occupants.Length>1
                    &&(occupants.Any(c=>c.PartyId==0)||occupants.Select(c=>c.PartyId).Distinct().Count()!=1))
                    throw new Exception("Large round table shared across parties");
            }
            var queued=customers.Where(c=>c.State==CustomerOrderState.QueueingForOrder).ToArray();
            maxWaiting=Mathf.Max(maxWaiting,queued.Length);
            if(customers.Count(c=>c.State is CustomerOrderState.Ordering or CustomerOrderState.ShowingOrder)>1)throw new Exception("Two customers own menu simultaneously");
            foreach(var c in customers)
            {
                float elapsed=Time.time-changed[c];
                if(c.State==CustomerOrderState.Ordering&&elapsed>2.2f)
                {
                    focus.position=queue.MenuAnchor.position+Vector3.right*1.2f;
                    Capture("ThinkingAndQueue");
                }
                if(c.State==CustomerOrderState.ShowingOrder&&elapsed>.4f)Capture("OrderConfirmed");
                if(c.State==CustomerOrderState.WaitingForFood&&elapsed>1.5f&&!c.AssignedSeat.IsStanding)
                {
                    var animator=c.GetComponentInChildren<Animator>();
                    var hips=animator.GetBoneTransform(HumanBodyBones.Hips).position;
                    var target=c.AssignedSeat.SittingSurface+Vector3.up*.12f;
                    if(Vector3.Distance(hips,target)>.045f)throw new Exception($"Seating misses stool: {c.CustomerName} hips={hips} surface={target}");
                }
                if(c.State==CustomerOrderState.WaitingForFood&&elapsed>2)
                {
                    var portion=c.Order.Portions.FirstOrDefault(p=>!p.Delivered);
                    if(portion!=null){hands.TryHold(portion.Item);if(!c.Interact(hands))throw new Exception("Delivery rejected");}
                }
                if(c.CustomerName=="SeatTest_3_1"&&c.State==CustomerOrderState.Eating&&elapsed>.8f)
                {
                    camera.FollowTarget=c.transform;
                    if(elapsed>1.2f)Capture("Eating");
                }
                if(c.State==CustomerOrderState.AwaitingSettlement&&elapsed>1)
                {
                    if(c.CustomerName=="SeatTest_3_1")Capture("Settlement");
                    if(elapsed>2&&!c.Interact(hands))throw new Exception("Settlement rejected");
                }
            }
            if(day.State==BusinessDayState.Completed)
            {
                if(orders!=spawnOrder.Count||finished!=spawnOrder.Count||maxWaiting<2||spawnOrder.Count<8)throw new Exception($"Insufficient coverage: orders={orders} exits={finished} queued={maxWaiting}");
                if(!eve.IsOpeningGuidanceReady)throw new Exception($"Eve did not reach lever: pos={eve.transform.position}, path={eve.GetComponent<NavMeshAgent>().pathStatus}, remaining={eve.GetComponent<NavMeshAgent>().remainingDistance}");
                if(registry.Seats.Any(s=>!s.IsAvailable))throw new Exception("Reservation leak after completion");
                Log("PASS Eve reached the lever from storage with the player occupying the direct doorway.");
                Stop($"PASS: 42 seats + 8 standing points reachable; {orders} orders in FIFO sequence, {finished} exits, peak {maxWaiting} waiting. Thinking and order-display duration verified; only one menu owner; full service completed.");
            }
        }
        catch(Exception ex){Stop("FAIL "+ex);}
    }
}

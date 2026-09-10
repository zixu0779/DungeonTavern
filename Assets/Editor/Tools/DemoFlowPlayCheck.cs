using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

[InitializeOnLoad]
static class DemoFlowPlayCheck
{
    const string Key="DungeonTavern.DemoFlowCheck";
    const string Output="/tmp/demo-flow-playcheck.txt";
    static double started;
    static readonly List<string> runtimeErrors=new();
    static void CaptureError(string message,string trace,LogType type) { if(type==LogType.Exception||type==LogType.Error)runtimeErrors.Add(message); }
    static PrototypePlayerMover player;
    static PlayerHands hands;
    static TavernMenuSystem menu;
    static CupDispenserPoint dispenser;
    static CupDispenserActivation activation;
    static PrototypeCameraOrbit orbit;
    static object Get(object target,string field)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    static void Log(string text)=>File.AppendAllText(Output,text+"\n");
    static DemoFlowPlayCheck(){EditorApplication.playModeStateChanged+=Mode;}
    [MenuItem("Tools/Demo Flow/Test UI")]
    static void RunUi(){SessionState.SetBool(Key+"UI",true);Run();}
    [MenuItem("Tools/Demo Flow/Test Interaction Polish")]
    static void RunPolish(){SessionState.SetBool(Key+"Polish",true);Run();}
    [MenuItem("Tools/Demo Flow/Test Demo Flow")]
    static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
        EditorSceneManager.SaveOpenScenes();
        runtimeErrors.Clear();Application.logMessageReceived-=CaptureError;Application.logMessageReceived+=CaptureError;
        SessionState.SetBool(Key,true);File.WriteAllText(Output,"Demo flow runtime regression\n");
        EditorSceneManager.OpenScene("Assets/Scenes/Tavern/Tavern_Main.unity");
        DemoPolishPlayCheck.CacheLeverGrip();
        EditorApplication.isPlaying=true;
    }
    static void Mode(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){runtimeErrors.Clear();Application.logMessageReceived-=CaptureError;Application.logMessageReceived+=CaptureError;started=EditorApplication.timeSinceStartup;EditorApplication.update+=WaitForLoad;}
        if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.update-=WaitForLoad;}
    }
    static void WaitForLoad()
    {
        if(EditorApplication.timeSinceStartup-started<4)return;
        player=UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();
        if(player==null){if(EditorApplication.timeSinceStartup-started>30)Finish("FAIL player did not load");return;}
        EditorApplication.update-=WaitForLoad;
        player.StartCoroutine(Drive(Check()));
    }
    static IEnumerator Drive(IEnumerator check)
    {
        var stack=new Stack<IEnumerator>();stack.Push(check);
        while(stack.Count>0)
        {
            object next=null;bool more=false;Exception error=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}
            catch(Exception ex){error=ex;}
            if(error!=null){Finish("FAIL "+error);yield break;}
            if(!more){stack.Pop();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        Finish("PASS all demo flow checks");
    }
    static void Finish(string result){Application.logMessageReceived-=CaptureError;if(runtimeErrors.Count>0)result="FAIL runtime errors: "+string.Join("; ",runtimeErrors.Distinct());Log(result);EditorApplication.update-=WaitForLoad;EditorApplication.delayCall+=()=>EditorApplication.isPlaying=false;}
    static IEnumerator Check()
    {
        var narrative=UnityEngine.Object.FindAnyObjectByType<Day1NarrativeController>();
        Assert(narrative.State==Day1FlowState.Awakening,"Demo startup state: "+narrative.State);
        Assert(!(bool)Get(narrative,"showingCinematic"),"Opening overlay visible");
        Log("PASS opening text skipped; prone/awakening gate retained");
        narrative.StopAllCoroutines();narrative.enabled=false;
        player.GetComponentInChildren<CharacterModelMotion>().EndFullBodyAction();
        var loader=UnityEngine.Object.FindAnyObjectByType<InitialAdditiveSceneLoader>();
        typeof(InitialAdditiveSceneLoader).GetMethod("SetHostContentVisible",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(loader,new object[]{true});
        var basement=SceneManager.GetSceneByName("SealRoom_B1");if(basement.isLoaded)yield return SceneManager.UnloadSceneAsync(basement);
        var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.position=new Vector3(30,0,14);body.enabled=true;
        player.MovementInputEnabled=true;
        hands=player.GetComponent<PlayerHands>();hands.Clear();
        menu=UnityEngine.Object.FindAnyObjectByType<TavernMenuSystem>();
        dispenser=UnityEngine.Object.FindAnyObjectByType<CupDispenserPoint>();
        activation=dispenser.GetComponent<CupDispenserActivation>();
        if(activation==null)activation=(CupDispenserActivation)Get(dispenser,"activation");
        orbit=UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>();orbit.FollowTarget=player.transform;
        if(SessionState.GetBool(Key+"UI",false))
        {
            SessionState.SetBool(Key+"UI",false);
            yield return DemoPolishPlayCheck.CheckUi(player,orbit,menu,narrative);
            yield break;
        }
        if(SessionState.GetBool(Key+"Polish",false))
        {
            SessionState.SetBool(Key+"Polish",false);
            yield return DemoPolishPlayCheck.Run(player);
            yield break;
        }
        yield return CheckCups();
        yield return CheckQueue();
        yield return CheckLever();
        yield return DemoPolishPlayCheck.Run(player);
        for(int x=-1;x<=4;x++)for(int z=2;z<=6;z++)
        {
            if(x==4&&z==2)continue;
            var position=new Vector3(x+.5f,1,z+.5f);
            Assert(Physics.Raycast(position,Vector3.down,out var hit,1.2f,~0,QueryTriggerInteraction.Ignore),"Entrance floor support missing "+position);
            Assert(hit.point.y>=-.05f,"Entrance floor support too low");
        }
        Log("PASS all 29 filled floor positions have physical support");
    }
    static CustomerServicePoint Order(string name,HeldItem item,int count=1)
    {
        var customer=new GameObject(name).AddComponent<CustomerServicePoint>();
        customer.GetComponent<NpcNavigator>().enabled=false;
        customer.GetComponent<NavMeshAgent>().enabled=false;
        customer.GetComponent<CharacterController>().enabled=false;
        var order=new CustomerOrder(new[]{new OrderRequest{item=item,quantity=count}},menu.FindDish);
        Assert(menu.RegisterOrder(customer,order),"Register test order");return customer;
    }
    static IEnumerator CheckCups()
    {
        if(menu.FindDish(HeldItem.MainDish)==null)
            ((List<DishDefinition>)Get(menu,"dishes")).Add(new DishDefinition{item=HeldItem.MainDish,label="Test food"});
        var food=Order("FoodOnly",HeldItem.MainDish);
        yield return new WaitForSeconds(.6f);
        Assert(!activation.IsActivated&&!dispenser.CupReady,"Food-only order activated cup dispenser");
        var one=Order("Drink1",HeldItem.TestDrink);var two=Order("Drink2",HeldItem.TestDrink);
        yield return new WaitForSeconds(1.3f);
        Assert(dispenser.CupReady&&activation.IsActivated&&dispenser.MissingCups==2,"Missing two cups");
        Assert(dispenser.Interact(hands)&&activation.IsActivated&&!dispenser.CupReady,"First cup should keep halo active");
        yield return new WaitForSeconds(.65f);
        Assert(dispenser.CupReady&&activation.IsActivated&&dispenser.MissingCups==1,"Replacement cup did not appear");
        Assert(hands.TryFillCup()&&menu.TryServe(one,HeldItem.TestDrink),"Serve first drink");hands.Clear();
        yield return null;yield return null;
        Assert(dispenser.CupReady&&activation.IsActivated,"Delivery incorrectly reset activation");
        Assert(dispenser.Interact(hands)&&!activation.IsActivated,"Last required cup should power down");
        Assert(hands.TryFillCup()&&menu.TryServe(two,HeldItem.TestDrink),"Serve second drink");hands.Clear();
        yield return new WaitForSeconds(.65f);
        Assert(!activation.IsActivated&&!dispenser.CupReady,"Delivered cups were counted as missing");
        var orders=(Dictionary<CustomerServicePoint,CustomerOrder>)Get(menu,"orders");
        orders[one].Eat(1000);orders[two].Eat(1000);Assert(menu.CompleteSale(one)&&menu.CompleteSale(two),"Settlement");
        yield return null;yield return null;
        Assert(!activation.IsActivated,"Settled cups incorrectly regenerated");
        var later=Order("LaterDrink",HeldItem.TestDrink);
        yield return new WaitForSeconds(.65f);Assert(dispenser.CupReady&&activation.IsActivated,"Later deficit failed to reactivate");
        menu.CancelOrder(later);menu.CancelOrder(food);
        yield return null;yield return null;Assert(!activation.IsActivated,"Cancelled demand did not stop dispenser");
        foreach(var c in new[]{food,one,two,later})UnityEngine.Object.Destroy(c.gameObject);
        Log("PASS food-only, persistent activation, replacement cup, last cup shutdown, serving, settlement, later demand and cancellation");
    }
    static IEnumerator CheckQueue()
    {
        var queue=UnityEngine.Object.FindAnyObjectByType<ServiceOrderQueue>();
        var head=new GameObject("QueueHead").AddComponent<CustomerServicePoint>();queue.Enqueue(head);
        head.GetComponent<NpcNavigator>().enabled=false;head.GetComponent<NavMeshAgent>().enabled=false;head.GetComponent<CharacterController>().enabled=false;
        var entry=new GameObject("QueueTestEntry").transform;
        var points=(List<Transform>)Get(queue,"queuePoints");
        Assert(NavMesh.SamplePosition(points[1].position,out var sample,1.5f,NavMesh.AllAreas),"Queue point off NavMesh");
        entry.position=sample.position;
        var guestObject=new GameObject("QueueFacing");guestObject.transform.position=entry.position;
        var customer=guestObject.AddComponent<CustomerServicePoint>();
        customer.GetComponent<CharacterController>().enabled=false;
        customer.Initialize("QueueFacing",new[]{new OrderRequest()},entry,UnityEngine.Object.FindAnyObjectByType<SeatRegistry>(),Color.white);
        Physics.SyncTransforms();
        customer.GetComponent<NavMeshAgent>().Warp(entry.position);
        customer.GetComponent<CharacterController>().enabled=true;
        customer.transform.rotation=Quaternion.LookRotation(-queue.GetFacing(customer));
        yield return new WaitForSeconds(1.5f);
        Assert(customer.State==CustomerOrderState.QueueingForOrder,"Queue guest did not stop behind head");
        Assert(Vector3.Dot(customer.transform.forward,queue.GetFacing(customer))>.98f,$"Initial queue arrival facing wrong pos={customer.transform.position} goal={queue.GetPosition(customer)} facing={customer.transform.forward} wanted={queue.GetFacing(customer)} nav={customer.GetComponent<NpcNavigator>().RemainingDistance}");
        queue.Remove(head);UnityEngine.Object.Destroy(head.gameObject);
        float deadline=Time.time+12;
        while(customer.State!=CustomerOrderState.Ordering&&Time.time<deadline)yield return null;
        yield return new WaitForSeconds(.4f);
        Assert(customer.State==CustomerOrderState.Ordering,"Queue did not advance to menu");
        Assert(Vector3.Dot(customer.transform.forward,queue.GetFacing(customer))>.98f,"Advanced queue facing wrong");
        UnityEngine.Object.Destroy(customer.gameObject);UnityEngine.Object.Destroy(entry.gameObject);
        Log("PASS initial queue arrival and advancing queue face toward queue/menu");
        yield return null;
    }
    static IEnumerator CheckLever()
    {
        var lever=UnityEngine.Object.FindAnyObjectByType<FloorLeverPoint>();
        var door=(DoorStateController)Get(lever,"entranceDoor");var sign=(TwoStateProp)Get(lever,"statusSign");
        var narrative=UnityEngine.Object.FindObjectsByType<Day1NarrativeController>(FindObjectsInactive.Include).Single();
        narrative.enabled=true;
        var story=(Ink.Runtime.Story)Get(narrative,"story");
        story.ChoosePathString("day01_prepare");
        typeof(Day1NarrativeController).GetMethod("ShowNextContent",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(narrative,null);
        for(int i=0;i<100&&narrative.State==Day1FlowState.Dialogue;i++)narrative.AdvanceForValidation();
        var eve=(Day1EveActor)Get(narrative,"eve");eve.gameObject.SetActive(true);
        float guideDeadline=Time.time+20;
        while(!narrative.CanOpenTavern&&Time.time<guideDeadline)yield return null;
        Assert(narrative.CanOpenTavern,"Eve guidance/Ink opening gate not ready");
        float yaw=orbit.CurrentCardinalYaw;float size=Camera.main.orthographicSize;
        for(int cycle=0;cycle<2;cycle++)
        {
            bool opening=cycle==0;
            if(!opening)
            {
                var day = UnityEngine.Object.FindAnyObjectByType<BusinessDayController>();
                day.DismissCustomers();
                float departureDeadline = Time.time + 40;
                while(day.ActiveCustomers > 0 && Time.time < departureDeadline) yield return null;
                if(day.ActiveCustomers>0)foreach(var c in UnityEngine.Object.FindObjectsByType<CustomerServicePoint>())
                {
                    var a=c.GetComponent<NavMeshAgent>();var motion=c.GetComponentInChildren<CharacterModelMotion>();
                    Log($"STUCK {c.name} state={c.State} pos={c.transform.position} remaining={c.GetComponent<NpcNavigator>().RemainingDistance} agent={a.enabled}/{a.isOnNavMesh} vel={a.velocity} destination={a.destination} standing={motion.IsStandingUp} collider={c.GetComponent<CharacterController>().bounds}");
                }
                Assert(day.ActiveCustomers == 0, "Guests did not leave before normal closing check");
                story.ChoosePathString("day01_close");
                typeof(Day1NarrativeController).GetMethod("ShowNextContent",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(narrative,null);
                for(int i=0;i<100&&narrative.State==Day1FlowState.Dialogue;i++)narrative.AdvanceForValidation();
                Assert(narrative.CanCloseTavern,"Closing Ink gate not ready");
            }
            Assert(lever.Interact(hands),"Lever interaction rejected");
            if(!opening)Assert(UnityEngine.Object.FindAnyObjectByType<BusinessDayController>().AdmissionsPaused,"Admissions continued during closing shot");
            Assert(lever.IsSwitching&&!player.MovementInputEnabled&&!lever.Interact(hands),"Switch not locked");
            yield return new WaitForSeconds(.65f);
            Assert(door.IsOpen!=opening&&sign.IsOpen!=opening,"Door/sign changed before camera arrived");
            float deadline=Time.time+15;bool captured=false;
            while(lever.IsSwitching&&Time.time<deadline)
            {
                if(sign.IsTransitioning&&!captured)
                {
                    Assert(door.IsOpen==opening&&!door.IsTransitioning,"Sign began before door completed");
                    yield return new WaitForSeconds(.55f);
                    ScreenCapture.CaptureScreenshot(opening?"/tmp/entrance-opening.png":"/tmp/entrance-closing.png");captured=true;
                }
                yield return null;
            }
            Assert(!lever.IsSwitching&&captured,"Door/sign sequence did not complete");
            Assert(player.MovementInputEnabled&&!orbit.EntranceFraming,"Control not restored");
            Assert(door.IsOpen==opening&&sign.IsOpen==opening&&!sign.IsTransitioning,"Final states inconsistent");
            Assert(Mathf.Abs(Mathf.DeltaAngle(orbit.transform.eulerAngles.y,yaw))<.01f&&Mathf.Abs(Camera.main.orthographicSize-size)<.01f,"Camera did not restore exploration framing");
            Assert(Vector3.Distance(orbit.transform.position,new Vector3(player.transform.position.x,0,player.transform.position.z))<.05f,"Camera did not return to player");
            Assert(narrative.State==(opening?Day1FlowState.ServingBran:Day1FlowState.Completed),"Business narrative did not advance after camera returned");
            Log("PASS "+(opening?"opening":"closing")+" camera -> door -> full sign animation -> player, locks and framing restored");
            if(opening)yield return CheckBasementTravel();
        }
    }
    static IEnumerator CheckBasementTravel()
    {
        var day=UnityEngine.Object.FindAnyObjectByType<BusinessDayController>();
        float deadline=Time.time+12;
        while(UnityEngine.Object.FindObjectsByType<CustomerServicePoint>().Length<2&&Time.time<deadline)yield return null;
        var customers=UnityEngine.Object.FindObjectsByType<CustomerServicePoint>();
        Assert(customers.Length>=2,"Travel check needs multiple customers");
        var seats=customers.Select(c=>c.AssignedSeat).ToArray();
        var orders=customers.Select(c=>c.Order).ToArray();
        var portal=UnityEngine.Object.FindObjectsByType<AdditiveScenePortal>().First(p=>(string)Get(p,"sceneToLoad")=="SealRoom_B1");
        typeof(AdditiveScenePortal).GetMethod("OnTriggerEnter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(portal,new object[]{player.GetComponent<CharacterController>()});
        deadline=Time.time+20;
        while((!SceneManager.GetSceneByName("SealRoom_B1").isLoaded||customers.Any(c=>c.gameObject.activeInHierarchy))&&Time.time<deadline)yield return null;
        yield return new WaitForSeconds(1);
        Assert(SceneManager.GetSceneByName("SealRoom_B1").isLoaded&&customers.All(c=>!c.gameObject.activeInHierarchy),"Tavern customers leaked into B1");
        Assert(customers.All(c=>c.gameObject.scene.name=="Tavern_Main"&&c.transform.parent!=null),"Customers have wrong scene ownership");
        Assert(UnityEngine.Object.FindObjectsByType<CustomerServicePoint>().Length==0,"Active customer remains in B1");
        var positions=customers.Select(c=>c.transform.position).ToArray();
        var states=customers.Select(c=>c.State).ToArray();
        float elapsed=(float)Get(day,"elapsedTime");int waiting=day.WaitingCustomers;
        ScreenCapture.CaptureScreenshot("/tmp/b1-customer-isolation.png");
        yield return new WaitForSeconds(7);
        Assert(day.WaitingCustomers==waiting&&(float)Get(day,"elapsedTime")==elapsed,"Arrivals continued while in B1");
        for(int i=0;i<customers.Length;i++)
            Assert(customers[i].transform.position==positions[i]&&customers[i].State==states[i],"Hidden customer continued simulating");
        var back=UnityEngine.Object.FindObjectsByType<AdditiveScenePortal>().First(p=>(string)Get(p,"sceneToLoad")=="Tavern_Main");
        typeof(AdditiveScenePortal).GetMethod("OnTriggerEnter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(back,new object[]{player.GetComponent<CharacterController>()});
        deadline=Time.time+20;
        while((SceneManager.GetSceneByName("SealRoom_B1").isLoaded||customers.Any(c=>!c.gameObject.activeInHierarchy))&&Time.time<deadline)yield return null;
        yield return new WaitForSeconds(.7f);
        Assert(!SceneManager.GetSceneByName("SealRoom_B1").isLoaded&&customers.All(c=>c.gameObject.activeInHierarchy),"Customers did not resume after return");
        for(int i=0;i<customers.Length;i++)Assert(customers[i].Order==orders[i]&&customers[i].AssignedSeat==seats[i],"Travel lost order or seat reservation");
        Assert(UnityEngine.Object.FindObjectsByType<PrototypePlayerMover>().Length==1,"Duplicate player after travel");
        Log($"PASS real F1/B1 portal round trip with {customers.Length} customers: hidden/frozen in B1, no arrivals for 7 seconds, same orders/seats restored in F1");
    }

}

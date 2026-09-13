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
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

static class DemoPolishPlayCheck
{
    static object Get(object obj,string field)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    static void Log(string value)=>File.AppendAllText("/tmp/demo-flow-playcheck.txt",value+"\n");
    public static void CacheLeverGrip()
    {
        var lever=UnityEngine.Object.FindAnyObjectByType<FloorLeverPoint>();
        var pivot=(Transform)Get(lever,"handlePivot");
        var points=pivot.GetComponentsInChildren<MeshFilter>().SelectMany(m=>m.sharedMesh.vertices.Select(v=>m.transform.TransformPoint(v))).ToArray();
        float top=points.Max(p=>p.y),bottom=points.Min(p=>p.y);
        var gripPoints=points.Where(p=>p.y>top-(top-bottom)*.12f).ToArray();
        var grip=gripPoints.Aggregate(Vector3.zero,(a,b)=>a+b)/gripPoints.Length;
        UnityEditor.SessionState.SetVector3("DemoPolish.LeverGrip",pivot.InverseTransformPoint(grip));
    }
    public static IEnumerator Run(PrototypePlayerMover player)
    {
        var orbit=UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>();
        var hands=player.GetComponent<PlayerHands>();
        var body=player.GetComponent<CharacterController>();
        var lever=UnityEngine.Object.FindAnyObjectByType<FloorLeverPoint>();
        var day=UnityEngine.Object.FindAnyObjectByType<BusinessDayController>();
        var menu=UnityEngine.Object.FindAnyObjectByType<TavernMenuSystem>();
        var narrative=UnityEngine.Object.FindAnyObjectByType<Day1NarrativeController>();
        var chest=UnityEngine.Object.FindObjectsByType<ChestInteractionPoint>().First();
        chest.enabled=false;var prop=chest.GetComponent<TwoStateProp>();
        var lid=chest.transform.Find("LidPivot");
        for(int pass=0;pass<2;pass++)
        {
            bool open=pass==0;prop.SetOpen(open);float previous=lid.localEulerAngles.x;float stop=Time.time+1.2f;
            while(Time.time<stop)
            {
                yield return null;Assert(!string.IsNullOrEmpty(chest.GetPrompt(hands)),"Chest prompt disappears during animation");float current=lid.localEulerAngles.x;float delta=Mathf.DeltaAngle(previous,current);
                Assert(chest.GetPrompt(hands)==(prop.IsTransitioning?(open?"F：打开箱子":"F：关闭箱子"):(open?"F：关闭箱子":"F：打开箱子")),"Chest prompt changed before animation completed");
                Assert(open?delta<.3f:delta>-.3f,"Chest animation reverses/repeats mid-transition");previous=current;
            }
            Assert(!prop.IsTransitioning&&Mathf.Abs(Mathf.DeltaAngle(previous,open?-25:0))<.2f,"Chest final pose wrong");
        }
        chest.enabled=true;Log("PASS chest opening/closing monotonic, single motion, correct stable poses");
        player.MovementInputEnabled=false;player.enabled=false;
        var obstacle=player.GetComponent<NavMeshObstacle>();obstacle.enabled=false;
        foreach(var door in UnityEngine.Object.FindObjectsByType<DoorStateController>().Where(d=>d.name.StartsWith("Door_Small")))
        for(int direction=-1;direction<=1;direction+=2)
        {
            var forward=door.transform.forward*direction;
            body.enabled=false;player.transform.position=door.transform.position-forward*1.25f+Vector3.up*.02f;Physics.SyncTransforms();body.enabled=true;
            door.Open();
            for(int warmup=0;warmup<20;warmup++){body.Move(Vector3.down*2*Time.deltaTime);yield return null;}
            float min=float.PositiveInfinity,max=float.NegativeInfinity,stop=Time.time+1.2f;
            while(Time.time<stop)
            {
                body.Move((forward*2.5f+Vector3.down*2)*Time.deltaTime);yield return null;
                // Exclude teleport contact settling outside the door; measure the entire frame passage itself.
                if(Mathf.Abs(Vector3.Dot(player.transform.position-door.transform.position,forward))<.75f)
                { min=Mathf.Min(min,player.transform.position.y);max=Mathf.Max(max,player.transform.position.y); }
            }
            Assert(Vector3.Dot(player.transform.position-door.transform.position,forward)>.7f,"Door blocks passage "+door.name);
            if(max-min>=.04f)Log($"DOOR TRACE {door.name} direction={direction} variation={min}..{max}");
            Assert(!float.IsInfinity(min)&&max-min<.02f,$"Door causes vertical bump {door.name}: {min}..{max}");
        }
        obstacle.enabled=true;player.enabled=true;player.MovementInputEnabled=true;
        Log("PASS all three small doors crossed both ways, vertical variation below 2 cm");
        body.enabled=false;player.transform.position=new Vector3(30,0,14);Physics.SyncTransforms();body.enabled=true;
        var pivot=(Transform)Get(lever,"handlePivot");
        var localGrip=UnityEditor.SessionState.GetVector3("DemoPolish.LeverGrip",Vector3.zero);
        Vector3 restDirection=pivot.TransformPoint(localGrip)-pivot.position;
        Assert(lever.Interact(hands),"Reopen rejected");
        yield return new WaitForSeconds(.15f);
        var keyboard=InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));
        yield return null; yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return null;
        InputSystem.RemoveDevice(keyboard);
        yield return new WaitForSeconds(.5f);
        Assert(orbit.EntranceFraming,"Interrupted entrance camera returned faster than opening pan");
        yield return new WaitForSeconds(.9f);
        Assert(!orbit.EntranceFraming&&player.MovementInputEnabled,"Interrupted entrance camera did not return control");
        while(lever.IsSwitching)yield return null;
        var movedDirection=pivot.TransformPoint(localGrip)-pivot.position;
        Assert(Mathf.Abs(restDirection.y-movedDirection.y)<.02f&&Vector3.Distance(new Vector3(-restDirection.x,restDirection.y,-restDirection.z),movedDirection)<.03f,"Lever endpoints not symmetric about vertical");
        Log("PASS lever grip reflected with equal ground angle; camera can return early while door/sign complete");
        var entry=new CustomerScheduleEntry();entry.Configure("EarlyClosing",0,HeldItem.TestDrink,Color.white);entry.SetArrivalKind(CustomerArrivalKind.Party);
        Assert((bool)typeof(BusinessDayController).GetMethod("TrySpawn",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(day,new object[]{entry}),"Could not spawn party for closure check");
        var customers=UnityEngine.Object.FindObjectsByType<CustomerServicePoint>();
        Assert(customers.Length>=2,"Need multiple guests");
        var seated=customers[0];var cc=seated.GetComponent<CharacterController>();cc.enabled=false;
        seated.transform.position=seated.AssignedSeat.Position;Physics.SyncTransforms();seated.GetComponent<NavMeshAgent>().Warp(seated.transform.position);cc.enabled=true;
        typeof(CustomerServicePoint).GetMethod("ChangeState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(seated,new object[]{CustomerOrderState.WaitingForFood,true});
        menu.RegisterOrder(seated,seated.Order);
        yield return new WaitForSeconds(2);
        int balance=menu.Balance;int waiting=day.WaitingCustomers;
        var departureFrames=new List<int>();foreach(var c in customers)c.StateChanged+=state=>{if(state==CustomerOrderState.Leaving)departureFrames.Add(Time.frameCount);};
        Quaternion beforeTurn=player.transform.rotation;
        Assert(lever.Interact(hands)&&!orbit.EntranceFraming,"Temporary close should keep player view");
        Assert(Quaternion.Angle(beforeTurn,player.transform.rotation)<.1f,"Closure turn snapped immediately");
        Assert(player.GetComponent<WorldSpeechBubble>().CurrentText.EndsWith("！"),"Missing owner announcement");
        Assert(player.MovementInputEnabled&&player.GetComponent<PlayerInteractionController>().enabled,"Temporary closure locks player input");
        var tables=UnityEngine.Object.FindAnyObjectByType<SeatRegistry>().Tables.Where(t=>t!=null&&t.isActiveAndEnabled).ToArray();
        var center=tables.Aggregate(Vector3.zero,(sum,t)=>sum+t.transform.position)/tables.Length;
        yield return new WaitForSeconds(.5f);
        Assert(Vector3.Dot(player.transform.forward,Vector3.ProjectOnPlane(center-player.transform.position,Vector3.up).normalized)>.98f,"Closure announcement does not face the hall");
        Vector3 beforeMove=player.transform.position;
        var moveKeys=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(moveKeys,new KeyboardState(Key.W));
        yield return new WaitForSeconds(.2f);
        InputSystem.QueueStateEvent(moveKeys,new KeyboardState());yield return null;InputSystem.RemoveDevice(moveKeys);
        Assert(Vector3.Distance(beforeMove,player.transform.position)>.1f,"Player cannot move during closure announcement");
        yield return new WaitForSeconds(1.2f);
        Assert(departureFrames.Count==customers.Length&&departureFrames.Distinct().Count()==1,"Guests did not start departing together");
        foreach(var c in customers.Where(c=>c!=null))
            Assert(c.GetComponent<WorldSpeechBubble>().CurrentText.All(ch=>"@#$%&*!?".Contains(ch))&&c.GetComponent<WorldSpeechBubble>().CurrentText.Length>=5,"Missing randomized complaint");
        yield return Capture("/tmp/temporary-closing.png");
        float exitDeadline=Time.time+40;
        while(lever.IsSwitching&&Time.time<exitDeadline){Assert(!orbit.EntranceFraming,"Temporary close detached camera");yield return null;}
        Assert(!lever.IsSwitching&&day.ActiveCustomers==0,"Guests stuck during temporary closure");
        Assert(menu.Balance==balance&&menu.PendingOrderCount==0&&day.WaitingCustomers==waiting,"Temporary closure charged/cancelled future arrivals");
        Log("PASS temporary closure: owner announcement, simultaneous departures, randomized complaints, no camera cut, no unpaid revenue, faces hall and movement remains available");
        yield return CheckUi(player,orbit,menu,narrative);
    }
    static void CheckDialogueCamera(PrototypeCameraOrbit orbit)
    {
        var choose=typeof(PrototypeCameraOrbit).GetMethod("ChooseDialogueYaw",BindingFlags.Instance|BindingFlags.NonPublic);
        var left=GameObject.CreatePrimitive(PrimitiveType.Capsule);var right=GameObject.CreatePrimitive(PrimitiveType.Capsule);
        left.transform.position=new Vector3(500,0,500);right.transform.position=new Vector3(502,0,500);
        var near=GameObject.CreatePrimitive(PrimitiveType.Cube);near.transform.position=new Vector3(501,5,496);near.transform.localScale=new Vector3(10,20,.5f);
        var far=GameObject.CreatePrimitive(PrimitiveType.Cube);far.transform.position=new Vector3(501,5,504);far.transform.localScale=near.transform.localScale;
        Quaternion original=orbit.transform.rotation;orbit.transform.rotation=Quaternion.Euler(0,40,0);
        float Pick()=>(float)choose.Invoke(orbit,new object[]{left.transform,right.transform});
        near.SetActive(false);far.SetActive(false);Physics.SyncTransforms();
        Assert(Mathf.Abs(Mathf.DeltaAngle(Pick(),0))<.01f,"Unblocked camera did not choose nearer perpendicular");
        near.SetActive(true);Physics.SyncTransforms();Assert(Mathf.Abs(Mathf.DeltaAngle(Pick(),180))<.01f,"Blocked nearer view did not choose opposite");
        far.SetActive(true);Physics.SyncTransforms();Assert(Mathf.Abs(Mathf.DeltaAngle(Pick(),0))<.01f,"Both blocked should choose shorter rotation");
        orbit.transform.rotation=original;
        foreach(var go in new[]{left,right,near,far}){go.SetActive(false);UnityEngine.Object.Destroy(go);}
        Log("PASS two perpendicular dialogue angles: clear/one blocked/both blocked; shortest-turn fallback");
    }
    public static void ClickUi(string name)
    {
        var button=DungeonTavern.UI.TavernUI.Instance.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name);
        var events=UnityEngine.EventSystems.EventSystem.current;
        var pointer=new UnityEngine.EventSystems.PointerEventData(events){position=RectTransformUtility.WorldToScreenPoint(null,button.transform.TransformPoint(((RectTransform)button.transform).rect.center))};
        var hits=new List<UnityEngine.EventSystems.RaycastResult>();events.RaycastAll(pointer,hits);
        Assert(hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==button,"UI obscured/not clickable: "+name);
        UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator Capture(string path)
    {
        ScreenCapture.CaptureScreenshot(path);
        yield return new WaitForEndOfFrame();
        yield return null;
    }
    public static IEnumerator CheckUi(PrototypePlayerMover player,PrototypeCameraOrbit orbit,TavernMenuSystem menu,Day1NarrativeController narrative)
    {
        var ui=DungeonTavern.UI.TavernUI.Instance;
        Assert(ui!=null,"Missing UI root");
        var canvases=ui.GetComponentsInChildren<Canvas>(true);
        Assert(canvases.Length==8&&canvases.Select(c=>c.sortingOrder).Distinct().Count()==8,"UI layers missing or ambiguous");
        Assert(canvases.All(c=>c.gameObject.layer==5&&c.renderMode==RenderMode.ScreenSpaceOverlay),"UI not isolated on overlay layer");
        Assert(DungeonTavern.UI.TavernUiTheme.Font!=null&&DungeonTavern.UI.TavernUiTheme.Font.HasCharacter('酒'),"Packaged Chinese font unavailable");
        ScreenCapture.CaptureScreenshot("/tmp/tavern-ui-hud.png");yield return new WaitForEndOfFrame();yield return null;
        var pause=UnityEngine.Object.FindAnyObjectByType<GamePauseMenu>();
        yield return new WaitForSeconds(.6f);
        ClickUi("PauseButton");Assert(GamePauseMenu.IsPaused,"Pause button did not respond");
        float time=Time.time;var position=player.transform.position;var rotation=orbit.transform.rotation;
        yield return new WaitForSecondsRealtime(.2f);yield return Capture("/tmp/pause-menu.png");
        yield return new WaitForSecondsRealtime(.4f);
        Assert(Time.time==time&&player.transform.position==position&&orbit.transform.rotation==rotation&&!player.GetComponent<PlayerInteractionController>().TryInteract(),"Pause did not freeze world/input");
        ClickUi("Option4");yield return null;Assert(pause.HistoryVisible,"History menu did not open");yield return Capture("/tmp/ui-history.png");ClickUi("HistoryBack");yield return null;
        ClickUi("Option2");Assert(pause.ControlsVisible,"Controls button did not respond");yield return new WaitForSecondsRealtime(.2f);yield return Capture("/tmp/pause-controls.png");
        ClickUi("Back");yield return null;ClickUi("Option3");Assert(pause.ConfirmingQuit,"Quit confirmation did not open");yield return new WaitForSecondsRealtime(.2f);yield return Capture("/tmp/pause-quit.png");
        yield return new WaitForSecondsRealtime(.2f);ClickUi("Cancel");yield return null;ClickUi("Resume");Assert(Time.timeScale>0,"Pause did not restore time");
        Set(menu,"isOpen",true);Set(menu,"selectedTab",0);yield return new WaitForSeconds(.2f);yield return Capture("/tmp/menu-summary.png");
        ClickUi("OrdersTab");Assert(menu.SelectedTab==1,"Orders tab did not respond");yield return new WaitForSeconds(.2f);yield return Capture("/tmp/menu-orders.png");
        yield return new WaitForSeconds(.2f);
        var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));
        yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;InputSystem.RemoveDevice(keyboard);
        Assert(!menu.IsOpen&&!GamePauseMenu.IsPaused,"Esc on ledger should close it without opening pause");
        // Verify live held-item / interaction views and both bubble forms.
        var body=player.GetComponent<CharacterController>();body.enabled=false;
        var lever=UnityEngine.Object.FindAnyObjectByType<FloorLeverPoint>();player.transform.position=lever.transform.position-Vector3.forward*.8f;
        Physics.SyncTransforms();body.enabled=true;
        var hands=player.GetComponent<PlayerHands>();hands.Clear();hands.TryHold(HeldItem.EmptyCup);
        var bubble=player.GetComponent<WorldSpeechBubble>();if(bubble==null)bubble=player.gameObject.AddComponent<WorldSpeechBubble>();
        bubble.Show("酒馆要临时关闭了，请各位先离开！");
        yield return new WaitForSeconds(.6f);yield return Capture("/tmp/ui-hud-interaction.png");
        Assert(ui.transform.Find("20_HUD/SafeArea/HeldItem").gameObject.activeInHierarchy,"Held item HUD hidden");
        Assert(ui.transform.Find("20_HUD/SafeArea/InteractionHint").gameObject.activeInHierarchy,"Interaction HUD hidden");
        var order=new CustomerOrder(new[]{new OrderRequest{item=HeldItem.TestDrink,quantity=2}},menu.FindDish);
        order.TryDeliver(HeldItem.TestDrink);bubble.ShowOrder(order,true);
        yield return new WaitForSeconds(.3f);yield return Capture("/tmp/ui-order-bubble.png");bubble.Hide();hands.Clear();
        narrative.enabled=true;menu.Toggle();
        var eve=(Day1EveActor)Get(narrative,"eve");
        eve.gameObject.SetActive(true);bool eveEnabled=eve.enabled;eve.enabled=false;eve.GetComponent<NpcNavigator>().Stop(true);
        yield return null;yield return null;
        var wallMaterials=UnityEngine.Object.FindObjectsByType<Renderer>().Where(r=>r.name.Contains("Wall")).ToDictionary(r=>r,r=>r.sharedMaterials);
        typeof(Day1NarrativeController).GetMethod("BeginCloseDialogue",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(narrative,new object[]{eve.transform});
        var finalView=Quaternion.Euler(0,(float)Get(orbit,"dialogueTargetYaw"),0);float initialTurn=Quaternion.Angle(orbit.transform.rotation,finalView);
        yield return new WaitForSeconds(.3f);
        if(initialTurn>10)Assert(Quaternion.Angle(orbit.transform.rotation,finalView)>initialTurn*.5f,"Dialogue camera rotates too quickly");
        CheckDialogueCamera(orbit);
        var story=(Ink.Runtime.Story)Get(narrative,"story");story.ChoosePathString("day01_eve_arrives");
        typeof(Day1NarrativeController).GetMethod("ShowNextContent",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(narrative,null);
        narrative.ContinueDialogue();narrative.ContinueDialogue();
        Assert(narrative.CurrentLine.Contains("这是你的钥匙")&&narrative.CurrentChoiceCount==0,"Single choice leaked into choice UI");
        narrative.ContinueDialogue();
        Assert(narrative.CurrentLine=="你：接过钥匙。"&&narrative.ChoiceTexts.Count==0,"Single action was not presented in dialogue");
        int actionCount=narrative.FullHistory.Count(t=>t=="你：接过钥匙。");
        narrative.ContinueDialogue();
        Assert(narrative.CurrentLine.Contains("你以前只交代过一句")&&narrative.CurrentChoiceCount>1,"Choices did not accompany preceding speech");
        Assert(narrative.FullHistory.Count(t=>t=="你：接过钥匙。")==actionCount,"Single action recorded twice");
        story.ChoosePathString("day01_eve_conversation");
        typeof(Day1NarrativeController).GetMethod("ShowNextContent",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(narrative,null);
        Assert(!menu.IsOpen,"Dialogue did not dismiss ledger");
        yield return new WaitForSeconds(1.5f);yield return Capture("/tmp/dialogue-speech.png");
        Assert(Vector3.Dot(player.transform.forward,Vector3.ProjectOnPlane(eve.transform.position-player.transform.position,Vector3.up).normalized)>.995f,"Player did not turn toward dialogue speaker");
        Vector3 pair=eve.transform.position-player.transform.position;pair.y=0;
        Vector3 cameraForward=Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up).normalized;
        Assert(Mathf.Abs(Vector3.Dot(pair.normalized,cameraForward))<.025f,"Live dialogue view not perpendicular to the stopped pair");
        foreach(var pairMaterials in wallMaterials)if(pairMaterials.Key)Assert(pairMaterials.Key.sharedMaterials.SequenceEqual(pairMaterials.Value),"Dialogue changed wall material: "+pairMaterials.Key.name);
        Log("PASS dialogue camera eases over 1.3 seconds, remains perpendicular, leaves all wall materials unchanged");
        if(narrative.CurrentChoiceCount==0) { ClickUi("Continue");yield return null; }
        for(int i=0;i<20&&narrative.CurrentChoiceCount==0;i++)narrative.ContinueDialogue();
        yield return new WaitForSeconds(.3f);yield return Capture("/tmp/dialogue-bottom.png");
        Assert(narrative.CurrentChoiceCount>0,"Expected conversation choices");
        int choiceCount=narrative.CurrentChoiceCount;string choiceLine=narrative.CurrentLine;
        var choiceKeyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(choiceKeyboard,new KeyboardState(Key.Digit1));
        yield return null;yield return null;InputSystem.QueueStateEvent(choiceKeyboard,new KeyboardState());yield return null;InputSystem.RemoveDevice(choiceKeyboard);
        Assert(narrative.CurrentChoiceCount==choiceCount&&narrative.CurrentLine==choiceLine,"Digit key still chooses dialogue");
        foreach(var b in ui.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.name.StartsWith("Choice")))
        {
            var label=b.GetComponentInChildren<UnityEngine.UI.Text>();
            Assert(label.alignment==TextAnchor.MiddleCenter&&!label.text.StartsWith("“")&&!char.IsDigit(label.text[0]),"Choice presentation not centered/stripped");
        }
        ClickUi("Choice1");yield return new WaitForSeconds(.2f);
        Assert(!string.IsNullOrEmpty(narrative.CurrentLine),"Dialogue choice did not produce text");
        Assert(ui.transform.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().All(s=>s.name!="DialogueChoices"),"Choices still use a scroll view");
        yield return Capture("/tmp/dialogue-speech.png");
        var speech=ui.GetComponentsInChildren<UnityEngine.UI.Text>().First(t=>t.name=="Speech");
        string raw=narrative.PresentedLine;int colon=raw.IndexOf('：');
        string spoken=colon>0&&colon<12?raw.Substring(colon+1).Trim():raw;
        if(colon>0&&colon<12&&spoken.StartsWith("“")&&spoken.EndsWith("”"))spoken=spoken.Substring(1,spoken.Length-2);
        Assert(speech.text==spoken,"Dialogue display quote stripping failed");
        Assert(narrative.FullHistory.Contains(raw),"Original dialogue missing from history");
        var speaker=ui.GetComponentsInChildren<UnityEngine.UI.Text>().First(t=>t.name=="Speaker");
        Assert(Mathf.Abs(((RectTransform)speaker.transform.parent).rect.width-Mathf.Max(64,speaker.preferredWidth+40))<1,"Speaker plaque not fitted to name");
        Log("PASS speech presentation strips outer quotes, history preserves original and name plaque fits text");
        string heldLine=narrative.CurrentLine;ClickUi("DialogueHistory");yield return null;
        Assert(pause.HistoryVisible&&GamePauseMenu.IsPaused,"Dialogue review did not pause");narrative.ContinueDialogue();Assert(narrative.CurrentLine==heldLine,"Review advanced dialogue");
        yield return Capture("/tmp/ui-dialogue-history.png");ClickUi("HistoryBack");yield return null;Assert(!GamePauseMenu.IsPaused,"Review did not return to dialogue");
        string priorLine=narrative.CurrentLine;if(narrative.CurrentChoiceCount==0)ClickUi("Continue");else ClickUi("Choice"+(narrative.CurrentChoiceCount-1));yield return null;
        Assert(narrative.CurrentLine!=priorLine||narrative.CurrentChoiceCount>0,"Continue button did not advance Ink");
        eve.enabled=eveEnabled;
        Log("PASS UI raycast clicks, tabs, Esc routing; pause freezes time/input and restores it; captured pause, controls, quit confirmation, both menu tabs and dialogue layout");
    }
}

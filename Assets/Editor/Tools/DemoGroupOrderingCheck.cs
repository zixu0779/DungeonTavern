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
using UnityEngine;
using UnityEngine.AI;
using DungeonTavern.UI;
static class DemoGroupOrderingCheck
{
    static object Get(object o,string key)=>o.GetType().GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Set(object o,string key,object value)=>o.GetType().GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    static object Call(object o,string key,params object[] args)=>o.GetType().GetMethod(key,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
    static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    static void Log(string message)=>File.AppendAllText("/tmp/demo-flow-playcheck.txt",message+"\n");
    public static IEnumerator Run(PrototypePlayerMover player,Day1NarrativeController narrative)
    {
        var menu=UnityEngine.Object.FindAnyObjectByType<TavernMenuSystem>();
        Assert(menu.Dishes.Count==8&&menu.Dishes.Count(d=>TavernMenuSystem.IsSharedDish(d.item))==2&&menu.Dishes.Count(d=>TavernMenuSystem.IsDrink(d.item))==3,"Demo menu categories");
        var dispenser=UnityEngine.Object.FindAnyObjectByType<CupDispenserPoint>();
        Log("Dispenser visual target "+dispenser.GuidancePosition+" interaction root "+dispenser.transform.position);
        yield return CheckApproach(player,narrative);
        // Shared portions have one identity, one consumption clock and one charged owner.
        var communal=new CustomerOrder(new[]{new OrderRequest{item=HeldItem.MainDish}},menu.FindDish);
        var a=new CustomerOrder(new[]{new OrderRequest{item=HeldItem.RootBread}},menu.FindDish){AllowDrinkSubstitute=true};
        var b=new CustomerOrder(new[]{new OrderRequest{item=HeldItem.GlowcapAle}},menu.FindDish){AllowDrinkSubstitute=true};
        a.AttachShared(communal,true);b.AttachShared(communal,false);
        Assert(ReferenceEquals(a.Portions[1],b.Portions[1]),"Shared dish duplicated");
        Assert(b.TryDeliver(HeldItem.TestDrink)&&b.Portions[0].Delivered&&!b.Portions[1].Delivered,"Personal delivery must precede shared");
        Assert(b.TryDeliver(HeldItem.TestDrink)&&a.Portions[1].Delivered,"Shared delivery through non-owner");
        b.Eat(100);Assert(!communal.AllConsumed,"Shared dish consumed twice by non-owner");a.Eat(100);
        Assert(communal.AllConsumed&&a.Total+b.Total==menu.FindDish(HeldItem.RootBread).price+menu.FindDish(HeldItem.GlowcapAle).price+menu.FindDish(HeldItem.MainDish).price,"Shared charge count");
        Log("PASS shared identity, any-member delivery, personal-first proxy, single consumption/charge");
        var day=UnityEngine.Object.FindAnyObjectByType<BusinessDayController>();day.enabled=false;Set(day,"demoService",true);
        var entry=new CustomerScheduleEntry();entry.Configure("GroupDemo",0,HeldItem.TestDrink,Color.white);entry.SetArrivalKind(CustomerArrivalKind.Party);Set(entry,"partySize",4);
        Assert((bool)Call(day,"TrySpawn",entry),"Four-person party spawn");
        var guests=UnityEngine.Object.FindObjectsByType<CustomerServicePoint>().Where(c=>c.CustomerName.StartsWith("GroupDemo")).ToArray();
        var queue=UnityEngine.Object.FindAnyObjectByType<ServiceOrderQueue>();
        var orbit=UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>();var focus=new GameObject("OrderingTestFocus");focus.transform.position=queue.MenuAnchor.position;orbit.FollowTarget=focus.transform;
        var playerBody=player.GetComponent<CharacterController>();playerBody.enabled=false;player.transform.position=new Vector3(30,0,14);playerBody.enabled=true;player.MovementInputEnabled=false;
        float deadline=Time.time+100;bool sawThinking=false,sawQuestion=false,sawVote=false,captured=false,rejected=false,accepted=false;
        while(guests.Any(c=>c.State is CustomerOrderState.Entering or CustomerOrderState.QueueingForOrder or CustomerOrderState.Ordering)&&Time.time<deadline)
        {
            var session=Get(queue,"session");
            if(session!=null && (int)Get(session,"votePhase")==4)
            {
                var votes=(IDictionary)Get(session,"votes");var keys=votes.Keys.Cast<object>().ToArray();
                int proposal=(int)Get(session,"proposalIndex");
                foreach(var key in keys)votes[key]=true;
                if(proposal==0){votes[keys[0]]=false;rejected=true;}
                else accepted=true;
            }
            if(session!=null && (int)Get(session,"proposalIndex")==1)
                Assert(((IList)Get(session,"shared")).Count==0,"A rejected proposal entered confirmed dishes");
            int ordering=guests.Count(c=>c.State==CustomerOrderState.Ordering);
            if(ordering>0){Assert(ordering==4,"Party began/leaves ordering separately");sawThinking=true;}
            foreach(var g in guests)
            {
                var bubble=g.GetComponent<WorldSpeechBubble>();var reaction=Get(bubble,"reaction") as string;
                if(reaction=="?")sawQuestion=true;if(reaction is "✓" or "×")sawVote=true;
            }
            if(sawQuestion&&!captured){yield return new WaitForSeconds(.2f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("/tmp/group-ordering-vote.png");captured=true;}
            yield return null;
        }
        Assert(guests.All(c=>c.State is CustomerOrderState.FindingSeat or CustomerOrderState.MovingToSeat or CustomerOrderState.WaitingForFood),"Group stalled: "+string.Join(";",guests.Select(c=>$"{c.State} {c.transform.position} target={queue.GetPosition(c)}")));
        Assert(sawThinking&&sawQuestion&&sawVote&&rejected&&accepted,"Missing group thought/proposal/rejection/acceptance phase");
        var portions=guests.SelectMany(c=>c.Order.Portions).Distinct().ToArray();
        var shared=portions.Where(p=>TavernMenuSystem.IsSharedDish(p.Item)).ToArray();
        Assert(shared.Length==1&&guests.All(c=>c.Order.Portions.Count(p=>TavernMenuSystem.IsDrink(p.Item))<=1&&c.Order.Portions.Count(p=>!TavernMenuSystem.IsSharedDish(p.Item)&&!TavernMenuSystem.IsDrink(p.Item))<=2),"Order caps");
        Assert(menu.PendingOrderCount==portions.Length&&menu.MissingServingCups==portions.Length,"Shared menu/cup demand double counted");
        foreach(var dish in shared)Assert(guests.All(c=>c.Order.Portions.Contains(dish)),"Shared dish not visible to all");
        Log("PASS real four-person queue -> distinct menu slots -> simultaneous asynchronous choices/votes -> depart together; dish caps and unique cup demand; forced rejection and unanimous acceptance verified");
        deadline=Time.time+70;
        while(guests.Any(c=>c.State!=CustomerOrderState.WaitingForFood)&&Time.time<deadline)yield return null;
        Assert(guests.All(c=>c.State==CustomerOrderState.WaitingForFood),"Group did not reach seats");
        focus.transform.position=guests[0].AssignedSeat.Table.SurfaceCenter;
        yield return new WaitForSeconds(.5f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("/tmp/group-seated-orders.png");
        yield return CheckDispenserGuide(player,narrative,dispenser);
        int balance=menu.Balance,total=guests.Sum(c=>c.Order.Total);var hands=player.GetComponent<PlayerHands>();hands.Clear();
        foreach(var guest in guests.Reverse())
            while(guest.Order.Needs(HeldItem.TestDrink)){hands.TryHold(HeldItem.TestDrink);Assert(guest.Interact(hands),"Proxy actual service rejected");}
        Assert(menu.PendingOrderCount==0&&menu.MissingServingCups==0,"Served proxy portions still need cups");
        deadline=Time.time+45;while(guests.Any(c=>c.State!=CustomerOrderState.AwaitingSettlement)&&Time.time<deadline)yield return null;
        foreach(var guest in guests)Assert(guest.CompleteSettlement(),"Shared party settlement failed");
        Assert(menu.Balance-balance==total,"Shared party billed incorrectly");
        Log("PASS actual customer proxy service, shared completion, eating, individual settlement with shared price charged once");
        UnityEngine.Object.Destroy(focus);orbit.FollowTarget=player.transform;
    }
    static IEnumerator CheckDispenserGuide(PrototypePlayerMover player,Day1NarrativeController narrative,CupDispenserPoint dispenser)
    {
        var guide=TavernUI.Instance.GetComponent<TavernGuidance>();
        Set(narrative,"<ManagementUnlocked>k__BackingField",true);
        Set(narrative,"<State>k__BackingField",Day1FlowState.ServingBran);
        foreach(var step in new[]{GuideStep.Awaken,GuideStep.Exit,GuideStep.Lever,GuideStep.Menu})TavernGuidance.Complete(step);
        player.MovementInputEnabled=true;
        yield return new WaitForSeconds(.7f);
        Assert(guide.CurrentStep==GuideStep.Cup,"Cup guidance not selected");
        if(!guide.IsExpanded)guide.ToggleGuide();
        float deadline=Time.time+10;while(!guide.RouteVisible&&Time.time<deadline)yield return null;
        if(!guide.RouteVisible)
        {
            bool source=NavMesh.SamplePosition(player.transform.position,out var sourceHit,1.5f,NavMesh.AllAreas);
            bool target=NavMesh.SamplePosition(dispenser.transform.position,out var targetHit,2,NavMesh.AllAreas);
            Log($"GUIDE DEBUG from={player.transform.position} goal={guide.Destination} navSource={source}/{sourceHit.position} navTarget={target}/{targetHit.position} attempted={guide.RouteCalculationCount} route={((Vector3[])Get(guide,"route")).Length}");
        }
        Assert(guide.RouteVisible,"Cup guidance has no reachable route");
        var marker=(Vector3)Get(guide,"markerDestination");
        Assert(Vector3.Distance(marker,dispenser.GuidancePosition)<.01f&&marker.y-guide.Destination.y>.7f,"Cup beacon was snapped to floor");
        int calculations=guide.RouteCalculationCount;var floor=guide.Destination;
        var orbit=UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>();var focus=new GameObject("DispenserGuideTestFocus");focus.transform.position=dispenser.GuidancePosition;orbit.FollowTarget=focus.transform;
        yield return new WaitForSeconds(2);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("/tmp/dispenser-elevated-guide.png");
        Assert(guide.RouteCalculationCount==calculations&&guide.Destination==floor&&(Vector3)Get(guide,"markerDestination")==marker,"Cup visual target triggers replanning");
        guide.ToggleGuide();UnityEngine.Object.Destroy(focus);orbit.FollowTarget=player.transform;
        var menu=UnityEngine.Object.FindAnyObjectByType<TavernMenuSystem>();menu.Toggle();yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("/tmp/dungeon-demo-menu.png");menu.Close();
        Log("PASS elevated dispenser beacon with reachable floor route and stable target; demo menu screenshot");
    }
    static IEnumerator CheckApproach(PrototypePlayerMover player,Day1NarrativeController narrative)
    {
        var eve=UnityEngine.Object.FindAnyObjectByType<Day1EveActor>(FindObjectsInactive.Include);eve.gameObject.SetActive(true);eve.enabled=false;
        yield return null;
        Set(eve,"player",player.transform);
        var body=player.GetComponent<CharacterController>();var npcBody=eve.GetComponent<CharacterController>();var agent=eve.GetComponent<NavMeshAgent>();
        body.enabled=false;npcBody.enabled=false;agent.enabled=false;
        player.transform.position=new Vector3(500,0,500);eve.transform.position=new Vector3(500,0,503.5f);Physics.SyncTransforms();
        Assert((bool)Call(eve,"CanStartConversation"),"Expanded trigger failed");
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=new Vector3(500,.6f,501.7f);obstacle.transform.localScale=new Vector3(4,1.2f,.7f);Physics.SyncTransforms();
        Assert(!(bool)Call(eve,"CanStartConversation"),"Dialogue crosses wall");obstacle.AddComponent<CounterVaultObstacle>();
        Assert((bool)Call(eve,"CanStartConversation"),"Dialogue cannot cross marked counter");UnityEngine.Object.Destroy(obstacle);
        // Place on a real straight aisle to validate bounded approach and both turns.
        var queue=UnityEngine.Object.FindAnyObjectByType<ServiceOrderQueue>();
        var start=queue.MenuAnchor.position-queue.MenuAnchor.forward*4;
        Assert(NavMesh.SamplePosition(start,out var hit,2,NavMesh.AllAreas),"No dialogue aisle");start=hit.position;
        eve.transform.position=start;agent.enabled=true;agent.Warp(start);npcBody.enabled=true;
        body.enabled=false;player.transform.position=start+queue.MenuAnchor.forward*3.3f;body.enabled=true;Physics.SyncTransforms();
        bool called=false;Action handler=()=>called=true;eve.ConversationRequested+=handler;
        Set(eve,"conversationStarted",false);float before=Time.time,distance=Vector3.Distance(eve.transform.position,player.transform.position);
        yield return (IEnumerator)Call(eve,"SettleBeforeConversation");
        eve.ConversationRequested-=handler;
        Assert(Time.time-before<.6f&&Vector3.Distance(eve.transform.position,start)<=eve.GetComponent<NpcNavigator>().ConfiguredSpeed*.5f+.12f,"Approach exceeded half-second travel limit");
        Assert(Vector3.Distance(eve.transform.position,player.transform.position)<=distance+.03f,"Approach increased pair distance");
        Assert(called,"Approach did not request dialogue");
        player.transform.rotation=Quaternion.LookRotation(eve.transform.position-player.transform.position)*Quaternion.Euler(0,180,0);
        eve.transform.rotation=Quaternion.LookRotation(player.transform.position-eve.transform.position)*Quaternion.Euler(0,180,0);
        Call(narrative,"BeginCloseDialogue",eve.transform);yield return new WaitForSeconds(1.4f);
        var toward=(player.transform.position-eve.transform.position).normalized;
        Assert(Vector3.Dot(eve.transform.forward,toward)>.98f&&Vector3.Dot(player.transform.forward,-toward)>.98f,"Both actors did not face each other");
        Call(narrative,"SetDialogueActive",false);Set(narrative,"closeDialogueActive",false);eve.gameObject.SetActive(false);
        Log("PASS expanded trigger rejects wall/allows counter, bounded 0.5-second approach never increases distance, both actors turn smoothly");
        eve.gameObject.SetActive(true);eve.enabled=false;
        bool testedCounter=false;
        foreach(var counter in UnityEngine.Object.FindObjectsByType<CounterVaultObstacle>())
        {
            var collider=counter.GetComponent<Collider>();if(!collider||collider.isTrigger)continue;
            var bounds=collider.bounds;var normal=bounds.size.x<bounds.size.z?Vector3.right:Vector3.forward;
            float half=bounds.size.x<bounds.size.z?bounds.extents.x:bounds.extents.z;
            var center=new Vector3(bounds.center.x,0,bounds.center.z);
            Log($"COUNTER {counter.name} {bounds} center {center} half {half}");
            if(!NavMesh.SamplePosition(center-normal*(half+.65f),out var npcSide,.4f,NavMesh.AllAreas)
                ||!NavMesh.SamplePosition(center+normal*(half+.65f),out var playerSide,.4f,NavMesh.AllAreas)
                ||Mathf.Abs(npcSide.position.y)>.3f||Mathf.Abs(playerSide.position.y)>.3f)continue;
            body.enabled=false;player.transform.position=playerSide.position;body.enabled=true;
            npcBody.enabled=false;agent.Warp(npcSide.position);eve.transform.position=npcSide.position;npcBody.enabled=true;Physics.SyncTransforms();
            Log($"COUNTER pair {eve.transform.position} / {player.transform.position}, can={(bool)Call(eve,"CanStartConversation")}");
            if(!(bool)Call(eve,"CanStartConversation"))
            {
                var delta=player.transform.position-eve.transform.position;
                foreach(var hitBlock in Physics.CapsuleCastAll(eve.transform.position+Vector3.up*.35f,eve.transform.position+Vector3.up*1.15f,.2f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))Log("BLOCK "+hitBlock.transform.name);
                continue;
            }
            Set(eve,"conversationStarted",false);float began=Time.time;var original=eve.transform.position;
            yield return (IEnumerator)Call(eve,"SettleBeforeConversation");
            Assert(Time.time-began<.6f&&Vector3.Dot(eve.transform.position-center,normal)<0,"Counter approach crossed to player side or exceeded time");
            Assert(Vector3.Distance(eve.transform.position,player.transform.position)<=Vector3.Distance(original,player.transform.position)+.03f,"Counter approach moved away");
            testedCounter=true;break;
        }
        Assert(testedCounter,"Could not verify dialogue across a real counter");eve.gameObject.SetActive(false);
        Log("PASS actual Bar counter conversation remains on NPC side and finishes within half a second");
    }
}

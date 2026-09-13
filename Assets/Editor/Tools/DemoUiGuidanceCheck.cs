using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonTavern.UI;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using UnityEngine;
using UnityEngine.UI;
static class DemoUiGuidanceCheck
{
    static void Assert(bool b,string message){if(!b)throw new Exception(message);}
    static void Log(string s)=>File.AppendAllText("/tmp/demo-flow-playcheck.txt",s+"\n");
    static object Get(object o,string name)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
    static void State(Day1NarrativeController n,Day1FlowState s)=>Set(n,"<State>k__BackingField",s);
    public static IEnumerator Startup(PrototypePlayerMover player,Day1NarrativeController n)
    {
        var ui=TavernUI.Instance;var guide=ui.GetComponent<TavernGuidance>();var menu=UnityEngine.Object.FindAnyObjectByType<TavernMenuSystem>();
        yield return new WaitForSeconds(.6f);
        Assert(!n.ManagementUnlocked&&!ui.StatusRevealed,"Management HUD unlocked before Eve conversation");
        menu.Toggle();Assert(!menu.IsOpen,"M/ledger opens before Eve conversation");
        ScreenCapture.CaptureScreenshot("/tmp/ui-area-arrival.png");yield return new WaitForEndOfFrame();
        Assert(guide.CurrentStep==GuideStep.Awaken&&guide.ButtonVisible&&!guide.TextVisible,"Guide must be available from startup and collapsed");
        n.StopAllCoroutines();
        yield return new WaitForSeconds(3.5f);
        Assert(guide.ButtonVisible&&!guide.TextVisible&&!guide.IsUrgent,"Startup guide should remain quiet and collapsed");
        var button=ui.GetComponentsInChildren<Button>().First(b=>b.name=="GuidanceButton");
        var hover=button.GetComponent<TavernTooltip>();Assert(hover!=null,"Guide tooltip missing");
        hover.OnPointerEnter(null);yield return new WaitForSeconds(.25f);Assert(hover.View.activeInHierarchy,"Guide tooltip did not appear");
        ScreenCapture.CaptureScreenshot("/tmp/ui-guide-tooltip.png");yield return new WaitForEndOfFrame();hover.OnPointerExit(null);
        Assert(!hover.View.GetComponent<CanvasGroup>().blocksRaycasts,"Fading tooltip still blocks clicks");
        yield return new WaitForSeconds(.08f);Assert(hover.View.GetComponent<CanvasGroup>().alpha>0&&hover.View.GetComponent<CanvasGroup>().alpha<1,"Tooltip has no fade-out");
        DemoPolishPlayCheck.ClickUi("GuidanceButton");yield return new WaitForSeconds(.25f);
        Assert(guide.TextVisible,"Guide button did not expand card");
        Assert(ui.GetComponentsInChildren<Text>().Any(t=>t.name=="Key"&&t.text=="W A S D"),"WASD keycap missing");
        ScreenCapture.CaptureScreenshot("/tmp/ui-awakening-delayed.png");yield return new WaitForEndOfFrame();
        TavernGuidance.Complete(GuideStep.Awaken);yield return new WaitForSeconds(.6f);
        Assert(!guide.TextVisible,"Awakening hint remained after accepted input: "+((RectTransform)Get(guide,"card")).GetComponent<CanvasGroup>().alpha);
        State(n,Day1FlowState.AwaitingStorageReturn);player.MovementInputEnabled=true;
        player.GetComponentInChildren<CharacterModelMotion>().EndFullBodyAction();
        Set(guide,"routeDelay",1f);
        yield return new WaitForSeconds(4);
        Assert(guide.CurrentStep==GuideStep.Exit&&guide.ButtonVisible&&guide.IsUrgent&&!guide.TextVisible&&!guide.RouteVisible,"Second timer should glow, not auto-open");
        var glow=(TavernGlowGraphic)Get(guide,"guideGlow");float dim=1,bright=0;
        for(int i=0;i<12;i++){yield return new WaitForSeconds(.28f);dim=Mathf.Min(dim,glow.color.a);bright=Mathf.Max(bright,glow.color.a);}
        Assert(dim>=.27f&&dim<.36f&&bright>.8f,"Guide light does not breathe between dim and bright");
        DemoPolishPlayCheck.ClickUi("GuidanceButton");yield return new WaitForSeconds(.3f);
        float readyUntil=Time.time+10;while(!guide.RouteVisible&&Time.time<readyUntil)yield return null;
        Assert(guide.TextVisible&&guide.RouteVisible,"Click did not show route and card together");
        DemoPolishPlayCheck.ClickUi("GuidanceButton");yield return new WaitForSeconds(.08f);
        float fading=((RectTransform)Get(guide,"card")).GetComponent<CanvasGroup>().alpha;
        Assert(fading>0&&fading<1,"Guide card has no fade-out");
        yield return new WaitForSeconds(.25f);
        Assert(!guide.TextVisible&&!guide.RouteVisible&&guide.ButtonVisible,"Toggle should hide guidance but keep its button");
        DemoPolishPlayCheck.ClickUi("GuidanceButton");yield return new WaitForSeconds(.3f);
        int calculations=guide.RouteCalculationCount;var destination=guide.Destination;
        yield return new WaitForSeconds(2);
        Assert(guide.RouteCalculationCount==calculations&&guide.Destination==destination,"Stationary guidance keeps recalculating or moving destination");
        var route=(Vector3[])Get(guide,"route");Log("B1 path corners="+route.Length);Assert(route.Length>1,"B1 exit route missing");
        ScreenCapture.CaptureScreenshot("/tmp/ui-b1-route.png");yield return new WaitForEndOfFrame();
        var visual=ui.GetComponentInChildren<GuidanceVisualGraphic>();
        Vector3 original=player.transform.position;
        var controller=player.GetComponent<CharacterController>();controller.Move(Vector3.right*.04f);
        yield return new WaitForEndOfFrame();
        Assert(Vector3.Distance(visual.RouteStart,player.transform.position+Vector3.up*.28f)<.001f,"Route start lags small player movement");
        player.transform.position=original;
        var orbit=UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>();var previousFollow=orbit.FollowTarget;
        var framing=new GameObject("GuidanceTestFraming");framing.transform.position=route[^1];orbit.FollowTarget=framing.transform;
        yield return new WaitForSeconds(1);
        ScreenCapture.CaptureScreenshot("/tmp/ui-guidance-beacon.png");yield return new WaitForEndOfFrame();
        orbit.FollowTarget=previousFollow;UnityEngine.Object.Destroy(framing);
        // Start/complete the same real transition event sequence before the first banner has expired.
        var transition=typeof(PlayerAreaTransition);
        for(int i=0;i<2;i++)
        {
            transition.GetMethod("RaiseStarted",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"SealRoom_B1",""});
            Assert(!((RectTransform)Get(guide,"notice")).gameObject.activeSelf,"Stale area banner survives transition start");
            yield return null;
            Assert(!((RectTransform)Get(guide,"notice")).gameObject.activeSelf,"Stale banner flashes during transition");
            transition.GetMethod("RaiseCompleted",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"SealRoom_B1",""});
            Assert(((CanvasGroup)Get(guide,"areaAlpha")).alpha==0,"New area did not restart transparent");
            Assert(((Text)Get(guide,"areaTitle")).text=="封印之间","Wrong new area name");
            yield return new WaitForSeconds(.1f);
        }
        var clock=(float[])Get(guide,"elapsed");float before=clock[(int)GuideStep.Exit];var pause=UnityEngine.Object.FindAnyObjectByType<GamePauseMenu>();pause.SetPaused(true);
        yield return new WaitForSecondsRealtime(.3f);Assert(clock[(int)GuideStep.Exit]==before,"Guidance timer advances while paused");pause.SetPaused(false);
        TavernGuidance.Complete(GuideStep.Exit);yield return new WaitForSeconds(.5f);Assert(!guide.TextVisible&&!guide.RouteVisible&&guide.ButtonVisible,"Pending future guidance button should remain visible");
        Set(guide,"routeDelay",25f);
        Log("PASS startup guide/keycap, continuous route follows 4cm movement, rapid banner replacement; B1 region banner, management gate, opt-in text/route, paused timer and completed hint suppression");
    }
    public static IEnumerator Unlock(PrototypePlayerMover player,Day1NarrativeController n,TavernMenuSystem menu)
    {
        var story=(Ink.Runtime.Story)Get(n,"story");story.ChoosePathString("day01_eve_conversation");
        var next=n.GetType().GetMethod("ShowNextContent",BindingFlags.NonPublic|BindingFlags.Instance);next.Invoke(n,null);
        for(int i=0;i<100&&!n.ManagementUnlocked;i++){n.AdvanceForValidation();yield return null;}
        Assert(n.ManagementUnlocked&&n.State==Day1FlowState.AwaitingOpeningSwitch,"Actual Eve Ink gate did not unlock management");
        var ui=TavernUI.Instance;yield return null;float alpha=ui.StatusOpacity;
        Assert(alpha<1,"HUD reveal skipped animation");yield return new WaitForSeconds(.15f);Assert(ui.StatusOpacity>alpha,"HUD reveal not progressing");
        ScreenCapture.CaptureScreenshot("/tmp/ui-hud-reveal.png");yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(.8f);Assert(ui.StatusOpacity>.99f,"HUD reveal did not finish");
        menu.Toggle();Assert(menu.IsOpen,"Ledger remains locked after Eve");menu.Close();
        Assert(ui.GetComponent<TavernGuidance>().IsComplete(GuideStep.Menu),"Early action did not complete guide");
        Log("PASS actual Eve completion unlocks HUD and M; reveal animates once; early menu completion recorded");
    }
}

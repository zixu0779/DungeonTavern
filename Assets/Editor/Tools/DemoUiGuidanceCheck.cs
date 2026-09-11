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
        n.StopAllCoroutines();State(n,Day1FlowState.AwaitingStorageReturn);player.MovementInputEnabled=true;
        Set(guide,"textDelay",.5f);Set(guide,"routeDelay",1f);
        yield return new WaitForSeconds(4);
        Assert(guide.CurrentStep==GuideStep.Exit&&guide.TextVisible&&guide.RouteVisible,"B1 delayed exit guidance not visible");
        var route=(Vector3[])Get(guide,"route");Log("B1 path corners="+route.Length);Assert(route.Length>1,"B1 exit route missing");
        ScreenCapture.CaptureScreenshot("/tmp/ui-b1-route.png");yield return new WaitForEndOfFrame();
        var clock=(float[])Get(guide,"elapsed");float before=clock[(int)GuideStep.Exit];var pause=UnityEngine.Object.FindAnyObjectByType<GamePauseMenu>();pause.SetPaused(true);
        yield return new WaitForSecondsRealtime(.3f);Assert(clock[(int)GuideStep.Exit]==before,"Guidance timer advances while paused");pause.SetPaused(false);
        TavernGuidance.Complete(GuideStep.Exit);yield return new WaitForSeconds(.5f);Assert(!guide.TextVisible&&!guide.RouteVisible,"Completed guidance remains visible");
        Set(guide,"textDelay",20f);Set(guide,"routeDelay",75f);
        Log("PASS B1 region banner, management gate, delayed text/route, paused timer and completed hint suppression");
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

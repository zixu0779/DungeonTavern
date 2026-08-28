using System;
using System.IO;
using System.Linq;
using DungeonTavern.Gameplay.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class CustomerFlowRuntimeCheck
{
    const string Key="DungeonTavern.CustomerFlowRuntimeCheck";
    const string Output="ArtSource/Previews/CustomerFlow/runtime.txt";
    static double start;
    static BusinessDayController day;
    static CustomerServicePoint customer;
    static PlayerHands hands;
    static CustomerOrderState previous = (CustomerOrderState)(-1);
    [Serializable] sealed class SavedScene { public string path; public bool loaded; public bool active; }
    [Serializable] sealed class SavedSetup { public SavedScene[] scenes; }
    static CustomerFlowRuntimeCheck(){EditorApplication.playModeStateChanged+=OnMode;}
    [MenuItem("Tools/Dungeon Tavern/Run Customer Flow Runtime Check")]
    static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode before this check.");
        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        EditorSceneManager.SaveOpenScenes();
        var setup=new SavedSetup{scenes=EditorSceneManager.GetSceneManagerSetup().Select(s=>new SavedScene{path=s.path,loaded=s.isLoaded,active=s.isActive}).ToArray()};
        if(setup.scenes.Any(s=>string.IsNullOrEmpty(s.path)))throw new Exception("Save untitled scenes first.");
        SessionState.SetString(Key+"Setup",JsonUtility.ToJson(setup));SessionState.SetBool(Key,true);
        File.WriteAllText(Output,"Actual Play Mode, existing Tavern_Main scene. Automated delivery and billing; normal navigation.\n");
        EditorSceneManager.OpenScene("Assets/Scenes/Tavern/Tavern_Main.unity",OpenSceneMode.Single);
        EditorApplication.isPlaying=true;
    }
    static void OnMode(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){start=EditorApplication.timeSinceStartup;EditorApplication.update+=Update;}
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update-=Update;
            var setup=JsonUtility.FromJson<SavedSetup>(SessionState.GetString(Key+"Setup",""));
            SessionState.SetBool(Key,false);
            EditorSceneManager.RestoreSceneManagerSetup(setup.scenes.Select(s=>new SceneSetup{path=s.path,isLoaded=s.loaded,isActive=s.active}).ToArray());
        }
    }
    static void Stop(string result){File.AppendAllText(Output,result+"\n");EditorApplication.update-=Update;EditorApplication.isPlaying=false;}
    static void Update()
    {
        if(!EditorApplication.isPlaying)return;
        try
        {
            if(EditorApplication.timeSinceStartup-start>55){Stop("FAIL: timeout; see last state and navigation diagnostics.");return;}
            if(day==null)
            {
                // The demo starts in B1. This test explicitly exercises the tavern after startup.
                if(Time.time < 2f)return;
                var loader=UnityEngine.Object.FindAnyObjectByType<DungeonTavern.Tavern25D.InitialAdditiveSceneLoader>();
                if(loader!=null)typeof(DungeonTavern.Tavern25D.InitialAdditiveSceneLoader)
                    .GetMethod("SetHostContentVisible",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .Invoke(loader,new object[]{true});
                day=UnityEngine.Object.FindAnyObjectByType<BusinessDayController>();
                if(day==null)return;
                if(!day.enabled||!day.BeginDay()){Stop("FAIL: existing scene cannot begin business day.");return;}
                hands=new GameObject("RuntimeCheckHands").AddComponent<PlayerHands>();
            }
            if(day.State==BusinessDayState.Completed){Stop("PASS: order, seating, food, eating, billing and exit completed in Play Mode.");return;}
            customer=UnityEngine.Object.FindAnyObjectByType<CustomerServicePoint>();
            if(customer==null)return;
            if(previous!=customer.State)
            {
                previous=customer.State;
                File.AppendAllText(Output,$"{Time.time:F2}: {previous} at {customer.transform.position}\n");
            }
            if(customer.State is CustomerOrderState.WaitingForFood or CustomerOrderState.Eating)
            {
                var pending=customer.Order.Portions.FirstOrDefault(p=>!p.Delivered);
                if(pending!=null){hands.TryHold(pending.Item);customer.Interact(hands);}
            }
            if(customer.State==CustomerOrderState.AwaitingSettlement)customer.Interact(hands);
        }
        catch(Exception exception){Stop("FAIL: "+exception);}
    }
}

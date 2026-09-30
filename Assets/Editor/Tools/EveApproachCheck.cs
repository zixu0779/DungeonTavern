using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using DungeonTavern.Prototypes.Rotation25D;

static class EveApproachCheck
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    [MenuItem("Tools/Dungeon Tavern/Check Eve Approach")]
    static void Check()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
        var loader=UnityEngine.Object.FindAnyObjectByType<InitialAdditiveSceneLoader>();
        typeof(InitialAdditiveSceneLoader).GetMethod("SetHostContentVisible",Flags).Invoke(loader,new object[]{true});
        var basement=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SealRoom_B1");
        if(basement.isLoaded)UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(basement);
        var story=UnityEngine.Object.FindAnyObjectByType<Day1NarrativeController>();story.enabled=false;story.StopAllCoroutines();
        typeof(Day1NarrativeController).GetProperty("State").SetValue(story,Day1FlowState.AwaitingEveInteraction);
        var eve=UnityEngine.Object.FindObjectsByType<Day1EveActor>(FindObjectsInactive.Include).Single();eve.gameObject.SetActive(true);eve.gameObject.SetActive(true);
        var agent=eve.GetComponent<NavMeshAgent>();
        var player=UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();
        player.GetComponentInChildren<CharacterModelMotion>().EndFullBodyAction();
        var camera=UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>();camera.EndDialogueFraming();camera.FollowTarget=player.transform;
        var log=new System.Text.StringBuilder();
        int vaults=0, vaultAttempts=0; float nextVault=0;
        var vault=player.GetComponent<CounterVaultController>();
        int scenario=-1;float started=0,triggered=-1;bool requested=false,cancelled=false,locked=false,framed=false;Vector3 lockedAt=default;
        var playerPositions=new[]{new Vector3(38,0,20),new Vector3(33,0,18.8f),new Vector3(24,0,22.8f),new Vector3(38,0,20),new Vector3(33,0,15.7f)};
        var evePositions=new[]{new Vector3(42,0,20),new Vector3(33,0,15),new Vector3(20,0,22.8f),new Vector3(42,0,20),new Vector3(40,0,20)};
        void MovePlayer(Vector3 p){var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.position=p;body.enabled=true;}
        void Next()
        {
            scenario++;if(scenario==playerPositions.Length){File.WriteAllText("/tmp/eve-approach-check.txt",log.ToString());EditorApplication.update-=Tick;return;}
            eve.StopAllCoroutines();agent.enabled=true;eve.GetComponent<NpcNavigator>().enabled=true;camera.EndDialogueFraming();
            typeof(Day1NarrativeController).GetField("eveApproaching",Flags).SetValue(story,false);
            typeof(Day1NarrativeController).GetProperty("State").SetValue(story,Day1FlowState.AwaitingEveInteraction);
            typeof(Day1EveActor).GetField("retryConversationAt",Flags).SetValue(eve,0f);
            MovePlayer(playerPositions[scenario]);player.MovementInputEnabled=true;
            agent.Warp(evePositions[scenario]);eve.transform.position=evePositions[scenario];
            triggered=-1;requested=cancelled=locked=framed=false;started=Time.time;
            eve.BeginArrival();
            if(scenario==4){typeof(Day1EveActor).GetField("retryConversationAt",Flags).SetValue(eve,Time.time+6f);nextVault=Time.time+.25f;}
        }
        eve.ApproachStarted+=()=>{triggered=Time.time;locked=!player.MovementInputEnabled;lockedAt=player.transform.position;framed=(bool)typeof(PrototypeCameraOrbit).GetField("dialogueFraming",Flags).GetValue(camera);if(scenario==3)eve.GetComponent<NpcNavigator>().enabled=false;};
        // Keep the real early-framing subscriber, but isolate the story graph from the close-distance event.
        typeof(Day1EveActor).GetField("ConversationRequested",Flags).SetValue(eve,(Action)(()=>requested=true));
        eve.ApproachCancelled+=()=>cancelled=true;
        void Tick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=Tick;return;}
            if(scenario==4 && vaultAttempts<4 && Time.time>=nextVault && !vault.IsVaulting)
            {
                bool jumped=vault.TryBeginVault(vaultAttempts%2==0?Vector3.forward:Vector3.back);
                if(jumped)vaults++;
                vaultAttempts++;nextVault=Time.time+1.2f;
            }
            if(!requested&&!cancelled&&Time.time-started<25)return;
            float distance=Vector3.ProjectOnPlane(player.transform.position-eve.transform.position,Vector3.up).magnitude;
            bool pass=scenario==3?cancelled&&!requested&&player.MovementInputEnabled:requested&&distance<=(float)typeof(Day1EveActor).GetField("conversationRange",Flags).GetValue(eve)+.03f&&locked&&framed&&Vector3.Distance(lockedAt,player.transform.position)<.03f && (scenario!=4||vaults==4);
            log.AppendLine($"scenario={scenario} pass={pass} requested={requested} cancelled={cancelled} vaults={vaults} distance={distance:F4} elapsed={Time.time-started:F2} afterTrigger={Time.time-triggered:F2} locked={locked} framed={framed} player={player.transform.position} eve={eve.transform.position} path={agent.pathStatus}");
            File.WriteAllText("/tmp/eve-approach-check.txt",log.ToString());Next();
        }
        EditorApplication.update+=Tick;Next();
    }
}

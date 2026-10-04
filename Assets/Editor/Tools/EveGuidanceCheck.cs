using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using DungeonTavern.Prototypes.Rotation25D;
[InitializeOnLoad] static class EveGuidanceCheck {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static readonly Vector3[] Starts={new Vector3(31,0,23),new Vector3(31,0,22),new Vector3(34.5f,0,20),new Vector3(36,0,20),new Vector3(30,0,15)};
 static int scenario;
 static Day1EveActor eve;static Transform goal;static float started;static bool initialized;static double entered;
 static EveGuidanceCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingPlayMode)SessionState.SetBool("EveLeverCheck",false);};entered=EditorApplication.timeSinceStartup;}
 [MenuItem("Tools/Dungeon Tavern/Check Eve Lever Guidance")]
 static void Run(){File.WriteAllText("/tmp/eve-lever-check.txt","Starting\n");SessionState.SetBool("EveLeverCheck",true);initialized=false;scenario=0;entered=EditorApplication.timeSinceStartup;EditorApplication.isPlaying=true;}
 static void Tick(){
 if(!SessionState.GetBool("EveLeverCheck",false))return;
 if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
 if(!initialized){
 if(EditorApplication.timeSinceStartup-entered<5)return;
 var loader=UnityEngine.Object.FindAnyObjectByType<InitialAdditiveSceneLoader>();if(loader==null)return;
 if((float)typeof(InitialAdditiveSceneLoader).GetField("startupOverlayAlpha",F).GetValue(loader)>.001f)return;
 typeof(InitialAdditiveSceneLoader).GetMethod("SetHostContentVisible",F).Invoke(loader,new object[]{true});
 var basement=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SealRoom_B1");if(basement.isLoaded){UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(basement);return;}
 var story=UnityEngine.Object.FindAnyObjectByType<Day1NarrativeController>();if(story==null)return;
 story.enabled=false;story.StopAllCoroutines();goal=(Transform)typeof(Day1NarrativeController).GetField("eveOpeningGuidePoint",F).GetValue(story);
 eve=UnityEngine.Object.FindObjectsByType<Day1EveActor>(FindObjectsInactive.Include).Single();eve.gameObject.SetActive(true);eve.gameObject.SetActive(true);eve.StopAllCoroutines();
 BeginScenario();initialized=true;
 File.AppendAllText("/tmp/eve-lever-check.txt",$"Start {eve.transform.position} Goal {goal.position}\n");return;}
 if(Time.time-started<18&&!eve.IsOpeningGuidanceReady)return;
 var a=eve.GetComponent<NavMeshAgent>();var p=eve.transform.position;
 var near=Physics.OverlapCapsule(p+Vector3.up*.3f,p+Vector3.up*1.2f,.4f).Where(c=>!c.isTrigger&&!c.transform.IsChildOf(eve.transform)).Select(c=>c.name+":"+c.bounds);
 File.AppendAllText("/tmp/eve-lever-check.txt",$"ready={eve.IsOpeningGuidanceReady} pos={p} target={goal.position} dest={a.destination} path={a.pathStatus} remaining={a.remainingDistance} velocity={a.velocity} desired={a.desiredVelocity} stopped={a.isStopped}\nCorners="+string.Join(";",a.path.corners.Select(v=>v.ToString()))+"\nColliders="+string.Join(";",near)+"\n");
 bool pass=eve.IsOpeningGuidanceReady && Vector3.ProjectOnPlane(p-goal.position,Vector3.up).magnitude<.3f;
 File.AppendAllText("/tmp/eve-lever-check.txt",$"scenario={scenario} pass={pass} elapsed={Time.time-started:F2}\n");
 if(++scenario<Starts.Length){BeginScenario();return;}
 SessionState.SetBool("EveLeverCheck",false);Debug.Log("EVE_LEVER_CHECK_COMPLETE");
 }
 static void BeginScenario(){
 eve.GetComponent<NpcNavigator>().Stop(true);
 var a=eve.GetComponent<NavMeshAgent>();if(!NavMesh.SamplePosition(Starts[scenario],out var hit,1.5f,a.areaMask))throw new Exception("Test start is off navigation");
 var body=eve.GetComponent<CharacterController>();body.enabled=false;
 if(!a.Warp(hit.position))throw new Exception("Test warp failed");eve.transform.position=hit.position;a.nextPosition=hit.position;Physics.SyncTransforms();body.enabled=true;
 eve.BeginOpeningSwitchGuidance(goal,"");started=Time.time;
 File.AppendAllText("/tmp/eve-lever-check.txt",$"scenario={scenario} start={hit.position}\n");
 }

}

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Gameplay.Interaction;
// Play Mode smoke check: opening input lock and real CharacterController vault over a counter-height fixture.
public static class OpeningVaultCheck {
 static PrototypePlayerMover player;static CharacterModelMotion motion;static Animator animator;static Day1NarrativeController story;static GameObject fixture;static Vector3 origin,start;static Quaternion rotation;static double at;static int phase;static string log;static HeldItem item;static int riseShot;static Keyboard testKeyboard;static string runtimeError;static Vector3 proneLeft,proneRight;
 [MenuItem("Tools/Characters/Test Opening Rise")]
 static void Test(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");
  EditorSceneManager.OpenScene("Assets/Scenes/Tavern/Tavern_Main.unity",OpenSceneMode.Single);
  SessionState.SetBool("OpeningRiseTest",true);EditorApplication.isPlaying=true;
 }
 [InitializeOnLoadMethod] static void ResumeTest(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("OpeningRiseTest",false)){SessionState.SetBool("OpeningRiseTest",false);testAt=EditorApplication.timeSinceStartup;EditorApplication.update+=WaitForOpening;}};}
 static double testAt;
 static void WaitForOpening(){
  if(!EditorApplication.isPlaying){EditorApplication.update-=WaitForOpening;return;}
  var player=UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();
  var story=UnityEngine.Object.FindAnyObjectByType<DungeonTavern.Tavern25D.Narrative.Day1NarrativeController>();
  if(!player||!player.GetComponentInChildren<Animator>()||!story||story.State!=DungeonTavern.Tavern25D.Narrative.Day1FlowState.Awakening){if(EditorApplication.timeSinceStartup-testAt>60){EditorApplication.update-=WaitForOpening;Debug.LogError("Opening test timed out waiting for Awakening");}return;}
  EditorApplication.update-=WaitForOpening;EditorApplication.ExecuteMenuItem("Tools/Characters/Check Opening and Vault");
 }
 [MenuItem("Tools/Characters/Check Opening and Vault")]
 static void Run(){
  if(!EditorApplication.isPlaying)throw new Exception("Start Play Mode first");
  player=UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();story=UnityEngine.Object.FindAnyObjectByType<Day1NarrativeController>();motion=player.GetComponentInChildren<CharacterModelMotion>();animator=motion.GetComponent<Animator>();origin=player.transform.position;rotation=player.transform.rotation;item=player.GetComponent<PlayerHands>().CurrentItem;log="";
  while(story.State==Day1FlowState.Dialogue)if(!story.AdvanceForValidation())break;
  runtimeError=null;Application.logMessageReceived+=RecordError;testKeyboard=InputSystem.AddDevice<Keyboard>();riseShot=0;phase=0;at=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
 }
 static void RecordError(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)runtimeError=message;}
 static void Assert(bool b,string s){if(!b)throw new Exception(s);log+="PASS "+s+"\n";}
 static void Tick(){try{if(!EditorApplication.isPlaying){End();return;}double t=EditorApplication.timeSinceStartup-at;
  if(phase==0&&t>.6){Assert(story.State==Day1FlowState.Awakening,"Opening awaits movement");Assert(!player.MovementInputEnabled&&motion.IsFullBodyAction,"Opening locks movement");Assert(animator.GetCurrentAnimatorStateInfo(0).IsName("Prone"),"Prone state");proneLeft=animator.GetBoneTransform(HumanBodyBones.LeftHand).position;proneRight=animator.GetBoneTransform(HumanBodyBones.RightHand).position;Shot("prone");InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.W));phase++;at=EditorApplication.timeSinceStartup;}
  else if(phase==1&&t>.15){InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());Assert(!player.MovementInputEnabled,"Wake input does not walk");Assert(Vector3.Distance(proneLeft,animator.GetBoneTransform(HumanBodyBones.LeftHand).position)<.15f&&Vector3.Distance(proneRight,animator.GetBoneTransform(HumanBodyBones.RightHand).position)<.15f,"Wake begins continuously from prone hands");Shot("waking");phase++;at=EditorApplication.timeSinceStartup;}
  else if(phase==2&&t<4.7){if(t>riseShot*.5){Shot("rise-"+riseShot);riseShot++;}}
  else if(phase==2&&t>=4.7){Assert(story.State==Day1FlowState.AwaitingStorageReturn,"Opening reaches exploration");Assert(player.MovementInputEnabled&&!motion.IsFullBodyAction,"Standing restores movement");Assert(Vector3.Distance(origin,player.transform.position)<.15f,"No opening root drift");
   fixture=new GameObject("VaultCheckFixture");Make("Floor",new Vector3(1000,-.1f,1000),new Vector3(12,.2f,12),false);var bar=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Models/Bar/Bar.prefab"),fixture.transform);var counter=System.Array.Find(bar.GetComponentsInChildren<Collider>(),c=>c.name=="Counter_Long_Collision");bar.transform.position+=new Vector3(1000,.55f,1001)-counter.bounds.center;
   var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(1000,0,999.8f);player.transform.rotation=Quaternion.identity;cc.enabled=true;Physics.SyncTransforms();start=player.transform.position;
   Assert(player.GetComponent<CounterVaultController>().TryBeginVault(Vector3.forward),"Vault begins at counter");Assert(!player.GetComponent<CounterVaultController>().TryBeginVault(Vector3.forward),"Duplicate vault rejected");phase++;at=EditorApplication.timeSinceStartup;}
  else if(phase==3&&t>.3){Assert(animator.GetCurrentAnimatorStateInfo(0).IsName("Vault"),"Vault animation active");Assert(!player.MovementInputEnabled,"Vault locks movement");Shot("vault");phase++;at=EditorApplication.timeSinceStartup;}
  else if(phase==4&&t>1.1){Assert(!player.GetComponent<CounterVaultController>().IsVaulting,"Vault ends");Assert(player.MovementInputEnabled&&player.GetComponent<CharacterController>().enabled,"Landing restores control and collision");Assert(player.transform.position.z>1001.7f&&Mathf.Abs(player.transform.position.y)<.1f,"Land beyond counter at floor height");Assert(animator.isHuman&&animator.avatar.isValid,"Replacement humanoid valid");Assert(runtimeError==null,"No runtime errors: "+runtimeError);Shot("standing");End();}
 }catch(Exception e){log+="FAIL "+e+"\n";End();}}
 static void Make(string name,Vector3 pos,Vector3 scale,bool obstacle){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(fixture.transform);go.transform.position=pos;go.transform.localScale=scale;if(obstacle)go.AddComponent<CounterVaultObstacle>();}
 static void End(){EditorApplication.update-=Tick;Application.logMessageReceived-=RecordError;if(testKeyboard!=null){InputSystem.RemoveDevice(testKeyboard);testKeyboard=null;}if(fixture)UnityEngine.Object.Destroy(fixture);if(player){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.SetPositionAndRotation(origin,rotation);cc.enabled=true;player.MovementInputEnabled=true;motion.EndFullBodyAction();}File.WriteAllText(Path.Combine(Path.GetTempPath(),"opening-vault-check.txt"),log);Debug.Log(log);}
 static void Shot(string name){var others=UnityEngine.Object.FindObjectsByType<Renderer>();var hidden=new System.Collections.Generic.List<Renderer>();foreach(var r in others)if(r.enabled&&!r.transform.IsChildOf(player.transform)){r.enabled=false;hidden.Add(r);}var go=new GameObject("ActionCamera");var c=go.AddComponent<Camera>();c.orthographic=true;c.orthographicSize=1.65f;c.transform.position=player.transform.position+player.transform.right*3+player.transform.forward*3+Vector3.up*2;c.transform.LookAt(player.transform.position+Vector3.up*.9f);var rt=new RenderTexture(900,900,24);c.targetTexture=rt;c.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(900,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"action-"+name+".png"),tex.EncodeToPNG());RenderTexture.active=old;c.targetTexture=null;UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);foreach(var r in hidden)if(r)r.enabled=true;}
}

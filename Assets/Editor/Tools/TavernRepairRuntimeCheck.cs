using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
internal static class TavernRepairRuntimeCheck
{
 const string Out="ArtSource/Previews/TavernRepairReview/";
 static PrototypePlayerMover player;static PlayerHands hands;static PlayerInteractionController interact;
 static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
 static void Check(bool pass,string text){if(!pass)throw new Exception(text);File.AppendAllText(Out+"runtime.txt","PASS "+text+"\n");}
 static void Move(Vector3 p){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=p;cc.enabled=true;Physics.SyncTransforms();cc.Move(Vector3.down*.01f);}
 [MenuItem("Tools/Dungeon Tavern/Test Tavern Repairs")]
 static void Start(){if(!EditorApplication.isPlaying)throw new Exception("Play required");File.WriteAllText(Out+"runtime.txt","");player=Find<PrototypePlayerMover>();if(!player)throw new InvalidOperationException("Wait for the B1 startup player to load before running the check.");player.StartCoroutine(Guard(Run()));}
 static IEnumerator Guard(IEnumerator run){while(true){object value;try{if(!run.MoveNext())break;value=run.Current;}catch(Exception e){File.AppendAllText(Out+"runtime.txt","FAIL "+e);Debug.LogException(e);yield break;}yield return value;}File.AppendAllText(Out+"runtime.txt","ALL PASSED\n");}
 static IEnumerator Run()
 {
  var narrative=Find<Day1NarrativeController>();narrative.StopAllCoroutines();narrative.enabled=false;player.MovementInputEnabled=false;var orbit=Find<PrototypeCameraOrbit>();orbit.EndDialogueFraming();
  EditorApplication.ExecuteMenuItem("Tools/Dungeon Tavern/Test B1 Return Trigger");yield return new WaitForSeconds(3);
  hands=player.GetComponent<PlayerHands>();interact=player.GetComponent<PlayerInteractionController>();hands.Clear();Move(new Vector3(33.7f,.05f,16));yield return null;
  var cup=Find<CupDispenserPoint>();Check(interact.TryInteract()&&cup.CupReady,"dispenser generates visible floating WoodenCup");yield return new WaitForSeconds(1.2f);
  Check(cup.transform.Find("FloatingCup").gameObject.activeInHierarchy,"floating cup is active in main scene");Check(interact.TryInteract()&&hands.CurrentItem==HeldItem.EmptyCup,"second F takes cup");yield return null;
  var visual=player.GetComponent<HeldCupVisual>();Check(visual&&visual.Cup&&visual.Cup.activeInHierarchy,"wooden cup attached to character grip anchor");
  Move(new Vector3(32.38f,.05f,21.8f));yield return null;Check(interact.TryInteract()&&hands.CurrentItem==HeldItem.TestDrink&&visual.Cup.activeSelf,"filled cup keeps its held model");
  var chest=UnityEngine.Object.FindObjectsByType<ChestInteractionPoint>().First();Move(chest.transform.position+Vector3.back*1.1f);yield return null;var prop=chest.GetComponent<TwoStateProp>();var animator=chest.GetComponent<Animator>();var lid=chest.transform.Find("LidPivot");
  Check(chest.GetPrompt(hands)=="F：打开箱子"&&interact.TryInteract(),"closed chest offers open action");Check(!chest.Interact(hands),"repeated F ignored during transition");yield return new WaitForSeconds(.2f);Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Opening"),"opening plays Opening clip");yield return new WaitForSeconds(.5f);
  Check(chest.GetPrompt(hands)=="F：关闭箱子"&&Mathf.Abs(Mathf.DeltaAngle(lid.localEulerAngles.x,-25))<.1f,"open pose and prompt agree without endpoint jump");Check(interact.TryInteract(),"open chest accepts closing action");yield return new WaitForSeconds(.2f);Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Closing"),"closing plays Closing clip");yield return new WaitForSeconds(.5f);Check(!prop.IsOpen&&Quaternion.Angle(lid.localRotation,Quaternion.identity)<.1f,"closing ends at Closed pose");
  interact.TryInteract();yield return new WaitForSeconds(.7f);Move(chest.transform.position+Vector3.back*4);yield return new WaitForSeconds(1.4f);Check(!prop.IsOpen&&animator.GetCurrentAnimatorStateInfo(0).IsName("Closed"),"leaving 2.4m closes chest after delay");
  for(int i=0;i<4;i++){var start=orbit.transform.rotation;orbit.RotateRight();Check(Quaternion.Angle(start,orbit.transform.rotation)<.01f,"camera does not snap on input");yield return new WaitForSeconds(.08f);var angle=Quaternion.Angle(start,orbit.transform.rotation);Check(angle>1&&angle<89,"camera has intermediate eased rotation");yield return new WaitForSeconds(.3f);Check(Mathf.Abs(Mathf.DeltaAngle(orbit.transform.eulerAngles.y,orbit.CurrentCardinalYaw))<.01f,"camera settles on fixed heading "+orbit.CurrentCardinalYaw);}
  var door=UnityEngine.Object.FindObjectsByType<DoorStateController>().Single(d=>d.name=="Door_Small_Stone_3");Move(new Vector3(46,.05f,17.5f));yield return new WaitForSeconds(.5f);Check(door.IsOpen,"storage to kitchen door opens automatically");var cc=player.GetComponent<CharacterController>();for(int i=0;i<60;i++){cc.Move(Vector3.back*.035f);yield return new WaitForFixedUpdate();}Check(player.transform.position.z<16,"player crosses storage kitchen doorway");
  Move(new Vector3(47.48f,.05f,22.9f));float low=0;bool travelled=false;for(int i=0;i<220;i++){if(SceneManager.GetSceneByName("SealRoom_B1").isLoaded){travelled=true;break;}cc.Move(Vector3.forward*.025f);low=Mathf.Min(low,player.transform.position.y);yield return new WaitForFixedUpdate();}
  yield return new WaitForSeconds(2);Check(low<-.65f,"player descends actual steps below F1 floor: "+low);Check(travelled&&SceneManager.GetSceneByName("SealRoom_B1").isLoaded,"walking down stairs triggers F1 to B1 without teleporting to trigger");
  Check(UnityEngine.Object.FindObjectsByType<PrototypePlayerMover>().Length==1,"round trip retains one player");EditorApplication.ExecuteMenuItem("Tools/Dungeon Tavern/Test B1 Return Trigger");yield return new WaitForSeconds(3);Check(!SceneManager.GetSceneByName("SealRoom_B1").isLoaded,"B1 to F1 return still works");
  Move(new Vector3(33.7f,.05f,16));yield return null;
 }
}

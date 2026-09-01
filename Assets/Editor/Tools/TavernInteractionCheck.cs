using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
internal static class TavernInteractionCheck
{
 const string Out="ArtSource/Previews/TavernInteractionRevision/";
 static PrototypePlayerMover player;
 static PlayerInteractionController interaction;
 static PlayerHands hands;
 static void Assert(bool condition,string message){if(!condition)throw new Exception(message);File.AppendAllText(Out+"runtime.txt","PASS "+message+"\n");}
 static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
 static void Move(Vector3 p){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=p;cc.enabled=true;Physics.SyncTransforms();cc.Move(Vector3.down*.01f);}
 [MenuItem("Tools/Dungeon Tavern/Test Tavern Interactions")]
 static void Start(){if(!EditorApplication.isPlaying)throw new Exception("Play required");File.WriteAllText(Out+"runtime.txt","");player=Find<PrototypePlayerMover>();player.StartCoroutine(Guard(Check()));}
 static IEnumerator Guard(IEnumerator routine){while(true){object current;try{if(!routine.MoveNext())break;current=routine.Current;}catch(Exception ex){File.AppendAllText(Out+"runtime.txt","FAIL "+ex);Debug.LogException(ex);yield break;}yield return current;}File.AppendAllText(Out+"runtime.txt","ALL PASSED\n");}
 static IEnumerator Check()
 {
  var narrative=Find<Day1NarrativeController>();if(narrative){narrative.StopAllCoroutines();narrative.enabled=false;}
  var orbit=Find<PrototypeCameraOrbit>();orbit.EndDialogueFraming();player.MovementInputEnabled=false;
  EditorApplication.ExecuteMenuItem("Tools/Dungeon Tavern/Test B1 Return Trigger");yield return new WaitForSeconds(3);
  interaction=player.GetComponent<PlayerInteractionController>();hands=player.GetComponent<PlayerHands>();hands.Clear();
  var cup=Find<CupDispenserPoint>();Assert(cup!=null,"F1 active after real return portal");
  var barrel=Find<DrinkBarrelPoint>();Assert(barrel.GetPrompt(hands)==""&&!barrel.Interact(hands),"empty hands cannot draw beer");
  Move(new Vector3(33.7f,.05f,16f));yield return null;
  Assert(interaction.TryInteract()&&cup.CupReady&&hands.CurrentItem==HeldItem.None,"first F activates dispenser without taking cup");
  yield return new WaitForSeconds(1.3f);
  Assert(cup.GetComponent<CupDispenserActivation>().IsActivated&&cup.transform.Find("FloatingCup").gameObject.activeSelf,"floating cup visible with energized ring");
  var p=cup.transform.Find("FloatingCup").localPosition;yield return new WaitForSeconds(.25f);Assert(Vector3.Distance(p,cup.transform.Find("FloatingCup").localPosition)>.001f,"cup visibly bobs over time");
  Assert(interaction.TryInteract()&&!cup.CupReady&&hands.CurrentItem==HeldItem.EmptyCup&&!cup.GetComponent<CupDispenserActivation>().IsActivated,"second F takes empty cup and deactivates dispenser");
  Assert(!cup.Interact(hands),"occupied hands cannot create extra cup");
  Move(new Vector3(32.38f,.05f,21.8f));yield return null;
  Assert(interaction.CurrentPrompt=="F：接酒"&&interaction.TryInteract()&&hands.CurrentItem==HeldItem.TestDrink,"F at barrel fills empty cup");
  Assert(barrel.GetPrompt(hands)==""&&!barrel.Interact(hands),"full cup cannot refill");
  Move(new Vector3(22.24f,.05f,15.2f));yield return null;
  Assert(interaction.CurrentPrompt=="F：查看订单"&&interaction.TryInteract(),"F menu prompt has no parenthetical shortcut and opens orders");Find<TavernMenuSystem>().Toggle();
  var lever=Find<FloorLeverPoint>();Move(lever.transform.position+Vector3.back*.8f);yield return null;var h=lever.transform.Find("HandlePivot");var rest=h.localRotation;
  Assert(interaction.TryInteract()&&lever.IsOn,"F toggles new lever on");yield return new WaitForSeconds(.6f);Assert(Quaternion.Angle(rest,h.localRotation)>45,"lever rotates about authored bottom pivot");
  Assert(interaction.TryInteract()&&!lever.IsOn,"F toggles lever off");yield return new WaitForSeconds(.6f);Assert(Quaternion.Angle(rest,h.localRotation)<.01f,"lever returns to authored rotation");
  foreach(var chest in UnityEngine.Object.FindObjectsByType<ChestInteractionPoint>()){Move(chest.transform.position+Vector3.back*1.1f);yield return null;var lid=chest.transform.Find("LidPivot");var q=lid.localRotation;Assert(interaction.TryInteract()&&chest.GetComponent<TwoStateProp>().IsOpen,"F opens "+chest.name);yield return new WaitForSeconds(1.2f);Assert(Quaternion.Angle(q,lid.localRotation)>5,"chest lid animation plays");interaction.TryInteract();}
  for(int i=0;i<4;i++){orbit.RotateRight();Assert(Mathf.Abs(Mathf.Repeat(orbit.CurrentCardinalYaw-45,90))<.01f,"camera fixed diagonal "+orbit.CurrentCardinalYaw);}
  var doors=UnityEngine.Object.FindObjectsByType<DoorStateController>();var bar=doors.Single(d=>d.name=="Gate_East");var storage=doors.Single(d=>d.name=="Door_Small_Stone_2");
  Move(new Vector3(36.2f,.05f,20));yield return new WaitForSeconds(1);Assert(storage.IsOpen&&!bar.IsOpen,"storage approach opens only storage door");
  var destination=new Vector3(35.7f,.05f,17.13f);var controller=player.GetComponent<CharacterController>();
  for(int step=0;step<150;step++){var delta=destination-player.transform.position;delta.y=0;if(delta.magnitude<.08f)break;controller.Move(Vector3.ClampMagnitude(delta,.04f));yield return new WaitForFixedUpdate();}
  yield return new WaitForSeconds(1);Assert(bar.IsOpen&&!storage.IsOpen,"long bar gate opens independently");
  Move(new Vector3(22.33f,.05f,22.99f));yield return new WaitForSeconds(1);Assert(doors.Single(d=>d.name=="Gate_North").IsOpen,"short bar gate remains automatic");
  Assert(!UnityEngine.Object.FindObjectsByType<Transform>().Any(t=>t.name=="HingeJambCollider"),"oversized hinge blockers removed");
  Move(new Vector3(33.7f,.05f,16));hands.Clear();yield return null;interaction.TryInteract();yield return new WaitForSeconds(1.2f);
 }
}

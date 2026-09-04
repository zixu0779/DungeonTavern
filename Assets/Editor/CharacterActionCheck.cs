using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DungeonTavern.Tavern25D;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
// A repeatable Play Mode animation check; temporary changes are restored.
public static class CharacterActionCheck {
 static GameObject npc;static Animator npcAnimator;static Transform npcHip;static float npcHipY;
 static CharacterModelMotion motion;static Animator animator;static PlayerHands hands;static PrototypePlayerMover mover;static HeldItem before;static bool input;static double at;static int phase;static float hipHeight;static string log;
 [MenuItem("Tools/Characters/Check Drink and Seating")]
 static void Run(){if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode and wait for the player first.");
  mover=UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();if(!mover)throw new InvalidOperationException("Player is not loaded.");
  motion=mover.GetComponentInChildren<CharacterModelMotion>();animator=motion.GetComponent<Animator>();hands=mover.GetComponent<PlayerHands>();before=hands.CurrentItem;input=mover.MovementInputEnabled;mover.MovementInputEnabled=false;hands.Clear();hands.TryHold(HeldItem.TestDrink);hipHeight=animator.GetBoneTransform(HumanBodyBones.Hips).position.y;npc=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Characters/Placeholder/Rogue.prefab"));npc.transform.position=mover.transform.position+Vector3.right*2;npc.transform.localScale=Vector3.one*.35f;npcAnimator=npc.GetComponent<Animator>();npcHip=System.Linq.Enumerable.First(npc.GetComponentsInChildren<Transform>(),t=>t.name=="hips");npcHipY=npcHip.position.y;npcAnimator.SetBool("Seated",true);
  phase=0;log="";at=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
 }
 static void Assert(bool yes,string label){if(!yes)throw new Exception(label);log+="PASS "+label+"\n";}
 static void Tick(){try{if(!EditorApplication.isPlaying){Finish();return;}double t=EditorApplication.timeSinceStartup-at;
  if(phase==0&&t>1.5){motion.PlayDrink();phase++;at=EditorApplication.timeSinceStartup;}
  else if(phase==1&&t>1){Assert(npcAnimator.GetCurrentAnimatorStateInfo(0).IsName("SeatedIdle"),"NPC seated idle");Assert(npcHip.position.y<npcHipY-.05f,"NPC pelvis lowered");Assert(animator.GetCurrentAnimatorStateInfo(1).IsName("Drink"),"Drink state");Assert(hands.GetComponent<HeldCupVisual>().DrinkTilt>.8f,"Animated drink curve");Capture("drink");phase++;at=EditorApplication.timeSinceStartup;}
  else if(phase==2&&t>1.8){Assert(animator.GetCurrentAnimatorStateInfo(1).IsName("HoldCup"),"Return to holding");Assert(hands.CurrentItem==HeldItem.TestDrink,"Animation preserves inventory");motion.SetSeated(true);phase++;at=EditorApplication.timeSinceStartup;}
  else if(phase==3&&t>2){Assert(animator.GetCurrentAnimatorStateInfo(0).IsName("SeatedIdle"),"Seated idle");Assert(animator.GetBoneTransform(HumanBodyBones.Hips).position.y<hipHeight-.1f,"Pelvis lowered");Capture("seated");motion.SetSeated(false);phase++;at=EditorApplication.timeSinceStartup;}
  else if(phase==4&&t>1.8){Assert(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"Stand then idle");Finish();}
 }catch(Exception e){log+="FAIL "+e+"\n";Finish();}}
 static void Finish(){EditorApplication.update-=Tick;if(npc)UnityEngine.Object.Destroy(npc);if(mover){motion.SetSeated(false);hands.Clear();if(before!=HeldItem.None)hands.TryHold(before);mover.MovementInputEnabled=input;}File.WriteAllText(Path.Combine(Path.GetTempPath(),"character-action-check.txt"),log);Debug.Log(log);}
 static void Capture(string name){var go=new GameObject("ActionCheckCamera");var c=go.AddComponent<Camera>();c.orthographic=true;c.orthographicSize=.95f;c.transform.position=mover.transform.position+mover.transform.right*2+mover.transform.forward*2+Vector3.up*1.25f;c.transform.LookAt(mover.transform.position+Vector3.up*.75f);var rt=new RenderTexture(800,800,24);c.targetTexture=rt;c.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(800,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,800,800),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"character-"+name+".png"),tex.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);}
}

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
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

static class DemoPolishPlayCheck
{
    static object Get(object obj,string field)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    static void Log(string value)=>File.AppendAllText("/tmp/demo-flow-playcheck.txt",value+"\n");
    public static void CacheLeverGrip()
    {
        var lever=UnityEngine.Object.FindAnyObjectByType<FloorLeverPoint>();
        var pivot=(Transform)Get(lever,"handlePivot");
        var points=pivot.GetComponentsInChildren<MeshFilter>().SelectMany(m=>m.sharedMesh.vertices.Select(v=>m.transform.TransformPoint(v))).ToArray();
        float top=points.Max(p=>p.y),bottom=points.Min(p=>p.y);
        var gripPoints=points.Where(p=>p.y>top-(top-bottom)*.12f).ToArray();
        var grip=gripPoints.Aggregate(Vector3.zero,(a,b)=>a+b)/gripPoints.Length;
        UnityEditor.SessionState.SetVector3("DemoPolish.LeverGrip",pivot.InverseTransformPoint(grip));
    }
    public static IEnumerator Run(PrototypePlayerMover player)
    {
        var orbit=UnityEngine.Object.FindAnyObjectByType<PrototypeCameraOrbit>();
        var hands=player.GetComponent<PlayerHands>();
        var body=player.GetComponent<CharacterController>();
        var lever=UnityEngine.Object.FindAnyObjectByType<FloorLeverPoint>();
        var day=UnityEngine.Object.FindAnyObjectByType<BusinessDayController>();
        var menu=UnityEngine.Object.FindAnyObjectByType<TavernMenuSystem>();
        var narrative=UnityEngine.Object.FindAnyObjectByType<Day1NarrativeController>();
        var chest=UnityEngine.Object.FindObjectsByType<ChestInteractionPoint>().First();
        chest.enabled=false;var prop=chest.GetComponent<TwoStateProp>();
        var lid=chest.transform.Find("LidPivot");
        for(int pass=0;pass<2;pass++)
        {
            bool open=pass==0;prop.SetOpen(open);float previous=lid.localEulerAngles.x;float stop=Time.time+1.2f;
            while(Time.time<stop)
            {
                yield return null;Assert(!string.IsNullOrEmpty(chest.GetPrompt(hands)),"Chest prompt disappears during animation");float current=lid.localEulerAngles.x;float delta=Mathf.DeltaAngle(previous,current);
                Assert(chest.GetPrompt(hands)==(prop.IsTransitioning?(open?"F：打开箱子":"F：关闭箱子"):(open?"F：关闭箱子":"F：打开箱子")),"Chest prompt changed before animation completed");
                Assert(open?delta<.3f:delta>-.3f,"Chest animation reverses/repeats mid-transition");previous=current;
            }
            Assert(!prop.IsTransitioning&&Mathf.Abs(Mathf.DeltaAngle(previous,open?-25:0))<.2f,"Chest final pose wrong");
        }
        chest.enabled=true;Log("PASS chest opening/closing monotonic, single motion, correct stable poses");
        player.MovementInputEnabled=false;player.enabled=false;
        var obstacle=player.GetComponent<NavMeshObstacle>();obstacle.enabled=false;
        foreach(var door in UnityEngine.Object.FindObjectsByType<DoorStateController>().Where(d=>d.name.StartsWith("Door_Small")))
        for(int direction=-1;direction<=1;direction+=2)
        {
            var forward=door.transform.forward*direction;
            body.enabled=false;player.transform.position=door.transform.position-forward*1.25f+Vector3.up*.02f;Physics.SyncTransforms();body.enabled=true;
            door.Open();
            for(int warmup=0;warmup<20;warmup++){body.Move(Vector3.down*2*Time.deltaTime);yield return null;}
            float min=float.PositiveInfinity,max=float.NegativeInfinity,stop=Time.time+1.2f;
            while(Time.time<stop)
            {
                body.Move((forward*2.5f+Vector3.down*2)*Time.deltaTime);yield return null;
                // Exclude teleport contact settling outside the door; measure the entire frame passage itself.
                if(Mathf.Abs(Vector3.Dot(player.transform.position-door.transform.position,forward))<.75f)
                { min=Mathf.Min(min,player.transform.position.y);max=Mathf.Max(max,player.transform.position.y); }
            }
            Assert(Vector3.Dot(player.transform.position-door.transform.position,forward)>.7f,"Door blocks passage "+door.name);
            if(max-min>=.04f)Log($"DOOR TRACE {door.name} direction={direction} variation={min}..{max}");
            Assert(!float.IsInfinity(min)&&max-min<.02f,$"Door causes vertical bump {door.name}: {min}..{max}");
        }
        obstacle.enabled=true;player.enabled=true;player.MovementInputEnabled=true;
        Log("PASS all three small doors crossed both ways, vertical variation below 2 cm");
        body.enabled=false;player.transform.position=new Vector3(30,0,14);Physics.SyncTransforms();body.enabled=true;
        var pivot=(Transform)Get(lever,"handlePivot");
        var localGrip=UnityEditor.SessionState.GetVector3("DemoPolish.LeverGrip",Vector3.zero);
        Vector3 restDirection=pivot.TransformPoint(localGrip)-pivot.position;
        Assert(lever.Interact(hands),"Reopen rejected");
        yield return new WaitForSeconds(.15f);
        var keyboard=InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));
        yield return null; yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return null;
        InputSystem.RemoveDevice(keyboard);
        yield return new WaitForSeconds(.5f);
        Assert(orbit.EntranceFraming,"Interrupted entrance camera returned faster than opening pan");
        yield return new WaitForSeconds(.9f);
        Assert(!orbit.EntranceFraming&&player.MovementInputEnabled,"Interrupted entrance camera did not return control");
        while(lever.IsSwitching)yield return null;
        var movedDirection=pivot.TransformPoint(localGrip)-pivot.position;
        Assert(Mathf.Abs(restDirection.y-movedDirection.y)<.02f&&Vector3.Distance(new Vector3(-restDirection.x,restDirection.y,-restDirection.z),movedDirection)<.03f,"Lever endpoints not symmetric about vertical");
        Log("PASS lever grip reflected with equal ground angle; camera can return early while door/sign complete");
        var entry=new CustomerScheduleEntry();entry.Configure("EarlyClosing",0,HeldItem.TestDrink,Color.white);entry.SetArrivalKind(CustomerArrivalKind.Party);
        Assert((bool)typeof(BusinessDayController).GetMethod("TrySpawn",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(day,new object[]{entry}),"Could not spawn party for closure check");
        var customers=UnityEngine.Object.FindObjectsByType<CustomerServicePoint>();
        Assert(customers.Length>=2,"Need multiple guests");
        var seated=customers[0];var cc=seated.GetComponent<CharacterController>();cc.enabled=false;
        seated.transform.position=seated.AssignedSeat.Position;Physics.SyncTransforms();seated.GetComponent<NavMeshAgent>().Warp(seated.transform.position);cc.enabled=true;
        typeof(CustomerServicePoint).GetMethod("ChangeState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(seated,new object[]{CustomerOrderState.WaitingForFood,true});
        menu.RegisterOrder(seated,seated.Order);
        yield return new WaitForSeconds(2);
        int balance=menu.Balance;int waiting=day.WaitingCustomers;
        var departureFrames=new List<int>();foreach(var c in customers)c.StateChanged+=state=>{if(state==CustomerOrderState.Leaving)departureFrames.Add(Time.frameCount);};
        Quaternion beforeTurn=player.transform.rotation;
        Assert(lever.Interact(hands)&&!orbit.EntranceFraming,"Temporary close should keep player view");
        Assert(Quaternion.Angle(beforeTurn,player.transform.rotation)<.1f,"Closure turn snapped immediately");
        Assert(player.GetComponent<WorldSpeechBubble>().CurrentText.EndsWith("！"),"Missing owner announcement");
        Assert(player.MovementInputEnabled&&player.GetComponent<PlayerInteractionController>().enabled,"Temporary closure locks player input");
        var tables=UnityEngine.Object.FindAnyObjectByType<SeatRegistry>().Tables.Where(t=>t!=null&&t.isActiveAndEnabled).ToArray();
        var center=tables.Aggregate(Vector3.zero,(sum,t)=>sum+t.transform.position)/tables.Length;
        yield return new WaitForSeconds(.5f);
        Assert(Vector3.Dot(player.transform.forward,Vector3.ProjectOnPlane(center-player.transform.position,Vector3.up).normalized)>.98f,"Closure announcement does not face the hall");
        Vector3 beforeMove=player.transform.position;
        var moveKeys=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(moveKeys,new KeyboardState(Key.W));
        yield return new WaitForSeconds(.2f);
        InputSystem.QueueStateEvent(moveKeys,new KeyboardState());yield return null;InputSystem.RemoveDevice(moveKeys);
        Assert(Vector3.Distance(beforeMove,player.transform.position)>.1f,"Player cannot move during closure announcement");
        yield return new WaitForSeconds(1.2f);
        Assert(departureFrames.Count==customers.Length&&departureFrames.Distinct().Count()==1,"Guests did not start departing together");
        foreach(var c in customers.Where(c=>c!=null))
            Assert(c.GetComponent<WorldSpeechBubble>().CurrentText.All(ch=>"@#$%&*!?".Contains(ch))&&c.GetComponent<WorldSpeechBubble>().CurrentText.Length>=5,"Missing randomized complaint");
        yield return Capture("/tmp/temporary-closing.png");
        float exitDeadline=Time.time+40;
        while(lever.IsSwitching&&Time.time<exitDeadline){Assert(!orbit.EntranceFraming,"Temporary close detached camera");yield return null;}
        Assert(!lever.IsSwitching&&day.ActiveCustomers==0,"Guests stuck during temporary closure");
        Assert(menu.Balance==balance&&menu.PendingOrderCount==0&&day.WaitingCustomers==waiting,"Temporary closure charged/cancelled future arrivals");
        Log("PASS temporary closure: owner announcement, simultaneous departures, randomized complaints, no camera cut, no unpaid revenue, faces hall and movement remains available");
        yield return CheckUi(player,orbit,menu,narrative);
    }
    static void CheckDialogueCamera(PrototypeCameraOrbit orbit)
    {
        var choose=typeof(PrototypeCameraOrbit).GetMethod("ChooseDialogueYaw",BindingFlags.Instance|BindingFlags.NonPublic);
        var left=GameObject.CreatePrimitive(PrimitiveType.Capsule);var right=GameObject.CreatePrimitive(PrimitiveType.Capsule);
        left.transform.position=new Vector3(500,0,500);right.transform.position=new Vector3(502,0,500);
        var near=GameObject.CreatePrimitive(PrimitiveType.Cube);near.transform.position=new Vector3(501,5,496);near.transform.localScale=new Vector3(10,20,.5f);
        var far=GameObject.CreatePrimitive(PrimitiveType.Cube);far.transform.position=new Vector3(501,5,504);far.transform.localScale=near.transform.localScale;
        Quaternion original=orbit.transform.rotation;orbit.transform.rotation=Quaternion.Euler(0,40,0);
        float Pick()=>(float)choose.Invoke(orbit,new object[]{left.transform,right.transform});
        near.SetActive(false);far.SetActive(false);Physics.SyncTransforms();
        Assert(Mathf.Abs(Mathf.DeltaAngle(Pick(),0))<.01f,"Unblocked camera did not choose nearer perpendicular");
        near.SetActive(true);Physics.SyncTransforms();Assert(Mathf.Abs(Mathf.DeltaAngle(Pick(),180))<.01f,"Blocked nearer view did not choose opposite");
        far.SetActive(true);Physics.SyncTransforms();Assert(Mathf.Abs(Mathf.DeltaAngle(Pick(),0))<.01f,"Both blocked should choose shorter rotation");
        // Both views blocked: a one-person obstruction must beat a wall hiding both people.
        far.transform.position=new Vector3(500,5,504);far.transform.localScale=new Vector3(.6f,20,.5f);
        Physics.SyncTransforms();Assert(Mathf.Abs(Mathf.DeltaAngle(Pick(),180))<.01f,"Both blocked: lower occlusion count did not win");
        far.SetActive(false);
        near.GetComponent<Renderer>().enabled=false;
        Physics.SyncTransforms();Assert(Mathf.Abs(Mathf.DeltaAngle(Pick(),180))<.01f,"Collision-only wall proxy was ignored");
        near.GetComponent<Renderer>().enabled=true;
        // A narrow wall strip intersects only one body row, below the old 0.6 m cutoff.
        near.transform.position=new Vector3(501,3.7f,497);near.transform.localScale=new Vector3(10,.25f,.1f);
        Physics.SyncTransforms();Assert(Mathf.Abs(Mathf.DeltaAngle(Pick(),180))<.01f,"Single torso obstruction was ignored");
        near.SetActive(false);
        orbit.transform.rotation=original;
        foreach(var go in new[]{left,right,near,far}){go.SetActive(false);UnityEngine.Object.Destroy(go);}
        Log("PASS two perpendicular dialogue angles: clear/one blocked/both blocked; shortest-turn fallback");
    }
    public static IEnumerator CheckRealDialogueCamera(PrototypePlayerMover player,PrototypeCameraOrbit orbit)
    {
        CheckDialogueCamera(orbit);
        player.MovementInputEnabled=false;player.enabled=false;
        var eve=UnityEngine.Object.FindAnyObjectByType<Day1EveActor>(FindObjectsInactive.Include);eve.gameObject.SetActive(true);
        eve.StopAllCoroutines();eve.enabled=false;
        var nav=eve.GetComponent<NpcNavigator>();if(nav){nav.Stop();nav.enabled=false;}
        var agent=eve.GetComponent<NavMeshAgent>();if(agent)agent.enabled=false;
        var choose=typeof(PrototypeCameraOrbit).GetMethod("ChooseDialogueYaw",BindingFlags.Instance|BindingFlags.NonPublic);
        var count=typeof(PrototypeCameraOrbit).GetMethod("CountCandidateBlockers",BindingFlags.Instance|BindingFlags.NonPublic);
        // Exercise real authored walls with real animated character bounds, not capsule stand-ins.
        var walls=UnityEngine.Object.FindObjectsByType<BoxCollider>()
            .Where(c=>c.enabled&&!c.isTrigger&&c.GetComponent<EditableHorizontalWallMiter>()!=null&&c.bounds.size.y>2).ToArray();
        int checkedPairs=0,oneSideClear=0,bothBlocked=0;bool captured=false;
        foreach(var wall in walls)
        {
            var center=wall.transform.TransformPoint(wall.center);center.y=0;
            var tangent=wall.transform.right;var normal=wall.transform.forward;
            for(int side=-1;side<=1;side+=2)
            {
                player.GetComponent<CharacterController>().enabled=false;
                player.transform.position=center+normal*side*.85f-tangent*.65f;
                eve.transform.position=center+normal*side*.85f+tangent*.65f;
                Physics.SyncTransforms();yield return null;
                orbit.transform.rotation=Quaternion.Euler(0,45,0);
                float yaw=(float)choose.Invoke(orbit,new object[]{player.transform,eve.transform});
                int selected=(int)count.Invoke(orbit,new object[]{yaw,player.transform,eve.transform});
                int other=(int)count.Invoke(orbit,new object[]{yaw+180,player.transform,eve.transform});
                Assert(selected<=other,"Real wall chose more obstructed side: "+wall.name);
                checkedPairs++;if(selected==0&&other>0)oneSideClear++;if(selected>0&&other>0)bothBlocked++;
                if(selected==0&&other>0&&!captured
                    &&NavMesh.SamplePosition(player.transform.position,out _,.3f,NavMesh.AllAreas)
                    &&NavMesh.SamplePosition(eve.transform.position,out _,.3f,NavMesh.AllAreas))
                {
                    captured=true;
                    var camera=(Camera)Get(orbit,"gameCamera");
                    var blocked=typeof(PrototypeCameraOrbit).GetMethod("CharacterBlocked",BindingFlags.Static|BindingFlags.NonPublic);
                    orbit.BeginDialogueFraming(player.transform,eve.transform);
                    int hiddenFrames=0,totalFrames=0;float until=Time.time+1.5f;
                    while(Time.time<until)
                    {
                        yield return null;totalFrames++;
                        int hidden=(int)blocked.Invoke(null,new object[]{camera.transform.position,camera.transform.forward,player.transform,player.transform,eve.transform})
                            +(int)blocked.Invoke(null,new object[]{camera.transform.position,camera.transform.forward,eve.transform,player.transform,eve.transform});
                        if(hidden>0)hiddenFrames++;
                    }
                    Log($"TRANSITION real wall: {hiddenFrames}/{totalFrames} frames have occlusion, final candidate clear");
                    yield return Capture("/tmp/dialogue-real-wall.png");
                    orbit.EndDialogueFraming();
                }
            }
        }
        player.transform.position=new Vector3(48.4750023f,.07999992f,17.0015678f);
        eve.transform.position=new Vector3(46.4133453f,.01999986f,18.0400677f);
        player.transform.rotation=Quaternion.Euler(0,296.746f,0);
        eve.transform.rotation=Quaternion.Euler(0,116.746f,0);
        orbit.transform.rotation=Quaternion.Euler(0,315,0);
        Physics.SyncTransforms();yield return null;
        int front=(int)count.Invoke(orbit,new object[]{26.74599f,player.transform,eve.transform});
        int back=(int)count.Invoke(orbit,new object[]{206.74599f,player.transform,eve.transform});
        float chosen=(float)choose.Invoke(orbit,new object[]{player.transform,eve.transform});
        Assert(back<front&&Mathf.Abs(Mathf.DeltaAngle(chosen,206.74599f))<.1f,"Reported corner did not choose less obstructed perpendicular view");
        Log($"PASS reported corner: 26.75 degrees {front}/18 blocked; 206.75 degrees {back}/18 blocked; selected {chosen}");
        Assert(checkedPairs>0&&oneSideClear>0,"No real-wall clear/opposite-blocked cases exercised");
        Log($"PASS real authored walls / real characters: {checkedPairs} pairs, {oneSideClear} clear-vs-blocked, {bothBlocked} both-blocked fallback");
    }
    public static IEnumerator CheckWallCutout(PrototypePlayerMover player,PrototypeCameraOrbit orbit)
    {
        foreach(var trigger in UnityEngine.Object.FindObjectsByType<AutomaticDoorTrigger>())trigger.enabled=false;
        foreach(var door in UnityEngine.Object.FindObjectsByType<DoorStateController>())if(door.name.StartsWith("Door_Small_Stone"))door.Close();
        player.MovementInputEnabled=false;player.enabled=false;player.GetComponent<CharacterController>().enabled=false;
        var eve=UnityEngine.Object.FindAnyObjectByType<Day1EveActor>(FindObjectsInactive.Include);eve.gameObject.SetActive(true);eve.enabled=false;eve.StopAllCoroutines();
        var nav=eve.GetComponent<NpcNavigator>();if(nav){nav.Stop();nav.enabled=false;}
        var agent=eve.GetComponent<NavMeshAgent>();if(agent)agent.enabled=false;
        player.transform.position=new Vector3(48.4750023f,.07999992f,17.0015678f);
        eve.transform.position=new Vector3(46.4133453f,.01999986f,18.0400677f);
        player.transform.rotation=Quaternion.Euler(0,296.746f,0);eve.transform.rotation=Quaternion.Euler(0,116.746f,0);
        Physics.SyncTransforms();
        var effect=orbit.GetComponent<DialogueOcclusionFader>();
        var walls=UnityEngine.Object.FindObjectsByType<MeshRenderer>().Where(r=>DialogueOcclusionFader.IsWall(r.transform)).ToArray();
        Assert(walls.Length>0,"No cuttable walls found");
        effect.enabled=false;
        var originals=walls.ToDictionary(r=>r,r=>r.sharedMaterials);
        var leaves=UnityEngine.Object.FindObjectsByType<MeshRenderer>().Where(r=>r.name=="DoorLeaf"||r.name.EndsWith("Door_Leaf")).ToArray();
        var smallLeaves=leaves.Where(r=>r.GetComponentInParent<DoorStateController>()?.name.StartsWith("Door_Small_Stone")==true).ToArray();
        Assert(smallLeaves.All(r=>!DialogueOcclusionFader.IsWall(r.transform)),"Small door leaves must remain opaque");
        var leafMaterials=leaves.ToDictionary(r=>r,r=>r.sharedMaterials);
        orbit.BeginDialogueFraming(player.transform,eve.transform);
        yield return new WaitForSeconds(1.5f);
        foreach(float yaw in new[]{26.74599f,206.74599f})
        {
            Set(orbit,"dialogueTargetYaw",yaw);
            yield return new WaitForSeconds(.15f);
            yield return Capture($"/tmp/wall-cutout-before-{(int)yaw}.png");
            effect.enabled=true;yield return new WaitForSeconds(.2f);
            Assert(Shader.GetGlobalFloat("_TavernCutTransition0")<.8f,"Cutout opened abruptly");
            yield return new WaitForSeconds(.7f);
            foreach(var mask in (System.Array)Get(effect,"masks"))
            {
                var mt=mask.GetType();
                Log($"MASK bounds={mt.GetField("bounds").GetValue(mask)} radius={mt.GetField("coverageRadius").GetValue(mask)} blocked={mt.GetField("blockedSamples").GetValue(mask)}");
            }
            Assert(Shader.GetGlobalVector("_TavernCutSphere0").w>1,"Player cutout did not open at the reported corner");
            Assert(((Vector3)Shader.GetGlobalVector("_TavernCutSphere0")-player.transform.position).sqrMagnitude<100,"Invalid wall contact sent to shader");
            Assert(Shader.GetGlobalFloat("_TavernCutTransition0")>.99f,"Cutout transition failed to finish");
            Assert(Shader.GetGlobalVector("_TavernCutSphere1").w>1 && Shader.GetGlobalFloat("_TavernCutPair")>.99f,"Dialogue silhouettes not joined");
            Assert(walls.All(r=>r.enabled),"Effect hid a complete wall renderer");
            foreach(var leaf in smallLeaves){var props=new MaterialPropertyBlock();leaf.GetPropertyBlock(props,0);Assert(props.GetFloat("_TavernCuttable")==0,"Small door must share cutout without inferred volume filling");}
            var sample=walls.First(r=>r.name=="Wall_Horizontal_06_3");
            var pb=new MaterialPropertyBlock();sample.GetPropertyBlock(pb,0);
            for(int face=2;face<=3;face++){var end=new MaterialPropertyBlock();sample.GetPropertyBlock(end,face);Assert(end.GetColor("_BaseColor").a>.99f,"Wall end face is transparent");}
            Log($"WALL shader={sample.sharedMaterials[0].shader.name} cuttable={pb.GetFloat("_TavernCuttable")} camera={Shader.GetGlobalVector("_TavernCutCamera")} actual={((Camera)Get(orbit,"gameCamera")).transform.position}");
            Assert(pb.GetTexture("_SectionMap")!=null,"Section must reuse authored wall texture");
            foreach(var r in walls){var props=new MaterialPropertyBlock();r.GetPropertyBlock(props,0);Assert(props.GetFloat("_TavernCuttable")==1,"Wall registration missing: "+r.name);}
            Set(effect,"enableSections",false);yield return null;
            yield return Capture($"/tmp/wall-section-off-{(int)yaw}.png");
            Set(effect,"enableSections",true);yield return null;
            yield return Capture($"/tmp/wall-cutout-after-{(int)yaw}.png");
            Log($"PASS cutout yaw {yaw}: sphere0={Shader.GetGlobalVector("_TavernCutSphere0")} sphere1={Shader.GetGlobalVector("_TavernCutSphere1")}");
            if (yaw > 180)
            {
                // Test-only camera sweep around the reported corner; production yaw rules stay untouched.
                foreach(float offset in new[]{-12f,-6f,0f,6f,12f})
                {
                    Set(orbit,"dialogueTargetYaw",yaw+offset);yield return new WaitForSeconds(.15f);
                    yield return Capture($"/tmp/wall-section-sweep-{(int)(offset+12)}.png");
                    Assert(walls.All(r=>r.enabled),"Camera sweep hid a complete wall");
                }
                Set(orbit,"dialogueTargetYaw",yaw);yield return null;
            }
            effect.enabled=false;
            Assert(Shader.GetGlobalVector("_TavernCutSphere0").w==0,"Cutout globals survived disable");
            Assert(walls.All(r=>r.sharedMaterials.SequenceEqual(originals[r])),"Authored wall materials not restored");
        }
        orbit.EndDialogueFraming();effect.enabled=true;orbit.FollowTarget=player.transform;
        orbit.transform.rotation=Quaternion.Euler(0,45,0);Set(orbit,"targetYaw",45f);
        yield return new WaitForSeconds(1.5f);
        Assert(Shader.GetGlobalVector("_TavernCutSphere0").w>1,"Exploration player occlusion did not open");
        Assert(Shader.GetGlobalVector("_TavernCutSphere1").w==0 && Shader.GetGlobalFloat("_TavernCutPair")==0,"NPC cutout survived dialogue exit");
        yield return new WaitForSeconds(.7f);
        var stableSphere=Shader.GetGlobalVector("_TavernCutSphere0");
        var animator=player.GetComponentInChildren<Animator>();animator.SetFloat("Speed",2);animator.Play("Walk",0,.4f);
        yield return new WaitForSeconds(.4f);animator.SetFloat("Speed",0);animator.Play("Idle",0,0);
        yield return new WaitForSeconds(.4f);
        Assert(Vector4.Distance(stableSphere,Shader.GetGlobalVector("_TavernCutSphere0"))<.015f,"Idle/walk pose pumps the cutout");
        Log("PASS locomotion animation does not change settled cutout dimensions");
        yield return Capture("/tmp/wall-cutout-exploration.png");
        player.transform.position=new Vector3(30,0,14);orbit.FollowTarget=player.transform;
        yield return new WaitForSeconds(1);
        Assert(Shader.GetGlobalVector("_TavernCutSphere0").w<.01f,"Cutout remained in clear hall");
        Log("PASS local sphere opens at both real corner views, all wall objects remain enabled, materials restored, clear hall closes cutout");
        // Reported open small-door edge: player is outside the old proximity box.
        player.transform.position=new Vector3(46.28782f,.07999992f,15.25949f);
        player.transform.rotation=Quaternion.Euler(0,48.412f,0);
        orbit.transform.rotation=Quaternion.Euler(0,225,0);Set(orbit,"targetYaw",225f);Physics.SyncTransforms();
        var reportedDoor=UnityEngine.Object.FindObjectsByType<DoorStateController>().Single(d=>d.name=="Door_Small_Stone_3");
        reportedDoor.Close();yield return new WaitForSeconds(.8f);
        var doorGroup=reportedDoor.GetComponentInParent<WallCutoutGroup>();
        Assert(doorGroup && reportedDoor.GetComponentsInChildren<Renderer>().All(r=>r.GetComponentInParent<WallCutoutGroup>()==doorGroup),"Door/frame/leaf do not share their wall group");
        var closedDoorProps=new MaterialPropertyBlock();reportedDoor.GetComponentInChildren<Renderer>().GetPropertyBlock(closedDoorProps,0);
        Assert(closedDoorProps.GetFloat("_TavernCutGroup")>.99f,"Closed small door does not follow the occluding wall group");
        Assert(Shader.GetGlobalFloat("_TavernCutTransition0")>.9f,"Reported closed door should participate in wall dissolve");
        foreach(bool open in new[]{true,false})
        {
            reportedDoor.SetOpen(open);
            float until=Time.time+1;
            while(Time.time<until)
            {
                yield return null;
                Assert(Shader.GetGlobalFloat("_TavernCutTransition0")>.9f,"Door state interrupted wall dissolve");
                foreach(var leaf in smallLeaves)
                {
                    var props=new MaterialPropertyBlock();leaf.GetPropertyBlock(props,0);
                    Assert(props.GetFloat("_TavernCuttable")==0 && leaf.sharedMaterials.SequenceEqual(leafMaterials[leaf]),"Moving leaf acquired cutout material");
                }
            }
            yield return Capture(open?"/tmp/small-door-open-independent.png":"/tmp/small-door-closed-independent.png");
        }
        Log("PASS opening, open, closing and closed door preserve wall dissolve and opaque leaves");
        // A narrow doorway must activate the sphere even with a clear centre ray.
        var fixture=new GameObject("Walls");
        var jambs=new List<GameObject>();
        foreach(float side in new[]{-1f,1f}){
            var jamb=GameObject.CreatePrimitive(PrimitiveType.Cube);jamb.transform.SetParent(fixture.transform);
            jamb.transform.position=new Vector3(1000+side*.3f,1.5f,999.6f);jamb.transform.localScale=new Vector3(.2f,3,.25f);jamb.AddComponent<WallCutoutGroup>();jambs.Add(jamb);
        }
        player.transform.position=new Vector3(1000,0,1000);orbit.transform.rotation=Quaternion.identity;Set(orbit,"targetYaw",0f);Physics.SyncTransforms();
        yield return new WaitForSeconds(1.2f);
        Assert(Shader.GetGlobalVector("_TavernCutSphere0").w>1,"Narrow doorway sphere probe failed");
        foreach(var jamb in jambs)jamb.SetActive(false);
        var rear=GameObject.CreatePrimitive(PrimitiveType.Cube);rear.transform.SetParent(fixture.transform);rear.transform.position=new Vector3(1000,1.5f,1000.8f);rear.transform.localScale=new Vector3(5,3,.25f);rear.AddComponent<WallCutoutGroup>();Physics.SyncTransforms();
        yield return new WaitForSeconds(1);
        Assert(Shader.GetGlobalVector("_TavernCutSphere0").w<.01f,"Wall behind actor incorrectly triggers cutout");
        rear.SetActive(false);
        var low=GameObject.CreatePrimitive(PrimitiveType.Cube);low.transform.SetParent(fixture.transform);low.transform.position=new Vector3(1000,.55f,999.6f);low.transform.localScale=new Vector3(4,1.1f,.25f);low.AddComponent<WallCutoutGroup>();Physics.SyncTransforms();
        yield return new WaitForSeconds(1);Assert(Shader.GetGlobalVector("_TavernCutSphere0").w>1,"Low wall hiding legs fails to open");
        // A rear-only trigger test misses the bug: activate a foreground cut while
        // a coloured rear wall is present, then compare actual rendered pixels.
        rear.SetActive(true);
        var rearMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));rearMaterial.SetColor("_BaseColor",Color.red);
        var frontMaterial=new Material(rearMaterial);frontMaterial.SetColor("_BaseColor",Color.blue);
        effect.enabled=false;
        rear.GetComponent<Renderer>().sharedMaterial=rearMaterial;low.GetComponent<Renderer>().sharedMaterial=frontMaterial;
        effect.enabled=true;Physics.SyncTransforms();yield return new WaitForSeconds(1.2f);
        Assert(Shader.GetGlobalFloat("_TavernCutTransition0")>.99f,"Concurrent front/rear test has no active cut");
        var frontRenderer=low.GetComponent<Renderer>();var rearRenderer=rear.GetComponent<Renderer>();
        var testBlock=new MaterialPropertyBlock();
        rearRenderer.GetPropertyBlock(testBlock,0);Assert(testBlock.GetFloat("_TavernCutGroup")==0,"Unhit rear wall group became eligible");
        frontRenderer.GetPropertyBlock(testBlock,0);Assert(testBlock.GetFloat("_TavernCutGroup")>.99f,"Hit foreground wall group is not eligible");
        // A child mesh without a collider still follows its wall group's probe hits.
        var unhitPiece=GameObject.CreatePrimitive(PrimitiveType.Cube);unhitPiece.name="UnhitGroupMember";
        unhitPiece.transform.SetParent(low.transform,false);unhitPiece.transform.localPosition=Vector3.right*.3f;
        unhitPiece.transform.localScale=Vector3.one*.05f;UnityEngine.Object.Destroy(unhitPiece.GetComponent<Collider>());
        unhitPiece.GetComponent<Renderer>().sharedMaterial=frontMaterial;
        yield return new WaitForSeconds(1.1f);
        unhitPiece.GetComponent<Renderer>().GetPropertyBlock(testBlock,0);
        Assert(testBlock.GetFloat("_TavernCutGroup")>.99f,"Unhit member does not follow the parent wall group");
        frontRenderer.GetPropertyBlock(testBlock,0);testBlock.SetFloat("_TavernCuttable",0);frontRenderer.SetPropertyBlock(testBlock,0);
        rearRenderer.GetPropertyBlock(testBlock,0);testBlock.SetFloat("_TavernCuttable",0);rearRenderer.SetPropertyBlock(testBlock,0);
        yield return new WaitForEndOfFrame();var opaquePixels=CutoutPixelCounts();
        frontRenderer.GetPropertyBlock(testBlock,0);testBlock.SetFloat("_TavernCuttable",1);frontRenderer.SetPropertyBlock(testBlock,0);
        rearRenderer.GetPropertyBlock(testBlock,0);testBlock.SetFloat("_TavernCuttable",1);rearRenderer.SetPropertyBlock(testBlock,0);
        yield return null;yield return new WaitForEndOfFrame();var cutPixels=CutoutPixelCounts();
        Assert(opaquePixels.x>100&&opaquePixels.y>100,"Coloured wall test is not visible");
        Assert(cutPixels.x>=opaquePixels.x*.99f,"Rear wall loses pixels when foreground cut is active");
        Assert(cutPixels.y<opaquePixels.y*.85f,"Foreground wall no longer dissolves");
        Log($"PASS rendered front/rear walls: rear red {opaquePixels.x}->{cutPixels.x}, front blue {opaquePixels.y}->{cutPixels.y}");
        yield return Capture("/tmp/wall-cutout-front-rear.png");
        rear.SetActive(false);
        low.transform.position=new Vector3(1000,.55f,998.5f);Physics.SyncTransforms();
        yield return new WaitForSeconds(.5f);Assert(Shader.GetGlobalVector("_TavernCutSphere0").w<.01f,"Clear feet trigger low wall too early");
        low.transform.position=new Vector3(1000,.55f,999.1f);Physics.SyncTransforms();
        yield return new WaitForSeconds(.5f);Assert(Shader.GetGlobalVector("_TavernCutSphere0").w>1,"Feet just occluded must activate cutout");
        orbit.transform.rotation=Quaternion.Euler(0,90,0);Set(orbit,"targetYaw",90f);
        yield return new WaitForSeconds(.5f);Assert(Shader.GetGlobalVector("_TavernCutSphere0").w<.01f,"Side view must clear low wall");
        low.transform.position=new Vector3(1000,.55f,999.4f);Physics.SyncTransforms();
        orbit.RotateLeft();bool openedDuringRotation=false;
        while(orbit.IsRotating){yield return null;if(orbit.IsRotating&&Shader.GetGlobalFloat("_TavernCutTransition0")>0)openedDuringRotation=true;}
        Assert(openedDuringRotation,"Q/E cutout starts only after the camera finishes rotating");
        yield return new WaitForSeconds(.4f);
        Log("PASS clear feet stay opaque; first foot occlusion opens; Q/E opens during rotation");
        // Adjacent wall enters the existing cut before the character reaches its edge.
        orbit.transform.rotation=Quaternion.identity;Set(orbit,"targetYaw",0f);
        low.transform.position=new Vector3(1000,.55f,999.4f);
        player.transform.position=new Vector3(1001.25f,0,1000);Physics.SyncTransforms();
        var nextWall=GameObject.CreatePrimitive(PrimitiveType.Cube);nextWall.transform.SetParent(fixture.transform);
        nextWall.transform.position=new Vector3(1002.5f,.55f,999.4f);nextWall.transform.localScale=new Vector3(1,1.1f,.25f);
        nextWall.AddComponent<WallCutoutGroup>();Physics.SyncTransforms();
        yield return new WaitForSeconds(1.3f);
        var nextProps=new MaterialPropertyBlock();var nextRenderer=nextWall.GetComponent<Renderer>();
        nextRenderer.GetPropertyBlock(nextProps,0);
        Assert(nextProps.GetFloat("_TavernCutGroup")==0,"Stationary actor activates adjacent wall prematurely");
        bool predicted=false;
        while(player.transform.position.x<1001.7f)
        {
            player.transform.position+=Vector3.right*(Time.deltaTime*1.5f);Physics.SyncTransforms();yield return null;
            nextRenderer.GetPropertyBlock(nextProps,0);
            if(nextProps.GetFloat("_TavernCutGroup")>0)predicted=true;
            Assert(Shader.GetGlobalFloat("_TavernCutTransition0")>.99f,"Wall handoff restarts the shared opening");
        }
        Assert(predicted,"Approaching adjacent wall did not join before actual occlusion");
        Log("PASS adjacent wall prediction preserves fully opened shared silhouette");
        UnityEngine.Object.Destroy(fixture);
        Log("PASS sphere opens narrow doorway with clear centre, rear wall does not activate it");
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("SealRoom_B1",UnityEngine.SceneManagement.LoadSceneMode.Additive);
        var basement=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SealRoom_B1");
        var hostEnvironment=GameObject.Find("Tavern_Main/Environment");
        Assert(hostEnvironment,"Main environment root missing");hostEnvironment.SetActive(false);
        var stone=UnityEngine.Object.FindObjectsByType<MeshRenderer>().First(r=>r.gameObject.scene==basement&&r.name=="South_01_RubbleWall_2m");
        yield return new WaitForSeconds(1.2f);
        Assert(stone.sharedMaterials.Any(m=>m&&m.shader.name=="DungeonTavern/Wall Cutout Unlit"),"B1 stone wall shader not registered");
        var collider=stone.GetComponent<Collider>();
        Assert(collider,"B1 test wall has no collider");
        Vector3 outward=stone.bounds.size.x>stone.bounds.size.z?Vector3.forward:Vector3.right;
        if(Vector3.Dot(outward,new Vector3(41,0,18)-stone.bounds.center)>0)outward=-outward;
        Vector3 basePoint=stone.bounds.center;basePoint.y=stone.bounds.min.y+.1f;
        player.transform.position=basePoint-outward*.8f;
        eve.transform.position=player.transform.position+Vector3.Cross(Vector3.up,outward)*1.5f;
        orbit.BeginDialogueFraming(player.transform,eve.transform);
        Set(orbit,"dialogueTargetYaw",Mathf.Atan2(-outward.x,-outward.z)*Mathf.Rad2Deg);
        Physics.SyncTransforms();yield return new WaitForSeconds(2);
        Assert(Shader.GetGlobalVector("_TavernCutSphere0").w>1,"B1 wall cutout failed to activate");
        var stairs=UnityEngine.Object.FindObjectsByType<MeshRenderer>().Where(r=>r.gameObject.scene==basement && r.name.StartsWith("Stair_")).ToArray();
        Assert(stairs.All(r=>!DialogueOcclusionFader.IsWall(r.transform)),"Stairs incorrectly classified as walls");
        effect.enabled=false;yield return null;yield return Capture("/tmp/wall-cutout-before-b1.png");
        effect.enabled=true;yield return new WaitForSeconds(.7f);
        Set(effect,"enableSections",false);yield return null;yield return Capture("/tmp/wall-section-off-b1.png");
        Set(effect,"enableSections",true);yield return null;
        yield return Capture("/tmp/wall-cutout-b1.png");
        var gate=UnityEngine.Object.FindObjectsByType<MeshRenderer>().First(r=>r.gameObject.scene==basement&&r.name=="StoneSlab");
        Assert(DialogueOcclusionFader.IsWall(gate.transform) && gate.sharedMaterials.All(m=>m.shader.name=="DungeonTavern/Wall Cutout Unlit"),"B1 gate leaf missing cutout");
        var gatePosition=gate.transform.position;gate.transform.position+=Vector3.up*.3f;
        yield return null;yield return null;
        var gateProps=new MaterialPropertyBlock();gate.GetPropertyBlock(gateProps,0);
        Assert(gateProps.GetMatrix("_TavernWallWorldToLocal")==gate.transform.worldToLocalMatrix,"Moving gate section transform stale");
        gate.transform.position=gatePosition;
        orbit.EndDialogueFraming();orbit.FollowTarget=player.transform;
        player.transform.position=gate.bounds.center+Vector3.left*1.2f;player.transform.position=new Vector3(player.transform.position.x,.08f,player.transform.position.z);
        orbit.transform.rotation=Quaternion.Euler(0,270,0);Set(orbit,"targetYaw",270f);Physics.SyncTransforms();
        yield return new WaitForSeconds(1.2f);yield return Capture("/tmp/wall-cutout-stone-gate.png");
        Assert(Shader.GetGlobalVector("_TavernCutSphere0").w>1,"Stone gate does not activate cutout");
        Log("PASS B1 gate leaf participates and its section transform follows movement");
        var connection=UnityEngine.Object.FindObjectsByType<MeshRenderer>().Single(r=>r.gameObject.scene==basement&&r.name=="StairArchConnection_Trial");
        Assert(!DialogueOcclusionFader.IsWall(connection.transform)&&!connection.GetComponent<Collider>(),"Stair connection must stay opaque and leave passage collision unchanged");
        orbit.EndDialogueFraming();orbit.FollowTarget=player.transform;
        // Isolate additive B1 presentation; these test-only render changes end with Play Mode.
        foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>())
            if(r.gameObject.scene!=basement&&!r.transform.IsChildOf(player.transform))r.enabled=false;
        player.transform.position=new Vector3(48.1432f,.08f,25.0443f);
        player.transform.rotation=Quaternion.Euler(0,-101.293f,0);
        orbit.transform.rotation=Quaternion.Euler(0,315,0);Set(orbit,"targetYaw",315f);Physics.SyncTransforms();
        yield return new WaitForSeconds(1.2f);yield return Capture("/tmp/rear-wall-fixed-b1.png");
        player.transform.position=new Vector3(36.3f,.08f,23.0f);Physics.SyncTransforms();
        orbit.enabled=false;orbit.transform.position=new Vector3(34.2f,1.2f,23.0f);
        ((Camera)Get(orbit,"gameCamera")).orthographicSize=5.4f;
        foreach(float yaw in new[]{45f,135f,315f}){
            orbit.transform.rotation=Quaternion.Euler(0,yaw,0);Set(orbit,"targetYaw",yaw);
            yield return new WaitForSeconds(1.2f);yield return Capture($"/tmp/stair-connection-play-{yaw}.png");
        }
        Log("PASS stair connection remains opaque, three runtime views captured");

        effect.enabled=false;
        Assert(stone.sharedMaterials.All(m=>!m||m.shader.name!="DungeonTavern/Wall Cutout Unlit"),"B1 original stone materials not restored");
        Log("PASS B1 stone wall registration, local mask activation, original material restore");
    }
    public static void ClickUi(string name)
    {
        var button=DungeonTavern.UI.TavernUI.Instance.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name);
        var events=UnityEngine.EventSystems.EventSystem.current;
        var pointer=new UnityEngine.EventSystems.PointerEventData(events){position=RectTransformUtility.WorldToScreenPoint(null,button.transform.TransformPoint(((RectTransform)button.transform).rect.center))};
        var hits=new List<UnityEngine.EventSystems.RaycastResult>();events.RaycastAll(pointer,hits);
        Assert(hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==button,"UI obscured/not clickable: "+name);
        UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
    }
    static Vector2Int CutoutPixelCounts()
    {
        var image=ScreenCapture.CaptureScreenshotAsTexture();var count=Vector2Int.zero;
        foreach(var pixel in image.GetPixels32())
        {
            if(pixel.r>80 && pixel.r>pixel.g*2 && pixel.r>pixel.b*2)count.x++;
            if(pixel.b>80 && pixel.b>pixel.g*2 && pixel.b>pixel.r*2)count.y++;
        }
        UnityEngine.Object.Destroy(image);return count;
    }
    static IEnumerator Capture(string path)
    {
        ScreenCapture.CaptureScreenshot(path);
        yield return new WaitForEndOfFrame();
        yield return null;
    }
    public static IEnumerator CheckUi(PrototypePlayerMover player,PrototypeCameraOrbit orbit,TavernMenuSystem menu,Day1NarrativeController narrative)
    {
        var ui=DungeonTavern.UI.TavernUI.Instance;
        Assert(ui!=null,"Missing UI root");
        var canvases=ui.GetComponentsInChildren<Canvas>(true);
        Assert(canvases.Length==8&&canvases.Select(c=>c.sortingOrder).Distinct().Count()==8,"UI layers missing or ambiguous");
        Assert(canvases.All(c=>c.gameObject.layer==5&&c.renderMode==RenderMode.ScreenSpaceOverlay),"UI not isolated on overlay layer");
        Assert(DungeonTavern.UI.TavernUiTheme.Font!=null&&DungeonTavern.UI.TavernUiTheme.Font.HasCharacter('酒'),"Packaged Chinese font unavailable");
        ScreenCapture.CaptureScreenshot("/tmp/tavern-ui-hud.png");yield return new WaitForEndOfFrame();yield return null;
        var pause=UnityEngine.Object.FindAnyObjectByType<GamePauseMenu>();
        yield return new WaitForSeconds(.6f);
        ClickUi("PauseButton");Assert(GamePauseMenu.IsPaused,"Pause button did not respond");
        float time=Time.time;var position=player.transform.position;var rotation=orbit.transform.rotation;
        yield return new WaitForSecondsRealtime(.2f);yield return Capture("/tmp/pause-menu.png");
        yield return new WaitForSecondsRealtime(.4f);
        Assert(Time.time==time&&player.transform.position==position&&orbit.transform.rotation==rotation&&!player.GetComponent<PlayerInteractionController>().TryInteract(),"Pause did not freeze world/input");
        ClickUi("Option4");yield return null;Assert(pause.HistoryVisible,"History menu did not open");yield return Capture("/tmp/ui-history.png");ClickUi("HistoryBack");yield return null;
        ClickUi("Option2");Assert(pause.ControlsVisible,"Controls button did not respond");yield return new WaitForSecondsRealtime(.2f);yield return Capture("/tmp/pause-controls.png");
        ClickUi("Back");yield return null;ClickUi("Option3");Assert(pause.ConfirmingQuit,"Quit confirmation did not open");yield return new WaitForSecondsRealtime(.2f);yield return Capture("/tmp/pause-quit.png");
        yield return new WaitForSecondsRealtime(.2f);ClickUi("Cancel");yield return null;ClickUi("Resume");Assert(Time.timeScale>0,"Pause did not restore time");
        Set(menu,"isOpen",true);Set(menu,"selectedTab",0);yield return new WaitForSeconds(.2f);yield return Capture("/tmp/menu-summary.png");
        ClickUi("OrdersTab");Assert(menu.SelectedTab==1,"Orders tab did not respond");yield return new WaitForSeconds(.2f);yield return Capture("/tmp/menu-orders.png");
        yield return new WaitForSeconds(.2f);
        var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));
        yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;InputSystem.RemoveDevice(keyboard);
        Assert(!menu.IsOpen&&!GamePauseMenu.IsPaused,"Esc on ledger should close it without opening pause");
        // Verify live held-item / interaction views and both bubble forms.
        var body=player.GetComponent<CharacterController>();body.enabled=false;
        var lever=UnityEngine.Object.FindAnyObjectByType<FloorLeverPoint>();player.transform.position=lever.transform.position-Vector3.forward*.8f;
        Physics.SyncTransforms();body.enabled=true;
        var hands=player.GetComponent<PlayerHands>();hands.Clear();hands.TryHold(HeldItem.EmptyCup);
        var bubble=player.GetComponent<WorldSpeechBubble>();if(bubble==null)bubble=player.gameObject.AddComponent<WorldSpeechBubble>();
        bubble.Show("酒馆要临时关闭了，请各位先离开！");
        yield return new WaitForSeconds(.6f);yield return Capture("/tmp/ui-hud-interaction.png");
        Assert(ui.transform.Find("20_HUD/SafeArea/HeldItem").gameObject.activeInHierarchy,"Held item HUD hidden");
        Assert(ui.transform.Find("20_HUD/SafeArea/InteractionHint").gameObject.activeInHierarchy,"Interaction HUD hidden");
        var order=new CustomerOrder(new[]{new OrderRequest{item=HeldItem.TestDrink,quantity=2}},menu.FindDish);
        order.TryDeliver(HeldItem.TestDrink);bubble.ShowOrder(order,true);
        yield return new WaitForSeconds(.3f);yield return Capture("/tmp/ui-order-bubble.png");bubble.Hide();hands.Clear();
        narrative.enabled=true;menu.Toggle();
        var eve=(Day1EveActor)Get(narrative,"eve");
        eve.gameObject.SetActive(true);bool eveEnabled=eve.enabled;eve.enabled=false;eve.GetComponent<NpcNavigator>().Stop(true);
        yield return null;yield return null;
        var wallMaterials=UnityEngine.Object.FindObjectsByType<Renderer>().Where(r=>r.name.Contains("Wall")).ToDictionary(r=>r,r=>r.sharedMaterials);
        typeof(Day1NarrativeController).GetMethod("BeginCloseDialogue",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(narrative,new object[]{eve.transform});
        var finalView=Quaternion.Euler(0,(float)Get(orbit,"dialogueTargetYaw"),0);float initialTurn=Quaternion.Angle(orbit.transform.rotation,finalView);
        yield return new WaitForSeconds(.3f);
        if(initialTurn>10)Assert(Quaternion.Angle(orbit.transform.rotation,finalView)>initialTurn*.5f,"Dialogue camera rotates too quickly");
        CheckDialogueCamera(orbit);
        var story=(Ink.Runtime.Story)Get(narrative,"story");story.ChoosePathString("day01_eve_arrives");
        typeof(Day1NarrativeController).GetMethod("ShowNextContent",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(narrative,null);
        narrative.ContinueDialogue();narrative.ContinueDialogue();
        Assert(narrative.CurrentLine.Contains("这是你的钥匙")&&narrative.CurrentChoiceCount==0,"Single choice leaked into choice UI");
        narrative.ContinueDialogue();
        Assert(narrative.CurrentLine=="你：接过钥匙。"&&narrative.ChoiceTexts.Count==0,"Single action was not presented in dialogue");
        int actionCount=narrative.FullHistory.Count(t=>t=="你：接过钥匙。");
        narrative.ContinueDialogue();
        Assert(narrative.CurrentLine.Contains("你以前只交代过一句")&&narrative.CurrentChoiceCount>1,"Choices did not accompany preceding speech");
        Assert(narrative.FullHistory.Count(t=>t=="你：接过钥匙。")==actionCount,"Single action recorded twice");
        story.ChoosePathString("day01_eve_conversation");
        typeof(Day1NarrativeController).GetMethod("ShowNextContent",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(narrative,null);
        Assert(!menu.IsOpen,"Dialogue did not dismiss ledger");
        yield return new WaitForSeconds(1.5f);yield return Capture("/tmp/dialogue-speech.png");
        Assert(Vector3.Dot(player.transform.forward,Vector3.ProjectOnPlane(eve.transform.position-player.transform.position,Vector3.up).normalized)>.995f,"Player did not turn toward dialogue speaker");
        Vector3 pair=eve.transform.position-player.transform.position;pair.y=0;
        Vector3 cameraForward=Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up).normalized;
        Assert(Mathf.Abs(Vector3.Dot(pair.normalized,cameraForward))<.025f,"Live dialogue view not perpendicular to the stopped pair");
        foreach(var pairMaterials in wallMaterials)if(pairMaterials.Key)Assert(pairMaterials.Key.sharedMaterials.SequenceEqual(pairMaterials.Value),"Dialogue changed wall material: "+pairMaterials.Key.name);
        Log("PASS dialogue camera eases over 1.3 seconds, remains perpendicular, leaves all wall materials unchanged");
        if(narrative.CurrentChoiceCount==0) { ClickUi("Continue");yield return null; }
        for(int i=0;i<20&&narrative.CurrentChoiceCount==0;i++)narrative.ContinueDialogue();
        yield return new WaitForSeconds(.3f);yield return Capture("/tmp/dialogue-bottom.png");
        Assert(narrative.CurrentChoiceCount>0,"Expected conversation choices");
        int choiceCount=narrative.CurrentChoiceCount;string choiceLine=narrative.CurrentLine;
        var choiceKeyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(choiceKeyboard,new KeyboardState(Key.Digit1));
        yield return null;yield return null;InputSystem.QueueStateEvent(choiceKeyboard,new KeyboardState());yield return null;InputSystem.RemoveDevice(choiceKeyboard);
        Assert(narrative.CurrentChoiceCount==choiceCount&&narrative.CurrentLine==choiceLine,"Digit key still chooses dialogue");
        foreach(var b in ui.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.name.StartsWith("Choice")))
        {
            var label=b.GetComponentInChildren<UnityEngine.UI.Text>();
            Assert(label.alignment==TextAnchor.MiddleCenter&&!label.text.StartsWith("“")&&!char.IsDigit(label.text[0]),"Choice presentation not centered/stripped");
        }
        ClickUi("Choice1");yield return new WaitForSeconds(.2f);
        Assert(!string.IsNullOrEmpty(narrative.CurrentLine),"Dialogue choice did not produce text");
        Assert(ui.transform.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().All(s=>s.name!="DialogueChoices"),"Choices still use a scroll view");
        yield return Capture("/tmp/dialogue-speech.png");
        var speech=ui.GetComponentsInChildren<UnityEngine.UI.Text>().First(t=>t.name=="Speech");
        string raw=narrative.PresentedLine;int colon=raw.IndexOf('：');
        string spoken=colon>0&&colon<12?raw.Substring(colon+1).Trim():raw;
        if(colon>0&&colon<12&&spoken.StartsWith("“")&&spoken.EndsWith("”"))spoken=spoken.Substring(1,spoken.Length-2);
        Assert(speech.text==spoken,"Dialogue display quote stripping failed");
        Assert(narrative.FullHistory.Contains(raw),"Original dialogue missing from history");
        var speaker=ui.GetComponentsInChildren<UnityEngine.UI.Text>().First(t=>t.name=="Speaker");
        Assert(Mathf.Abs(((RectTransform)speaker.transform.parent).rect.width-Mathf.Max(64,speaker.preferredWidth+40))<1,"Speaker plaque not fitted to name");
        Log("PASS speech presentation strips outer quotes, history preserves original and name plaque fits text");
        string heldLine=narrative.CurrentLine;ClickUi("DialogueHistory");yield return null;
        Assert(pause.HistoryVisible&&GamePauseMenu.IsPaused,"Dialogue review did not pause");narrative.ContinueDialogue();Assert(narrative.CurrentLine==heldLine,"Review advanced dialogue");
        yield return Capture("/tmp/ui-dialogue-history.png");ClickUi("HistoryBack");yield return null;Assert(!GamePauseMenu.IsPaused,"Review did not return to dialogue");
        string priorLine=narrative.CurrentLine;if(narrative.CurrentChoiceCount==0)ClickUi("Continue");else ClickUi("Choice"+(narrative.CurrentChoiceCount-1));yield return null;
        Assert(narrative.CurrentLine!=priorLine||narrative.CurrentChoiceCount>0,"Continue button did not advance Ink");
        eve.enabled=eveEnabled;
        Log("PASS UI raycast clicks, tabs, Esc routing; pause freezes time/input and restores it; captured pause, controls, quit confirmation, both menu tabs and dialogue layout");
    }
}

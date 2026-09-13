using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Tavern25D;
using DungeonTavern.Prototypes.Rotation25D;
static class CustomerPresentationCheck
{
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    static void Assert(bool yes,string text){if(!yes)throw new Exception(text);}
    static void Log(string text)=>File.AppendAllText("/tmp/demo-flow-playcheck.txt",text+"\n");
    public static IEnumerator Run(PrototypePlayerMover player)
    {
        var barrel=UnityEngine.Object.FindAnyObjectByType<DrinkBarrelPoint>();var hands=player.GetComponent<PlayerHands>();
        var playerBody=player.GetComponent<CharacterController>();playerBody.enabled=false;player.transform.position=barrel.transform.position+Vector3.back*1.55f;playerBody.enabled=true;Physics.SyncTransforms();
        hands.Clear();hands.TryHold(HeldItem.EmptyCup);
        Assert(player.GetComponent<PlayerInteractionController>().TryInteract()&&hands.CurrentItem==HeldItem.TestDrink,"Actual barrel F interaction blocked by its own parent collider");hands.Clear();
        playerBody.enabled=false;player.transform.position=new Vector3(30,0,14);playerBody.enabled=true;
        Log("PASS actual scene barrel selected through PlayerInteractionController and fills held empty cup");
        var seat=UnityEngine.Object.FindObjectsByType<SeatPoint>().First(s=>!s.IsStanding&&s.Chair);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Gameplay/Prefabs/Customer_Test.prefab");
        var go=UnityEngine.Object.Instantiate(prefab,seat.Position,Quaternion.LookRotation(seat.Facing));
        var customer=go.GetComponent<CustomerServicePoint>();
        Assert(Mathf.Abs(customer.GetComponent<NpcNavigator>().ConfiguredSpeed-player.MoveSpeed)<.001f,"Customer speed differs from player");
        var eve=UnityEngine.Object.FindAnyObjectByType<DungeonTavern.Tavern25D.Narrative.Day1EveActor>(FindObjectsInactive.Include);
        eve.gameObject.SetActive(true);eve.enabled=false;yield return null;
        Assert(Mathf.Abs(eve.GetComponent<NpcNavigator>().ConfiguredSpeed-player.MoveSpeed*1.08f)<.001f,"Eve speed should be 108 percent of player");
        var menu=UnityEngine.Object.FindAnyObjectByType<TavernMenuSystem>();
        Set(customer,"menuSystem",menu);Set(customer,"<Order>k__BackingField",new CustomerOrder(new[]{new OrderRequest{item=HeldItem.TestDrink,quantity=1}},menu.FindDish));
        Set(customer,"isInitialized",true);go.transform.rotation=Quaternion.LookRotation(-seat.Facing);
        Set(customer,"assignedSeat",seat);
        foreach(var r in go.GetComponentsInChildren<Renderer>())r.enabled=true;
        var animator=go.GetComponentInChildren<Animator>();
        typeof(CustomerServicePoint).GetMethod("ChangeState",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(customer,new object[]{CustomerOrderState.MovingToSeat,true});
        yield return new WaitForSeconds(.15f);
        Assert(Vector3.Dot(go.transform.forward,seat.Facing)<.95f,"Seat facing snapped instantly");
        yield return new WaitForSeconds(2);
        Assert(customer.State==CustomerOrderState.WaitingForFood&&Vector3.Dot(go.transform.forward,seat.Facing)>.99f,"Customer did not turn before sitting");
        for(int i=0;i<10;i++)
        {
            yield return new WaitForSeconds(.5f);
            var hip=animator.GetBoneTransform(HumanBodyBones.Hips);var knee=animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            float angle=Vector3.Angle(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position-knee.position,animator.GetBoneTransform(HumanBodyBones.RightFoot).position-knee.position);
            if(i>1){Assert(animator.GetCurrentAnimatorStateInfo(0).IsName("SeatedIdle"),"Customer left seated state");Assert(angle<130,"Customer knee is standing during seated loop");Assert(Vector3.Distance(hip.position,seat.SittingSurface+Vector3.up*.12f)<.03f,"Hips off stool");}
        }
        go.SetActive(false);yield return null;go.SetActive(true);yield return new WaitForSeconds(1);
        Assert(animator.GetBool("Seated")&&animator.GetCurrentAnimatorStateInfo(0).IsName("SeatedIdle"),"Reactivation reset seated pose");
        Capture(go,animator);
        Log("PASS seated loop: knee remains bent for 5 seconds; hips on stool; seated state survives deactivate/reactivate");
        foreach(var table in UnityEngine.Object.FindObjectsByType<SeatingTable>())
        {
            var seats=table.Seats.Where(s=>s&&!s.IsStanding).ToArray();if(seats.Length==0)continue;
            var center=table.SurfaceCenter;
            foreach(var s in seats)
            {
                var toward=Vector3.ProjectOnPlane(center-s.SittingSurface,Vector3.up).normalized;
                Assert(Vector3.Dot(toward,s.Facing)>.1f,"Seat faces away from tabletop: "+table.name+" / "+s.name+" position="+s.SittingSurface+" center="+center+" facing="+s.Facing+" toward="+toward);
                if(table.TableType!=SeatingTableType.Long)Assert(Vector3.Dot(toward,s.Facing)>.99f,"Round-table seat does not face center");
                else Assert(Mathf.Abs(s.Facing.x)<.001f&&Mathf.Abs(s.Facing.z)>.999f,"Authored long table side seat must face inward along world Z: "+table.name+"/"+s.name);
            }
        }
        Log("PASS all authored round-table seats face center; long-table seats share perpendicular inward headings");
        foreach(var table in UnityEngine.Object.FindObjectsByType<SeatingTable>().Where(t=>t.TableType==SeatingTableType.SmallRound||t.name=="TableSet_Rectangular_Long_2"))
        {
            var guests=new System.Collections.Generic.List<GameObject>();
            foreach(var place in table.Seats)
            {
                var seatedGuest=UnityEngine.Object.Instantiate(prefab,place.Position,Quaternion.LookRotation(place.Facing));guests.Add(seatedGuest);
                var service=seatedGuest.GetComponent<CustomerServicePoint>();service.enabled=false;Set(service,"assignedSeat",place);
                foreach(var renderer in seatedGuest.GetComponentsInChildren<Renderer>())renderer.enabled=true;
                typeof(CustomerServicePoint).GetMethod("ChangeState",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(service,new object[]{CustomerOrderState.WaitingForFood,true});
            }
            yield return new WaitForSeconds(2);
            CaptureTable(table,guests.ToArray());foreach(var guest in guests)UnityEngine.Object.Destroy(guest);
        }
        customer.enabled=false;Set(customer,"isInitialized",false);
        typeof(CustomerServicePoint).GetMethod("ChangeState",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(customer,new object[]{CustomerOrderState.Leaving,true});
        var motion=go.GetComponentInChildren<CharacterModelMotion>();Assert(motion.IsStandingUp,"Stand-up not started");
        yield return new WaitForSeconds(1.5f);Assert(!motion.IsStandingUp,"Stand-up did not finish");
        Assert(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"Stand-up did not return to idle");
        Set(customer,"assignedSeat",null);Set(customer,"<State>k__BackingField",CustomerOrderState.WaitingForFood);
        customer.GetComponent<NpcNavigator>().enabled=false;customer.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;
        customer.enabled=true;go.transform.position=new Vector3(500,0,502);
        player.enabled=false;player.GetComponent<CharacterController>().enabled=false;player.transform.SetPositionAndRotation(new Vector3(500,0,500),Quaternion.identity);
        var interaction=player.GetComponent<PlayerInteractionController>();interaction.enabled=false;player.GetComponent<PlayerHands>().Clear();
        yield return null;Physics.SyncTransforms();
        var find=typeof(PlayerInteractionController).GetMethod("FindClosestTarget",BindingFlags.NonPublic|BindingFlags.Instance);
        Assert((InteractionPoint)find.Invoke(interaction,null)==customer,"Visible customer beyond old radius not selectable");
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(500,1,501);wall.transform.localScale=new Vector3(4,3,.2f);Physics.SyncTransforms();
        Assert((InteractionPoint)find.Invoke(interaction,null)==null,"Interaction passes through wall");wall.SetActive(false);
        player.transform.rotation=Quaternion.Euler(0,180,0);Assert((InteractionPoint)find.Invoke(interaction,null)==null,"Distant guest behind player selected");
        player.transform.rotation=Quaternion.identity;
        var second=UnityEngine.Object.Instantiate(prefab,new Vector3(501.5f,0,501.3f),Quaternion.identity).GetComponent<CustomerServicePoint>();
        second.GetComponent<NpcNavigator>().enabled=false;second.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;
        Set(second,"menuSystem",menu);Set(second,"<Order>k__BackingField",new CustomerOrder(new[]{new OrderRequest{item=HeldItem.TestDrink,quantity=1}},menu.FindDish));Set(second,"<State>k__BackingField",CustomerOrderState.WaitingForFood);
        yield return null;Physics.SyncTransforms();Assert((InteractionPoint)find.Invoke(interaction,null)==customer,"Forward guest not preferred over adjacent guest");
        player.transform.LookAt(second.ServicePosition);var euler=player.transform.eulerAngles;player.transform.rotation=Quaternion.Euler(0,euler.y,0);
        Assert((InteractionPoint)find.Invoke(interaction,null)==second,"Turning toward adjacent guest did not change selection");
        Log("PASS interaction uses body distance, blocks walls, rejects distant rear guests and selects facing guest among neighbours");
        UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(second.gameObject);
        UnityEngine.Object.Destroy(go);
    }
    static void CaptureTable(SeatingTable table,GameObject[] guests)
    {
        var parts=table.GetComponentsInChildren<Transform>(true).Concat(guests.SelectMany(g=>g.GetComponentsInChildren<Transform>())).ToArray();var layers=parts.Select(t=>t.gameObject.layer).ToArray();foreach(var part in parts)part.gameObject.layer=31;
        var go=new GameObject("TableCheckCamera");var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=table.TableType==SeatingTableType.Long?4.5f:3.5f;
        camera.transform.position=table.SurfaceCenter+new Vector3(0,10,-5);camera.transform.LookAt(table.SurfaceCenter);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.15f,.17f);
        var rt=new RenderTexture(1100,900,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1100,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1100,900),0,0);image.Apply();File.WriteAllBytes("/tmp/seating-"+table.TableType+".png",image.EncodeToPNG());RenderTexture.active=old;for(int i=0;i<parts.Length;i++)parts[i].gameObject.layer=layers[i];UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);
    }
    static void Capture(GameObject guest,Animator animator)
    {
        var head=animator.GetBoneTransform(HumanBodyBones.Head).position;var hip=animator.GetBoneTransform(HumanBodyBones.Hips).position;
        var go=new GameObject("SeatingCheckCamera");var c=go.AddComponent<Camera>();c.orthographic=true;c.orthographicSize=1.4f;c.transform.position=hip+guest.transform.right*3+guest.transform.forward*2+Vector3.up*1.5f;c.transform.LookAt((head+hip)*.5f);
        var chair=guest.GetComponent<CustomerServicePoint>().AssignedSeat.Chair;
        var parts=guest.GetComponentsInChildren<Transform>().Concat(chair.GetComponentsInChildren<Transform>()).ToArray();var layers=parts.Select(t=>t.gameObject.layer).ToArray();foreach(var part in parts)part.gameObject.layer=31;
        c.cullingMask=1<<31;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.12f,.15f,.17f);
        var rt=new RenderTexture(900,900,24);c.targetTexture=rt;c.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(900,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,900),0,0);image.Apply();File.WriteAllBytes("/tmp/customer-seated.png",image.EncodeToPNG());RenderTexture.active=old;for(int i=0;i<parts.Length;i++)parts[i].gameObject.layer=layers[i];UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);
    }
}

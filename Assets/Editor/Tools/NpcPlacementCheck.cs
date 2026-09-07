using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Tavern25D;

static class NpcPlacementCheck
{
    [MenuItem("Tools/Dungeon Tavern/Check Eve Door In Play Mode")]
    static void CheckEve()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play Mode first");
        var loader=UnityEngine.Object.FindAnyObjectByType<InitialAdditiveSceneLoader>();
        typeof(InitialAdditiveSceneLoader).GetMethod("SetHostContentVisible",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(loader,new object[]{true});
                var basement=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SealRoom_B1");
                if(basement.isLoaded)UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(basement);
        foreach(var story in UnityEngine.Object.FindObjectsByType<DungeonTavern.Tavern25D.Narrative.Day1NarrativeController>())story.enabled=false;
        var eve=UnityEngine.Object.FindObjectsByType<DungeonTavern.Tavern25D.Narrative.Day1EveActor>(FindObjectsInactive.Include).Single();eve.gameObject.SetActive(true);
        var agent=eve.GetComponent<NavMeshAgent>();var start=new Vector3(40,0,20);agent.Warp(start);eve.transform.position=start;
        var camera=UnityEngine.Object.FindAnyObjectByType<DungeonTavern.Prototypes.Rotation25D.PrototypeCameraOrbit>();camera.EndDialogueFraming();camera.FollowTarget=eve.transform;
        var player=UnityEngine.Object.FindAnyObjectByType<DungeonTavern.Prototypes.Rotation25D.PrototypePlayerMover>();
        player.MovementInputEnabled=false;player.GetComponentInChildren<CharacterModelMotion>().EndFullBodyAction();
        var playerBody=player.GetComponent<CharacterController>();playerBody.enabled=false;player.transform.position=new Vector3(37,0,20);playerBody.enabled=true;
        eve.BeginOpeningSwitchGuidance(GameObject.Find("EveOpeningSwitchGuide").transform,"拉动拉杆打开酒馆。");
        double started=EditorApplication.timeSinceStartup;
        void Check()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=Check;return;}
            if(EditorApplication.timeSinceStartup-started<10)return;
            var log=new StringBuilder();log.AppendLine($"ready={eve.IsOpeningGuidanceReady} pos={eve.transform.position} remaining={agent.remainingDistance} status={agent.pathStatus} velocity={agent.velocity} desired={agent.desiredVelocity} corners={string.Join(";",agent.path.corners.Select(v=>v.ToString()))}");
            foreach(var c in Physics.OverlapSphere(eve.transform.position+Vector3.up*.7f,1.2f,~0,QueryTriggerInteraction.Collide))log.AppendLine($"{c.name}: {c.GetType().Name} trigger={c.isTrigger} bounds={c.bounds} door={c.GetComponentInParent<DoorStateController>()?.IsOpen}");
            File.WriteAllText("/tmp/eve-door-check.txt",log.ToString());ScreenCapture.CaptureScreenshot("/tmp/eve-door-check.png");EditorApplication.update-=Check;
        }
        EditorApplication.update+=Check;
    }
    [MenuItem("Tools/Dungeon Tavern/Audit NPC Routes")]
    static void Audit()
    {
        var log=new StringBuilder();var registry=UnityEngine.Object.FindAnyObjectByType<SeatRegistry>();registry.RefreshSeats();
        var menu=GameObject.Find("MenuApproach").transform.position;
        foreach(var seat in registry.Seats)
        {
            var path=new NavMeshPath();bool found=NavMesh.SamplePosition(seat.Position,out var hit,1.5f,NavMesh.AllAreas);
            if(found)NavMesh.CalculatePath(menu,hit.position,NavMesh.AllAreas,path);
            log.AppendLine($"{seat.transform.parent.name}/{seat.name}: {path.status} sample={Vector3.Distance(hit.position,seat.Position):F2} pos={seat.Position} chair={(seat.Chair != null ? seat.Chair.position.ToString() : "standing")}");
            if(seat.Chair!=null)foreach(var r in seat.Chair.GetComponentsInChildren<Renderer>())log.AppendLine(" chair bounds="+r.bounds);
        }
        foreach(var table in registry.Tables.Where(t=>t.name.Contains("Long")))
        {
            foreach(var c in table.GetComponentsInChildren<Collider>())log.AppendLine($"COLLIDER {table.name}/{c.name}: {c.GetType().Name} {c.bounds}");
        }
        var target=GameObject.Find("EveOpeningSwitchGuide").transform.position;
        foreach(var p in new[]{new Vector3(40,0,20),new Vector3(40,0,14),new Vector3(46,0,18)})
        {var path=new NavMeshPath();NavMesh.CalculatePath(p,target,NavMesh.AllAreas,path);log.AppendLine($"Eve {p} to {target}: {path.status} corners={string.Join(";",path.corners.Select(v=>v.ToString()))}");}
        File.WriteAllText("/tmp/npc-routes.txt",log.ToString());
    }
}

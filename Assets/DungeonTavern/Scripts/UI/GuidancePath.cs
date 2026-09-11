using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace DungeonTavern.UI
{
    internal static class GuidancePath
    {
        public static Vector3[] Calculate(Vector3 from,Vector3 to,bool basement,Transform player)
        {
            // F1 already owns a baked navigation surface. Never use its overlapping coordinates for B1.
            if(!basement&&NavMesh.SamplePosition(from,out var start,1.5f,NavMesh.AllAreas)&&NavMesh.SamplePosition(to,out var end,2,NavMesh.AllAreas))
            {
                var path=new NavMeshPath();
                if(NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)return path.corners;
            }
            // B1 has no NPC navigation surface. A small physics grid follows floor/stair support.
            // Bounded to 4096 visits and refreshed once per second, only while the overdue hint is visible.
            const float spacing=.45f;
            var origin=from;var open=new List<Vector2Int>{Vector2Int.zero};var closed=new HashSet<Vector2Int>();
            var points=new Dictionary<Vector2Int,Vector3>{{Vector2Int.zero,from}};var cost=new Dictionary<Vector2Int,float>{{Vector2Int.zero,0}};var parent=new Dictionary<Vector2Int,Vector2Int>();
            for(int visit=0;visit<4096&&open.Count>0;visit++)
            {
                int best=0;float score=float.MaxValue;
                for(int i=0;i<open.Count;i++){float f=cost[open[i]]+Vector3.Distance(points[open[i]],to);if(f<score){score=f;best=i;}}
                var cell=open[best];open.RemoveAt(best);if(!closed.Add(cell))continue;var p=points[cell];
                if(Vector3.ProjectOnPlane(p-to,Vector3.up).magnitude<1.2f&&Mathf.Abs(p.y-to.y)<2)
                {var result=new List<Vector3>{p};while(parent.TryGetValue(cell,out var previous)){cell=previous;result.Add(points[cell]);}result.Reverse();return result.ToArray();}
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                {
                    if(x==0&&z==0)continue;var n=cell+new Vector2Int(x,z);if(closed.Contains(n)||Mathf.Abs(n.x)>80||Mathf.Abs(n.y)>80)continue;
                    Vector3 q=new(origin.x+n.x*spacing,p.y,origin.z+n.y*spacing);
                    if(!Physics.Raycast(q+Vector3.up*.55f,Vector3.down,out var ground,1.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||ground.normal.y<.65f)continue;
                    q.y=ground.point.y;if(Mathf.Abs(q.y-p.y)>.5f)continue;
                    bool blocked=false;
                    foreach(var hit in Physics.CapsuleCastAll(p+Vector3.up*.75f,p+Vector3.up*1.25f,.22f,(q-p).normalized,Vector3.Distance(p,q),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                        if(!hit.transform.IsChildOf(player)&&hit.normal.y<.65f){blocked=true;break;}
                    if(blocked)continue;float g=cost[cell]+Vector3.Distance(p,q);
                    if(cost.TryGetValue(n,out float old)&&old<=g)continue;
                    cost[n]=g;points[n]=q;parent[n]=cell;if(!open.Contains(n))open.Add(n);
                }
            }
            return System.Array.Empty<Vector3>(); // Do not draw a straight line through walls when no safe route exists.
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonTavern.UI
{
    // Project world-height geometry into the same overlay as bubbles. The guide remains readable behind scenery.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GuidanceVisualGraphic : MaskableGraphic
    {
        readonly List<Vector3> path=new(), curve=new();
        readonly List<Vector2> projected=new();
        TavernUI ui; Camera view; Vector3 destination; GuideStep step;
        static readonly Color Cyan=new(.24f,.79f,1f), Ice=new(.8f,.96f,1f);
        public Vector3 RouteStart { get; private set; }
        public void Draw(TavernUI owner,Camera camera,Vector3[] route,Vector3 from,Vector3 target,GuideStep guideStep)
        {
            ui=owner;view=camera;step=guideStep;path.Clear();curve.Clear();
            destination=target;
            if(route.Length>1)
            {
                // Trim the travelled prefix each frame; route search itself can run less frequently.
                int segment=0;float closest=float.PositiveInfinity;Vector3 join=route[0];
                for(int i=0;i<route.Length-1;i++)
                {
                    Vector3 delta=route[i+1]-route[i];float t=Mathf.Clamp01(Vector3.Dot(from-route[i],delta)/Mathf.Max(.0001f,delta.sqrMagnitude));
                    Vector3 p=route[i]+t*delta;float distance=(p-from).sqrMagnitude;
                    if(distance<closest){closest=distance;segment=i;join=p;}
                }
                path.Add(from);
                if((join-from).sqrMagnitude>.0025f)path.Add(join);
                for(int i=segment+1;i<route.Length;i++)if((route[i]-path[^1]).sqrMagnitude>.0025f)path.Add(route[i]);
                // The visual connector may rise to a prop on a counter; it is not a navigation segment.
                if(step==GuideStep.Cup&&(path[^1]-destination).sqrMagnitude>.01f)path.Add(destination);
                // The destination is fixed by the guide, not by each new route search.
                curve.Add(path[0]);
                for(int i=1;i<path.Count-1;i++)
                {
                    Vector3 a=path[i-1],b=path[i],c=path[i+1];
                    // Round within the path clearance, rather than an overshooting Catmull-Rom spline.
                    float cut=Mathf.Min(.16f,Vector3.Distance(a,b)*.35f,Vector3.Distance(b,c)*.35f);
                    Vector3 entry=b+(a-b).normalized*cut,exit=b+(c-b).normalized*cut;
                    curve.Add(entry);
                    for(int j=1;j<=6;j++){float t=j/6f;curve.Add((1-t)*(1-t)*entry+2*(1-t)*t*b+t*t*exit);}
                }
                if(path.Count>1)curve.Add(path[^1]);
            }
            RouteStart=from+Vector3.up*.28f;
            SetVerticesDirty();
        }
        Vector2 Project(Vector3 p)=>ui.WorldToUI(p,view);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!ui||!view)return;
            projected.Clear();
            foreach(var p in curve)projected.Add(Project(p+Vector3.up*.28f));
            // Joined strips, not separate dash objects. Soft shoulders surround an uninterrupted bright core.
            Strip(vh,projected,16,WithAlpha(Cyan,.045f));
            Strip(vh,projected,9,WithAlpha(Cyan,.10f));
            Strip(vh,projected,4,WithAlpha(Cyan,.28f));
            Strip(vh,projected,1.4f,WithAlpha(Ice,.92f));
            if(view.WorldToViewportPoint(destination).z<=0)return;
            Vector2 foot=Project(destination+Vector3.up*.025f),top=Project(destination+Vector3.up*1.5f);
            // A world-aligned pool anchors the beacon to the floor.
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;
                int n=vh.currentVertCount;
                vh.AddVert(foot,WithAlpha(Cyan,.32f),Vector2.zero);
                vh.AddVert(Project(destination+new Vector3(Mathf.Cos(a)*.45f,.025f,Mathf.Sin(a)*.45f)),WithAlpha(Cyan,0),Vector2.zero);
                vh.AddVert(Project(destination+new Vector3(Mathf.Cos(b)*.45f,.025f,Mathf.Sin(b)*.45f)),WithAlpha(Cyan,0),Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);
            }
            for(int layer=4;layer>=1;layer--)
            {
                float width=layer*6f;
                Vector2 middle=Vector2.Lerp(foot,top,.43f);
                Beam(vh,foot,middle,width,WithAlpha(Cyan,.015f),WithAlpha(Cyan,.10f));
                Beam(vh,middle,top,width,WithAlpha(Cyan,.10f),WithAlpha(Cyan,0));
            }
            Vector2 centre=Project(destination+Vector3.up*(.68f+Mathf.Sin(Time.time*2)*.025f));
            for(int i=5;i>=1;i--)Diamond(vh,centre,25+i*3,WithAlpha(Cyan,.025f));
            Diamond(vh,centre,26,new Color(.35f,.72f,.79f));
            Diamond(vh,centre,23,new Color(.045f,.09f,.115f));
            // Broken outer runic frame and a small copper setting distinguish a destination seal from a button.
            Line(vh,centre+new Vector2(-30,2),centre+new Vector2(-17,15),2,Ice);
            Line(vh,centre+new Vector2(30,2),centre+new Vector2(17,15),2,Ice);
            Line(vh,centre+new Vector2(-7,-29),centre+new Vector2(7,-29),2,TavernUiTheme.Gold);
            if(step is GuideStep.Cup or GuideStep.Fill)
            {
                Line(vh,centre+new Vector2(-7,9),centre+new Vector2(-7,-9),2,Ice);
                Line(vh,centre+new Vector2(-7,-9),centre+new Vector2(6,-9),2,Ice);
                Line(vh,centre+new Vector2(6,-9),centre+new Vector2(6,9),2,Ice);
                Line(vh,centre+new Vector2(-7,9),centre+new Vector2(6,9),2,Ice);
                Line(vh,centre+new Vector2(6,5),centre+new Vector2(11,5),2,Ice);
                Line(vh,centre+new Vector2(11,5),centre+new Vector2(11,-4),2,Ice);
                Line(vh,centre+new Vector2(11,-4),centre+new Vector2(6,-4),2,Ice);
            }
            else
            {
                // Doorway/pin silhouette: an explicit destination, no directional arrow.
                Line(vh,centre+new Vector2(-9,-11),centre+new Vector2(-9,7),2.5f,Ice);
                Line(vh,centre+new Vector2(-9,7),centre+new Vector2(0,14),2.5f,Ice);
                Line(vh,centre+new Vector2(0,14),centre+new Vector2(9,7),2.5f,Ice);
                Line(vh,centre+new Vector2(9,7),centre+new Vector2(9,-11),2.5f,Ice);
                Line(vh,centre+new Vector2(-4,-11),centre+new Vector2(-4,6),2,Ice);
                Line(vh,centre+new Vector2(-4,6),centre+new Vector2(3,9),2,Ice);
                Line(vh,centre+new Vector2(3,9),centre+new Vector2(3,-11),2,Ice);
            }
        }
        static Color WithAlpha(Color c,float alpha){c.a=alpha;return c;}
        static void Strip(VertexHelper vh,List<Vector2> points,float width,Color tint)
        {
            if(points.Count<2)return;int start=vh.currentVertCount;
            for(int i=0;i<points.Count;i++)
            {
                Vector2 direction=points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)];
                Vector2 normal=new Vector2(-direction.y,direction.x).normalized*width*.5f;
                vh.AddVert(points[i]-normal,tint,Vector2.zero);vh.AddVert(points[i]+normal,tint,Vector2.zero);
                if(i>0){int n=start+i*2;vh.AddTriangle(n-2,n-1,n+1);vh.AddTriangle(n-2,n+1,n);}
            }
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {
            Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
            Quad(vh,a-n,a+n,b+n,b-n,tint,tint);
        }
        static void Beam(VertexHelper vh,Vector2 a,Vector2 b,float width,Color low,Color high)
        {Quad(vh,a+Vector2.left*width,a+Vector2.right*width,b+Vector2.right*width*.5f,b+Vector2.left*width*.5f,low,high);}
        static void Diamond(VertexHelper vh,Vector2 c,float r,Color tint)
        {Quad(vh,c+Vector2.up*r,c+Vector2.right*r,c+Vector2.down*r,c+Vector2.left*r,tint,tint);}
        static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color first,Color last)
        {
            int n=vh.currentVertCount;vh.AddVert(a,first,Vector2.zero);vh.AddVert(b,first,Vector2.zero);vh.AddVert(c,last,Vector2.zero);vh.AddVert(d,last,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}

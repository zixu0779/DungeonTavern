using UnityEngine;
using UnityEngine.UI;
namespace DungeonTavern.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TavernPanelGraphic : MaskableGraphic
    {
        public Color Border=new(.54f,.39f,.22f);
        public float Cut=10,BorderWidth=2;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var outer=Points(rectTransform.rect,Cut);var r=rectTransform.rect;
            r.xMin+=BorderWidth;r.xMax-=BorderWidth;r.yMin+=BorderWidth;r.yMax-=BorderWidth;
            var inner=Points(r,Mathf.Max(0,Cut-BorderWidth));
            // Ring geometry avoids double alpha blending during fades.
            for(int i=0;i<8;i++){vh.AddVert(outer[i],Border,Vector2.zero);vh.AddVert(inner[i],Border,Vector2.zero);}
            for(int i=0;i<8;i++){int a=i*2,b=((i+1)%8)*2;vh.AddTriangle(a,b,b+1);vh.AddTriangle(a,b+1,a+1);}
            int start=vh.currentVertCount;vh.AddVert(r.center,color,Vector2.zero);foreach(var p in inner)vh.AddVert(p,color,Vector2.zero);
            for(int i=0;i<8;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%8);
        }
        static Vector2[] Points(Rect r,float c)=>new Vector2[]{new(r.xMin+c,r.yMin),new(r.xMax-c,r.yMin),new(r.xMax,r.yMin+c),new(r.xMax,r.yMax-c),new(r.xMax-c,r.yMax),new(r.xMin+c,r.yMax),new(r.xMin,r.yMax-c),new(r.xMin,r.yMin+c)};
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace DungeonTavern.UI
{
    // Resolution-independent cut stone / copper frame. No texture stretching at different aspect ratios.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TavernPanelGraphic : MaskableGraphic
    {
        public Color Border = new(.54f, .39f, .22f);
        public float Cut = 10, BorderWidth = 2;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            Polygon(vh, rect, Cut, Border);
            rect.xMin += BorderWidth; rect.xMax -= BorderWidth;
            rect.yMin += BorderWidth; rect.yMax -= BorderWidth;
            Polygon(vh, rect, Mathf.Max(0, Cut - BorderWidth), color);
        }
        static void Polygon(VertexHelper vh, Rect r, float c, Color tint)
        {
            int start = vh.currentVertCount;
            Vector2[] p = { new(r.xMin+c,r.yMin),new(r.xMax-c,r.yMin),new(r.xMax,r.yMin+c),new(r.xMax,r.yMax-c),new(r.xMax-c,r.yMax),new(r.xMin+c,r.yMax),new(r.xMin,r.yMax-c),new(r.xMin,r.yMin+c) };
            vh.AddVert(r.center, tint, Vector2.zero);
            foreach(var point in p) vh.AddVert(point, tint, Vector2.zero);
            for(int i=0;i<8;i++) vh.AddTriangle(start,start+1+i,start+1+(i+1)%8);
        }
    }
}

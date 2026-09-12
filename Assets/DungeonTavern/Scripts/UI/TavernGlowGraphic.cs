using UnityEngine;
using UnityEngine.UI;
namespace DungeonTavern.UI
{
    // Vertex-alpha falloff around the cut-corner badge; no solid rectangular backplate.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TavernGlowGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;Vector2 centre=r.center;
            for(int ring=0;ring<3;ring++)
            {
                float extent=ring==0?42:ring==1?34:30;
                var tint=color;tint.a*=ring==0?0:ring==1?.25f:.8f;
                Vector2[] points={new(-extent+12,extent),new(extent-12,extent),new(extent,extent-12),new(extent,-extent+12),
                    new(extent-12,-extent),new(-extent+12,-extent),new(-extent,-extent+12),new(-extent,extent-12)};
                foreach(var p in points)vh.AddVert(centre+p,tint,Vector2.zero);
                if(ring==0)continue;
                for(int i=0;i<8;i++){int a=(ring-1)*8+i,b=(ring-1)*8+(i+1)%8;vh.AddTriangle(a,b,b+8);vh.AddTriangle(a,b+8,a+8);}
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace DungeonTavern.UI
{
    public enum TavernGlyph { Menu, Coin, Guests, Cup, Dish, SideDish, Exit, Lever, Book, Arrow }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TavernIcon : MaskableGraphic
    {
        public TavernGlyph Glyph;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch(Glyph)
            {
                case TavernGlyph.Menu: Line(vh,6,8,26,8);Line(vh,6,16,26,16);Line(vh,6,24,26,24);break;
                case TavernGlyph.Coin: Ring(vh,16,16,13);Ring(vh,16,16,9);Line(vh,16,9,21,16);Line(vh,21,16,16,23);Line(vh,16,23,11,16);Line(vh,11,16,16,9);break;
                case TavernGlyph.Guests: Ring(vh,12,9,5);Line(vh,3,28,3,23);Line(vh,3,23,7,18);Line(vh,7,18,17,18);Line(vh,17,18,22,23);Line(vh,22,23,22,28);Line(vh,3,28,22,28);Ring(vh,25,10,3);Line(vh,26,18,30,23);Line(vh,30,23,30,28);break;
                case TavernGlyph.Cup: Line(vh,5,7,22,7);Line(vh,22,7,22,28);Line(vh,22,28,5,28);Line(vh,5,28,5,7);Line(vh,22,11,28,11);Line(vh,28,11,28,23);Line(vh,28,23,22,23);Line(vh,10,12,10,23);break;
                case TavernGlyph.Dish: Ring(vh,16,18,11);Ring(vh,16,18,7);Line(vh,11,4,21,4);break;
                case TavernGlyph.SideDish: Line(vh,3,15,29,15);Line(vh,3,15,9,27);Line(vh,9,27,23,27);Line(vh,23,27,29,15);Line(vh,12,11,10,6);Line(vh,20,11,22,5);break;
                case TavernGlyph.Lever: Line(vh,4,28,28,28);Line(vh,10,28,10,23);Line(vh,10,23,22,23);Line(vh,22,23,22,28);Line(vh,16,23,23,7);Ring(vh,24,5,3);break;
                case TavernGlyph.Book: Line(vh,3,5,16,8);Line(vh,16,8,29,5);Line(vh,29,5,29,26);Line(vh,29,26,16,29);Line(vh,16,29,3,26);Line(vh,3,26,3,5);Line(vh,16,8,16,29);break;
                default: Line(vh,16,27,16,5);Line(vh,16,5,7,14);Line(vh,16,5,25,14);break;
            }
        }
        void Ring(VertexHelper vh,float x,float y,float radius)
        { for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;Line(vh,x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius);} }
        void Line(VertexHelper vh,float x,float y,float u,float v)
        {
            var rect=GetPixelAdjustedRect();Vector2 a=new(rect.x+x/32*rect.width,rect.y+(32-y)/32*rect.height),b=new(rect.x+u/32*rect.width,rect.y+(32-v)/32*rect.height);
            Vector2 d=(b-a).normalized;Vector2 n=new(-d.y,d.x);n*=Mathf.Min(rect.width,rect.height)*.034f;
            int start=vh.currentVertCount;vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        public static TavernIcon Add(Transform parent,TavernGlyph glyph,float x,float y,float size,Color? tint=null)
        {var r=TavernUiTheme.Rect(glyph+"Icon",parent);TavernUiTheme.Place(r,x,y,size,size);var icon=r.gameObject.AddComponent<TavernIcon>();icon.Glyph=glyph;icon.color=tint??TavernUiTheme.Gold;icon.raycastTarget=false;return icon;}
    }
}

using UnityEngine;
using UnityEngine.UI;
using DungeonTavern.Gameplay.Interaction;
namespace DungeonTavern.UI
{
    // Compact painted silhouettes shared by the ledger and order bubbles.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TavernDishIcon : MaskableGraphic
    {
        public HeldItem Item;
        static readonly Color Ink=new(.23f,.17f,.12f), Bone=new(.96f,.88f,.66f), Green=new(.35f,.52f,.22f), Meat=new(.66f,.28f,.16f), Gold=new(.83f,.59f,.22f);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();raycastTarget=false;
            if(TavernMenuSystem.IsDrink(Item))
            {
                Ellipse(vh,.76f,.48f,.23f,.28f,Ink);Ellipse(vh,.76f,.48f,.13f,.17f,Bone);
                Quad(vh,.13f,.12f,.64f,.84f,Ink);
                Quad(vh,.21f,.2f,.55f,.76f,Item==HeldItem.GlowcapAle?new Color(.24f,.64f,.57f):Item==HeldItem.CinderMead?Meat:Gold);
                Ellipse(vh,.38f,.8f,.3f,.09f,Bone);
                if(Item==HeldItem.GlowcapAle){Ellipse(vh,.32f,.47f,.05f,.06f,Bone);Ellipse(vh,.45f,.6f,.04f,.05f,Bone);}
                if(Item==HeldItem.CinderMead)Quad(vh,.34f,.35f,.44f,.57f,Gold);
                return;
            }
            Ellipse(vh,.5f,.2f,.47f,.13f,Ink);Ellipse(vh,.5f,.23f,.4f,.08f,Bone);
            switch(Item)
            {
                case HeldItem.MainDish:
                    Quad(vh,.19f,.25f,.81f,.58f,Ink);Ellipse(vh,.5f,.56f,.35f,.14f,Meat);
                    Ellipse(vh,.4f,.59f,.09f,.065f,Bone);Ellipse(vh,.62f,.56f,.075f,.05f,Green);
                    Quad(vh,.25f,.76f,.28f,.94f,Bone);Quad(vh,.65f,.72f,.68f,.87f,Bone);break;
                case HeldItem.CaveBoarPlatter:
                    Quad(vh,.19f,.4f,.83f,.49f,Bone);Ellipse(vh,.84f,.48f,.06f,.09f,Bone);Ellipse(vh,.17f,.45f,.06f,.09f,Bone);
                    Ellipse(vh,.5f,.48f,.25f,.23f,Ink);Ellipse(vh,.5f,.51f,.22f,.18f,Meat);
                    Quad(vh,.37f,.46f,.41f,.59f,Gold);Quad(vh,.51f,.42f,.55f,.62f,Gold);break;
                case HeldItem.RootBread:
                    Ellipse(vh,.49f,.46f,.35f,.23f,Ink);Ellipse(vh,.49f,.5f,.32f,.19f,Gold);
                    Quad(vh,.3f,.52f,.35f,.64f,Bone);Quad(vh,.47f,.54f,.52f,.67f,Bone);Quad(vh,.64f,.52f,.69f,.64f,Bone);break;
                case HeldItem.SideDish:
                    Ellipse(vh,.36f,.39f,.17f,.14f,Gold);Ellipse(vh,.66f,.39f,.16f,.13f,Gold);Ellipse(vh,.51f,.6f,.17f,.15f,Gold);
                    Ellipse(vh,.47f,.61f,.025f,.03f,Ink);Ellipse(vh,.33f,.4f,.025f,.03f,Ink);break;
                default:
                    Quad(vh,.47f,.28f,.52f,.8f,Green);
                    for(int i=0;i<3;i++){Ellipse(vh,.37f,.4f+i*.13f,.14f,.065f,Green);Ellipse(vh,.63f,.46f+i*.13f,.14f,.065f,Green);}break;
            }
        }
        Vector3 P(float x,float y)=>new(rectTransform.rect.xMin+x*rectTransform.rect.width,rectTransform.rect.yMin+y*rectTransform.rect.height,0);
        void Quad(VertexHelper vh,float x,float y,float right,float top,Color c)
        {int n=vh.currentVertCount;vh.AddVert(P(x,y),c,Vector2.zero);vh.AddVert(P(right,y),c,Vector2.zero);vh.AddVert(P(right,top),c,Vector2.zero);vh.AddVert(P(x,top),c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        void Ellipse(VertexHelper vh,float x,float y,float rx,float ry,Color c)
        {int n=vh.currentVertCount;vh.AddVert(P(x,y),c,Vector2.zero);for(int i=0;i<=16;i++){float angle=i*Mathf.PI/8;vh.AddVert(P(x+Mathf.Cos(angle)*rx,y+Mathf.Sin(angle)*ry),c,Vector2.zero);if(i>0)vh.AddTriangle(n,n+i,n+i+1);}}
    }
}

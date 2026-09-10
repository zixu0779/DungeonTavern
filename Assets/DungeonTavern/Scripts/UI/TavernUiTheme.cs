using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace DungeonTavern.UI
{
    public static class TavernUiTheme
    {
        public static readonly Color Ink = new(.075f,.086f,.092f,1), Panel = new(.105f,.117f,.12f,.98f),
            Copper = new(.56f,.40f,.23f), Gold = new(.9f,.70f,.38f), Cream = new(.94f,.89f,.77f),
            Muted = new(.65f,.66f,.61f), Paper = new(.9f,.82f,.64f), PaperInk = new(.20f,.16f,.12f);
        static Font font;
        public static Font Font => font != null ? font : font = Resources.Load<Font>("TavernSans");
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var r = (RectTransform)go.transform; r.SetParent(parent,false); return r;
        }
        public static void Fill(RectTransform r, float inset=0)
        { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset; }
        // All design coordinates are top-left based, in a 1920 x 1080 reference canvas.
        public static void Place(RectTransform r, float x,float y,float w,float h, Vector2? anchor=null)
        { r.anchorMin=r.anchorMax=anchor??new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h); }
        public static Image Image(string name,Transform parent,Color color,bool raycast=false)
        { var r=Rect(name,parent);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=raycast;return image; }
        public static RectTransform PanelRect(string name,Transform parent, Color? color=null, bool raycast=false)
        {
            var r=Rect(name,parent);var g=r.gameObject.AddComponent<TavernPanelGraphic>();g.color=color??Panel;g.raycastTarget=raycast;
            var shadow=r.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.4f);shadow.effectDistance=new Vector2(0,-5);
            return r;
        }
        public static Text Label(string name,Transform parent,string value,int size=26,Color? color=null,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var r=Rect(name,parent);var t=r.gameObject.AddComponent<Text>();t.font=Font;t.fontSize=size;t.color=color??Cream;t.alignment=alignment;
            t.text=value;t.supportRichText=false;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;
            return t;
        }
        public static Button Button(string name,Transform parent,string value,UnityAction action,bool primary=false)
        {
            var r=PanelRect(name,parent,primary?new Color(.31f,.24f,.145f):Panel,true);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<TavernPanelGraphic>();
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.45f,1.35f,1.15f);colors.pressedColor=new Color(.8f,.75f,.65f);colors.selectedColor=colors.highlightedColor;colors.disabledColor=new Color(.48f,.48f,.48f,.75f);colors.fadeDuration=.12f;b.colors=colors;
            var navigation=b.navigation;navigation.mode=Navigation.Mode.None;b.navigation=navigation;
            var label=Label("Label",r,value,26,primary?Gold:Cream,TextAnchor.MiddleCenter);Fill(label.rectTransform,8);
            if(action!=null)b.onClick.AddListener(action);return b;
        }
        public static void Rule(Transform parent,float x,float y,float width)
        { var image=Image("CopperRule",parent,Copper);Place(image.rectTransform,x,y,width,1); }
        public static ScrollRect Scroll(string name,Transform parent,out RectTransform content)
        {
            var root=Rect(name,parent);var scroll=root.gameObject.AddComponent<ScrollRect>();
            var viewport=Rect("Viewport",root);Fill(viewport);viewport.gameObject.AddComponent<RectMask2D>();
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            content=Rect("Content",viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var bar=Image("Scrollbar",root,new Color(1,1,1,.04f),true);
            bar.rectTransform.anchorMin=new Vector2(1,0);bar.rectTransform.anchorMax=Vector2.one;bar.rectTransform.offsetMin=new Vector2(-8,0);bar.rectTransform.offsetMax=Vector2.zero;
            var handle=Image("Handle",bar.transform,Copper,true);Fill(handle.rectTransform);
            var scrollbar=bar.gameObject.AddComponent<Scrollbar>();scrollbar.targetGraphic=handle;scrollbar.handleRect=handle.rectTransform;scrollbar.direction=Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.scrollSensitivity=35;scroll.inertia=true;return scroll;
        }
        public static void Clear(Transform parent)
        { for(int i=parent.childCount-1;i>=0;i--){var child=parent.GetChild(i);child.gameObject.SetActive(false);Object.Destroy(child.gameObject);} }
        public static void Key(Transform parent,string key,float x,float y,float width=46)
        {
            var r=PanelRect("Key_"+key,parent,new Color(.18f,.19f,.19f));Place(r,x,y,width,38);
            var t=Label("Glyph",r,key,21,Gold,TextAnchor.MiddleCenter);Fill(t.rectTransform,2);
        }
    }
}

using UnityEngine;
using System.Linq;
using DungeonTavern.Gameplay.Interaction;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class WorldSpeechBubble : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new(0f, 2.2f, 0f);
        [SerializeField, Min(0f)] private float headClearance = 0.18f;
        private string line;
        private CustomerOrder order;
        private bool eating;
        private float hideAt = -1;
        private UnityEngine.RectTransform view;
        private string viewKey;
        private readonly System.Collections.Generic.List<UnityEngine.UI.Image> progress = new();
        private readonly System.Collections.Generic.List<RectTransform> icons = new();
        private float nextRefresh;
        private Renderer[] characterRenderers;

        private void Awake()
        {
            characterRenderers = GetComponentsInChildren<Renderer>(true);
        }

        public bool IsVisible => order != null || !string.IsNullOrEmpty(line);
        public string CurrentText => line ?? string.Empty;

        public void Show(string text, float visibleSeconds = -1f)
        {
            order = null;
            line = text;
            hideAt = visibleSeconds > 0 ? Time.time + visibleSeconds : -1;
        }

        private void Update() { if (hideAt > 0 && Time.time >= hideAt) Hide(); }

        public void Hide()
        {
            order = null;
            line = string.Empty;
            hideAt = -1;
        }

        public void ShowOrder(CustomerOrder customerOrder, bool isEating)
        {
            order = customerOrder;
            eating = isEating;
            line = string.Empty;
            hideAt = -1;
        }

        private void OnDisable()
        {
            if(view!=null)Destroy(view.gameObject);
            view=null;viewKey=null;
        }
        private void LateUpdate()
        {
            var ui=DungeonTavern.UI.TavernUI.Instance;
            var camera=Camera.main;
            if(ui==null||camera==null)return;
            var head=GetHeadAnchor();var point=camera.WorldToViewportPoint(head);
            bool visible=IsVisible&&point.z>0&&point.x>=0&&point.x<=1&&point.y>=0&&point.y<=1;
            if(view!=null)DungeonTavern.UI.TavernFadeIn.Show(view.gameObject,visible);
            if(!visible)return;
            if(view==null)
            {
                view=DungeonTavern.UI.TavernUiTheme.PanelRect(name+"_Bubble",ui.BubbleLayer,DungeonTavern.UI.TavernUiTheme.Paper);
                DungeonTavern.UI.TavernFadeIn.Add(view.gameObject);
                view.anchorMin=view.anchorMax=new Vector2(.5f,.5f);view.pivot=new Vector2(.5f,0);
            }
            if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.1f;RefreshView();}
            var local=ui.WorldToUI(head,camera)+new Vector2(0,18);
            var bounds=ui.BubbleLayer.rect;
            local.x=Mathf.Clamp(local.x,bounds.xMin+view.rect.width*.5f+12,bounds.xMax-view.rect.width*.5f-12);
            local.y=Mathf.Min(local.y,bounds.yMax-view.rect.height-12);
            view.anchoredPosition=local;
        }
        private void RefreshView()
        {
            var groups=order?.Portions.Where(p=>!p.Consumed).GroupBy(p=>p.Item).ToArray();
            string key=order==null?line:string.Join("|",groups.Select(g=>$"{g.Key}:{g.Count()}"));
            if(viewKey!=key)
            {
                viewKey=key;DungeonTavern.UI.TavernUiTheme.Clear(view);progress.Clear();icons.Clear();
                var tail=DungeonTavern.UI.TavernUiTheme.Image("Tail",view,DungeonTavern.UI.TavernUiTheme.Paper);
                DungeonTavern.UI.TavernUiTheme.Place(tail.rectTransform,-6,0,12,12,new Vector2(.5f,0));tail.rectTransform.localEulerAngles=new Vector3(0,0,45);
                if(order==null)
                {
                    var text=DungeonTavern.UI.TavernUiTheme.Label("Speech",view,line,24,DungeonTavern.UI.TavernUiTheme.PaperInk,TextAnchor.MiddleCenter);
                    float width=Mathf.Clamp(text.preferredWidth+42,120,360);text.rectTransform.sizeDelta=new Vector2(width-36,100);
                    float height=Mathf.Max(58,text.preferredHeight+26);
                    view.sizeDelta=new Vector2(width,height);DungeonTavern.UI.TavernUiTheme.Fill(text.rectTransform,14);
                }
                else
                {
                    view.sizeDelta=new Vector2(152,groups.Length*60+16);
                    for(int i=0;i<groups.Length;i++)
                    {
                        var icon=DungeonTavern.UI.TavernUiTheme.Rect("FoodIcon",view);DungeonTavern.UI.TavernUiTheme.Place(icon,18,14+i*60,34,36);icons.Add(icon);
                        DrawIcon(icon,groups[i].Key);
                        var quantity=DungeonTavern.UI.TavernUiTheme.Label("Quantity",view,"× "+groups[i].Count(),26,DungeonTavern.UI.TavernUiTheme.PaperInk);
                        DungeonTavern.UI.TavernUiTheme.Place(quantity.rectTransform,70,8+i*60,70,42);
                        var track=DungeonTavern.UI.TavernUiTheme.Image("Track",view,new Color(.35f,.28f,.18f,.3f));DungeonTavern.UI.TavernUiTheme.Place(track.rectTransform,18,55+i*60,116,3);
                        var fill=DungeonTavern.UI.TavernUiTheme.Image("Consumption",track.transform,new Color(.52f,.32f,.10f));DungeonTavern.UI.TavernUiTheme.Fill(fill.rectTransform);progress.Add(fill);
                    }
                }
            }
            if(groups==null)return;
            for(int i=0;i<groups.Length&&i<progress.Count;i++)
            {
                bool active=eating&&groups[i].Any(p=>p.Delivered&&!p.Consumed);
                float remaining=active?groups[i].Where(p=>p.Delivered).Average(p=>p.RemainingFraction):1;
                progress[i].transform.parent.gameObject.SetActive(active);
                progress[i].rectTransform.anchorMax=new Vector2(remaining,1);
                icons[i].localEulerAngles=new Vector3(0,0,active?Mathf.Sin(Time.time*3)*6:0);
            }
        }
        static void DrawIcon(RectTransform parent,HeldItem item)
        {
            void Part(string name,float x,float y,float w,float h,Color c)
            {var image=DungeonTavern.UI.TavernUiTheme.Image(name,parent,c);DungeonTavern.UI.TavernUiTheme.Place(image.rectTransform,x,y,w,h);}
            var ink=DungeonTavern.UI.TavernUiTheme.PaperInk;
            if(item==HeldItem.TestDrink)
            {
                Part("Handle",23,9,11,19,ink);Part("HandleHole",25,12,6,12,DungeonTavern.UI.TavernUiTheme.Paper);
                Part("Cup",0,4,25,30,ink);Part("Drink",4,8,17,22,new Color(.72f,.42f,.13f));Part("Foam",0,1,25,7,new Color(1,.97f,.85f));
            }
            else {Part("Plate",0,27,34,6,ink);Part("Food",5,10,25,17,item==HeldItem.MainDish?new Color(.60f,.26f,.13f):new Color(.3f,.43f,.17f));}
        }
        private Vector3 GetHeadAnchor()
        {
            float highestPoint = float.NegativeInfinity;
            characterRenderers ??= GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < characterRenderers.Length; index++)
            {
                Renderer renderer = characterRenderers[index];
                if (renderer != null && renderer.enabled)
                    highestPoint = Mathf.Max(highestPoint, renderer.bounds.max.y);
            }

            Vector3 anchor = transform.position;
            anchor.x += worldOffset.x;
            anchor.z += worldOffset.z;
            anchor.y = float.IsNegativeInfinity(highestPoint)
                ? transform.position.y + worldOffset.y
                : highestPoint + headClearance;
            return anchor;
        }

    }
}

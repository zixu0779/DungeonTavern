using UnityEngine;
using System.Linq;
using DungeonTavern.Gameplay.Interaction;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class WorldSpeechBubble : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new(0f, 2.2f, 0f);
        [SerializeField, Min(0f)] private float headClearance = 0.18f;
        private static readonly System.Collections.Generic.List<WorldSpeechBubble> activeBubbles=new();
        readonly System.Collections.Generic.List<Rect> occupied=new();
        readonly System.Collections.Generic.List<Vector2> candidates=new();
        RectTransform leaderLine;
        private string line;
        private void OnEnable()=>activeBubbles.Add(this);
        private CustomerOrder order;
        private bool eating;
        private HeldItem[] confirmed;
        private HeldItem candidate;
        private string reaction;
        private int animatedFrom;
        private float confirmationTime;
        private readonly System.Collections.Generic.List<RectTransform> confirmedViews=new();
        public void ShowChoosing(System.Collections.Generic.IEnumerable<HeldItem> selected,HeldItem pending,string state)
        {
            var items=selected.ToArray();
            if(confirmed==null||items.Length>confirmed.Length){animatedFrom=confirmed?.Length??0;confirmationTime=Time.time;}
            confirmed=items;candidate=pending;reaction=state;order=null;line="...";hideAt=-1;
        }
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
            confirmed=null;
            order = null;
            line = text;
            hideAt = visibleSeconds > 0 ? Time.time + visibleSeconds : -1;
        }

        private void Update() { if (hideAt > 0 && Time.time >= hideAt) Hide(); }

        public void Hide()
        {
            confirmed=null;
            order = null;
            line = string.Empty;
            hideAt = -1;
        }

        public void ShowOrder(CustomerOrder customerOrder, bool isEating)
        {
            confirmed=null;
            order = customerOrder;
            eating = isEating;
            line = string.Empty;
            hideAt = -1;
        }

        private void OnDisable()
        {
            activeBubbles.Remove(this);
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
            if(confirmed!=null)AnimateConfirmations();
            var local=ui.WorldToUI(head,camera)+new Vector2(0,18);
            var bounds=ui.BubbleLayer.rect;
            local.x=Mathf.Clamp(local.x,bounds.xMin+view.rect.width*.5f+12,bounds.xMax-view.rect.width*.5f-12);
            local.y=Mathf.Min(local.y,bounds.yMax-view.rect.height-12);
            var headLocal=local;
            if(confirmed!=null||order!=null)
            {
                occupied.Clear();candidates.Clear();candidates.Add(local);
                foreach(var other in activeBubbles)
                {
                    if(other==this)break;
                    if(!other||other.confirmed==null&&other.order==null||!other.view||!other.view.gameObject.activeInHierarchy)continue;
                    var pos=other.view.anchoredPosition;var size=other.view.sizeDelta;
                    var r=new Rect(pos.x-size.x*.5f-8,pos.y-8,size.x+16,size.y+16);occupied.Add(r);
                    candidates.Add(new Vector2(r.xMin-view.rect.width*.5f,local.y));
                    candidates.Add(new Vector2(r.xMax+view.rect.width*.5f,local.y));
                    candidates.Add(new Vector2(local.x,r.yMax));
                }
                float best=float.PositiveInfinity;
                foreach(var candidatePosition in candidates)
                {
                    var rect=new Rect(candidatePosition.x-view.rect.width*.5f,candidatePosition.y,view.rect.width,view.rect.height);
                    if(rect.xMin<bounds.xMin+8||rect.xMax>bounds.xMax-8||rect.yMax>bounds.yMax-8||occupied.Exists(r=>r.Overlaps(rect)))continue;
                    float cost=(candidatePosition-headLocal).sqrMagnitude;
                    if(cost<best){best=cost;local=candidatePosition;}
                }
                if(!leaderLine)
                {
                    leaderLine=DungeonTavern.UI.TavernUiTheme.Image("HeadLink",view,new Color(.68f,.51f,.29f,.65f)).rectTransform;
                    leaderLine.SetAsFirstSibling();leaderLine.anchorMin=leaderLine.anchorMax=new Vector2(.5f,0);leaderLine.pivot=new Vector2(.5f,0);
                }
                var link=headLocal-local;leaderLine.gameObject.SetActive(link.sqrMagnitude>16);
                leaderLine.anchoredPosition=Vector2.zero;leaderLine.sizeDelta=new Vector2(2,link.magnitude);
                leaderLine.localEulerAngles=new Vector3(0,0,Mathf.Atan2(link.y,link.x)*Mathf.Rad2Deg-90);
            }
            view.anchoredPosition=local;
        }
        private void RefreshView()
        {
            if(confirmed!=null){RefreshChoosing();return;}
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
                    bool compact=groups.Length>1;int columns=Mathf.Min(3,groups.Length);
                    view.sizeDelta=compact?new Vector2(columns*60+20,Mathf.Ceil(groups.Length/(float)columns)*60+16):new Vector2(152,76);
                    for(int i=0;i<groups.Length;i++)
                    {
                        float x=compact?14+i%columns*60:18,y=compact?8+i/columns*60:14;
                        var icon=DungeonTavern.UI.TavernUiTheme.Rect("FoodIcon",view);DungeonTavern.UI.TavernUiTheme.Place(icon,x,y,34,36);icons.Add(icon);
                        DrawIcon(icon,groups[i].Key);
                        var quantity=DungeonTavern.UI.TavernUiTheme.Label("Quantity",view,"× "+groups[i].Count(),compact?16:26,DungeonTavern.UI.TavernUiTheme.PaperInk);
                        DungeonTavern.UI.TavernUiTheme.Place(quantity.rectTransform,compact?x:70,compact?y+33:8,compact?48:70,compact?22:42);
                        var track=DungeonTavern.UI.TavernUiTheme.Image("Track",view,new Color(.35f,.28f,.18f,.3f));DungeonTavern.UI.TavernUiTheme.Place(track.rectTransform,compact?x:18,compact?y+54:55,compact?48:116,3);
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
        private void RefreshChoosing()
        {
            string key="choosing:"+string.Join(",",confirmed)+":"+candidate+":"+reaction;
            if(viewKey!=key)
            {
                viewKey=key;DungeonTavern.UI.TavernUiTheme.Clear(view);confirmedViews.Clear();
                view.sizeDelta=new Vector2(Mathf.Max(132,confirmed.Length*40+24),confirmed.Length>0?116:66);
                if(confirmed.Length>0)
                {
                    var tray=DungeonTavern.UI.TavernUiTheme.PanelRect("ConfirmedDishes",view,new Color(.88f,.78f,.57f));
                    DungeonTavern.UI.TavernUiTheme.Place(tray,4,4,view.sizeDelta.x-8,44);
                    for(int i=0;i<confirmed.Length;i++)
                    {
                        var icon=DungeonTavern.UI.TavernUiTheme.Rect("Confirmed"+i,tray);
                        DungeonTavern.UI.TavernUiTheme.Place(icon,8+i*40,5,34,36);DrawIcon(icon,confirmed[i]);confirmedViews.Add(icon);
                    }
                }
                float y=confirmed.Length>0?60:10;
                if(candidate!=HeldItem.None)
                {
                    var icon=DungeonTavern.UI.TavernUiTheme.Rect("ConsideringDish",view);
                    DungeonTavern.UI.TavernUiTheme.Place(icon,view.sizeDelta.x*.5f-38,y,34,36);DrawIcon(icon,candidate);
                }
                var text=DungeonTavern.UI.TavernUiTheme.Label("Reaction",view,reaction,27,DungeonTavern.UI.TavernUiTheme.PaperInk,TextAnchor.MiddleCenter);
                DungeonTavern.UI.TavernUiTheme.Place(text.rectTransform,candidate==HeldItem.None?0:view.sizeDelta.x*.5f+3,y,candidate==HeldItem.None?view.sizeDelta.x:40,38);
            }
        }
        private void AnimateConfirmations()
        {
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-confirmationTime)/.45f));
            for(int i=0;i<confirmedViews.Count;i++)
            {
                var icon=confirmedViews[i];
                icon.anchoredPosition=new Vector2(8+i*40,-5-(i>=animatedFrom?(1-t)*48:0));
                icon.localScale=Vector3.one*(i>=animatedFrom?Mathf.Lerp(.7f,1,t):1);
            }
        }
        static void DrawIcon(RectTransform parent,HeldItem item)
        {
            var graphic=parent.gameObject.AddComponent<DungeonTavern.UI.TavernDishIcon>();graphic.Item=item;graphic.raycastTarget=false;
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

using System.Collections.Generic;
using System.Linq;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static DungeonTavern.UI.TavernUiTheme;

namespace DungeonTavern.UI
{
    public enum GuideStep { Exit, Lever, Menu, Cup, Fill, Serve, Settle }
    // Session progress belongs to this persistent UI object; a future save includes completed.
    public sealed class TavernGuidance : MonoBehaviour
    {
        static TavernGuidance instance;
        readonly HashSet<GuideStep> completed=new();
        readonly float[] elapsed=new float[7];
        readonly List<RectTransform> dashes=new();
        [SerializeField,Min(1)] float textDelay=20,routeDelay=75;
        RectTransform notice, card, marker; Text areaTitle, areaSubtitle, title, body;
        CanvasGroup areaAlpha, cardAlpha;
        TavernUI ui; PrototypePlayerMover player; Day1NarrativeController narrative; TavernMenuSystem menu;
        FloorLeverPoint lever; Camera camera; PlayerHands hands; Transform target;
        Vector3 lastRouteFrom=new(float.PositiveInfinity,0,0),lastRouteTo;
        Vector3[] route=System.Array.Empty<Vector3>(); float nextPoll,nextRoute,areaAge=10;
        bool inBasement,initialized; GuideStep? current;
        public GuideStep? CurrentStep => current;
        public bool IsComplete(GuideStep step)=>completed.Contains(step);
        public bool TextVisible => card.gameObject.activeInHierarchy;
        public bool RouteVisible => marker.gameObject.activeInHierarchy;
        public static void Complete(GuideStep step) { if(instance)instance.completed.Add(step); }
        public void Initialize(TavernUI owner,RectTransform layer)
        {
            instance=this;ui=owner;
            notice=PanelRect("AreaAnnouncement",layer);Place(notice,-235,132,470,100,new Vector2(.5f,1));areaAlpha=notice.gameObject.AddComponent<CanvasGroup>();areaAlpha.blocksRaycasts=false;
            areaTitle=Label("AreaName",notice,"",32,Gold,TextAnchor.MiddleCenter);Place(areaTitle.rectTransform,24,8,422,48);
            areaSubtitle=Label("Floor",notice,"",20,Muted,TextAnchor.MiddleCenter);Place(areaSubtitle.rectTransform,24,59,422,29);
            card=PanelRect("FirstTimeGuidance",layer);Place(card,-310,145,620,128,new Vector2(.5f,1));cardAlpha=card.gameObject.AddComponent<CanvasGroup>();cardAlpha.blocksRaycasts=false;
            title=Label("Title",card,"",26,Gold);Place(title.rectTransform,26,12,568,40);
            body=Label("Instruction",card,"",23);Place(body.rectTransform,26,55,568,60);
            marker=PanelRect("GuidanceTarget",ui.BubbleLayer);marker.pivot=new Vector2(.5f,.5f);marker.sizeDelta=new Vector2(52,52);
            marker.GetComponent<TavernPanelGraphic>().color=new Color(.08f,.16f,.18f);TavernIcon.Add(marker,TavernGlyph.Arrow,10,10,32,new Color(.6f,.83f,.85f));
            notice.gameObject.SetActive(false);card.gameObject.SetActive(false);marker.gameObject.SetActive(false);
        }
        void OnDestroy(){if(instance==this)instance=null;}
        void Update()
        {
            if(!ui)return;
            if(Time.unscaledTime>=nextPoll)
            {
                nextPoll=Time.unscaledTime+.35f;
                if(!player)player=FindAnyObjectByType<PrototypePlayerMover>();
                if(!narrative)narrative=FindAnyObjectByType<Day1NarrativeController>();
                if(!menu)menu=FindAnyObjectByType<TavernMenuSystem>();
                if(!lever)lever=FindAnyObjectByType<FloorLeverPoint>(FindObjectsInactive.Include);
                if(!hands&&player)hands=player.GetComponent<PlayerHands>();
                if(!camera)camera=FindAnyObjectByType<PrototypeCameraOrbit>()?.GetComponentInChildren<Camera>();
                bool basement=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SealRoom_B1").isLoaded;
                if(player&&(!initialized||basement!=inBasement)&&!ui.TransitionVisible)
                {
                    bool wasInitialized=initialized; initialized=true;inBasement=basement;areaAge=0;lastRouteFrom=new Vector3(float.PositiveInfinity,0,0);route=System.Array.Empty<Vector3>();
                    areaTitle.text=basement?"封印之间":"地下酒馆";areaSubtitle.text=basement?"B1 · 地下层":"F1 · 酒馆大厅";
                    if(wasInitialized&&!basement)Complete(GuideStep.Exit);
                }
                PickStep();
            }
            bool blocked=!player||!narrative||GamePauseMenu.IsPaused||ui.TransitionVisible||TavernUI.WindowOpen||narrative.State==Day1FlowState.Dialogue;
            if(!blocked)areaAge+=Time.deltaTime;
            notice.gameObject.SetActive(initialized&&!blocked&&areaAge<3.4f);
            areaAlpha.alpha=areaAge<.4f?Ease(areaAge/.4f):1-Mathf.Clamp01((areaAge-2.8f)/.6f);
            notice.anchoredPosition=new Vector2(-235,-132+8*(1-areaAlpha.alpha));
            bool actionable=!blocked&&current.HasValue&&!completed.Contains(current.Value)&&player.MovementInputEnabled;
            if(actionable)elapsed[(int)current.Value]+=Time.deltaTime;
            float age=current.HasValue?elapsed[(int)current.Value]:0;
            bool awakening=!blocked&&narrative.State==Day1FlowState.Awakening&&areaAge>=3.4f;
            if(awakening){title.text="站起身来";body.text="按 WASD 起身，随后沿石阶寻找出口。";}
            card.gameObject.SetActive(awakening||(actionable&&age>=textDelay&&areaAge>=3.4f));
            cardAlpha.alpha=awakening?1:Ease((age-textDelay)/.3f);
            bool showRoute=actionable&&age>=routeDelay&&target&&camera;
            marker.gameObject.SetActive(showRoute);
            if(showRoute)
            {
                if(Time.unscaledTime>=nextRoute&&((player.transform.position-lastRouteFrom).sqrMagnitude>.5f||(target.position-lastRouteTo).sqrMagnitude>.5f||route.Length==0))
                { nextRoute=Time.unscaledTime+1;lastRouteFrom=player.transform.position;lastRouteTo=target.position;route=GuidancePath.Calculate(lastRouteFrom,lastRouteTo,inBasement,player.transform); }
                DrawRoute();
            }
            else foreach(var dash in dashes)dash.gameObject.SetActive(false);
        }
        static float Ease(float t){t=Mathf.Clamp01(t);return 1-Mathf.Pow(1-t,3);}
        void PickStep()
        {
            current=null;target=null;if(!player||!hands||!narrative)return;
            if(lever&&lever.IsOn)Complete(GuideStep.Lever);
            if(inBasement)
            {
                if(narrative.State==Day1FlowState.AwaitingStorageReturn)
                    Set(GuideStep.Exit,FindAnyObjectByType<AdditiveScenePortal>()?.transform,"寻找出口","沿石阶返回楼上的酒馆。\nWASD 移动");
                return;
            }
            if(!narrative.ManagementUnlocked)return;
            if(!completed.Contains(GuideStep.Lever))
            {if(narrative.CanOpenTavern)Set(GuideStep.Lever,lever?.transform,"开始营业","到伊芙身旁的拉杆处，按 F 打开酒馆。");return;}
            var guests=FindObjectsByType<CustomerServicePoint>();
            var settling=guests.FirstOrDefault(c=>c.State==CustomerOrderState.AwaitingSettlement);
            if(!completed.Contains(GuideStep.Settle)&&settling){Set(GuideStep.Settle,settling.transform,"为客人结账","走到已用餐完毕的客人身旁，按 F 结账。");return;}
            if(menu&&menu.PendingOrderCount>0&&!completed.Contains(GuideStep.Menu))
            {Set(GuideStep.Menu,FindAnyObjectByType<TavernMenuPoint>()?.transform,"查看订单","按 M 打开账簿，或靠近菜单按 F 查看。");return;}
            if(hands.CurrentItem==HeldItem.EmptyCup)
            {Set(GuideStep.Fill,FindAnyObjectByType<DrinkBarrelPoint>()?.transform,"接取酒水","带着空杯走到酒桶旁，按 F 接酒。");return;}
            if(hands.CurrentItem is HeldItem.TestDrink or HeldItem.MainDish or HeldItem.SideDish)
            {
                var guest=guests.Where(c=>c.Order!=null&&c.Order.Needs(hands.CurrentItem)&&c.State is CustomerOrderState.WaitingForFood or CustomerOrderState.Eating).OrderBy(c=>(c.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault();
                if(guest)Set(GuideStep.Serve,guest.transform,"为客人上菜","靠近需要这份餐点的客人，按 F 送上餐点。");return;
            }
            var cups=FindAnyObjectByType<CupDispenserPoint>();
            if(cups&&cups.CupReady)Set(GuideStep.Cup,cups.transform,"寻找酒杯","取杯器已升起酒杯，靠近后按 F 取杯。");
        }
        void Set(GuideStep step,Transform destination,string heading,string instruction)
        {if(completed.Contains(step)||!destination)return;current=step;target=destination;title.text=heading;body.text=instruction;}
        void DrawRoute()
        {
            Vector2 point=ui.WorldToUI(target.position+Vector3.up*1.8f,camera),unclamped=point;var bounds=ui.BubbleLayer.rect;
            point.x=Mathf.Clamp(point.x,bounds.xMin+42,bounds.xMax-42);point.y=Mathf.Clamp(point.y,bounds.yMin+42,bounds.yMax-42);
            marker.anchoredPosition=point+Vector2.up*(Mathf.Sin(Time.time*2)*3);
            var glyph=marker.GetChild(0);Vector2 direction=unclamped-point;glyph.localRotation=Quaternion.Euler(0,0,direction.sqrMagnitude>1?Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90:0);
            int used=0;
            for(int i=1;i<route.Length&&used<240;i++)
            {
                Vector3 a=route[i-1]+Vector3.up*.1f,b=route[i]+Vector3.up*.1f;float length=Vector3.Distance(a,b);
                for(float t=0;t<length&&used<240;t+=.55f)
                {
                    Vector3 p=Vector3.Lerp(a,b,t/length),q=Vector3.Lerp(a,b,Mathf.Min(t+.25f,length)/length);
                    if(camera.WorldToViewportPoint(p).z<=0)continue;
                    if(used>=dashes.Count){var line=Image("GuidanceRoute",ui.BubbleLayer,new Color(.55f,.77f,.78f,.6f));line.rectTransform.pivot=new Vector2(0,.5f);dashes.Add(line.rectTransform);}
                    var dash=dashes[used++];dash.gameObject.SetActive(true);Vector2 from=ui.WorldToUI(p,camera),to=ui.WorldToUI(q,camera),delta=to-from;
                    dash.anchoredPosition=from;dash.sizeDelta=new Vector2(delta.magnitude,2.5f);dash.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                }
            }
            for(int i=used;i<dashes.Count;i++)dashes[i].gameObject.SetActive(false);
            marker.SetAsLastSibling();
        }
    }
}

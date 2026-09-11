using System.Collections.Generic;
using System.Linq;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DungeonTavern.UI.TavernUiTheme;

namespace DungeonTavern.UI
{
    public enum GuideStep { Exit, Lever, Menu, Cup, Fill, Serve, Settle, Awaken }
    // Session progress belongs to this persistent UI object; a future save includes completed.
    [DefaultExecutionOrder(1100)]
    public sealed class TavernGuidance : MonoBehaviour
    {
        static TavernGuidance instance;
        readonly HashSet<GuideStep> completed=new();
        readonly float[] elapsed=new float[8];
        [SerializeField,Min(1)] float textDelay=20,routeDelay=75;
        RectTransform notice, card, marker; Text areaTitle, areaSubtitle, title, body, keyLabel, keyAction;
        RectTransform keycap; GuidanceVisualGraphic visual;
        CanvasGroup areaAlpha, cardAlpha;
        TavernUI ui; PrototypePlayerMover player; Day1NarrativeController narrative; TavernMenuSystem menu;
        FloorLeverPoint lever; Camera camera; PlayerHands hands; Transform target;
        Vector3 lastRouteFrom=new(float.PositiveInfinity,0,0),lastRouteTo;
        Vector3[] route=System.Array.Empty<Vector3>(); float nextPoll,nextRoute,areaAge=10;
        bool inBasement,initialized,travelling; GuideStep? current;
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
            card=Image("FirstTimeGuidance",layer,new Color(.075f,.086f,.092f,.9f)).rectTransform;Place(card,-285,28,570,154,new Vector2(.5f,1));
            var accent=Image("GuidanceAccent",card,Gold);Place(accent.rectTransform,0,12,3,130);cardAlpha=card.gameObject.AddComponent<CanvasGroup>();cardAlpha.blocksRaycasts=false;
            title=Label("Title",card,"",26,Gold);Place(title.rectTransform,24,8,522,40);
            body=Label("Instruction",card,"",23);Place(body.rectTransform,24,51,522,40);
            var use=Label("KeyPrefix",card,"使用",21,Muted);Place(use.rectTransform,24,104,46,34);
            keycap=PanelRect("GuidanceKey",card);Place(keycap,80,104,122,34);
            keyLabel=Label("Key",keycap,"W A S D",21,Cream,TextAnchor.MiddleCenter);Fill(keyLabel.rectTransform,1);
            keyAction=Label("KeyAction",card,"移动",21,Muted);Place(keyAction.rectTransform,214,104,320,34);
            marker=Rect("GuidanceTarget",ui.BubbleLayer);Fill(marker);
            visual=marker.gameObject.AddComponent<GuidanceVisualGraphic>();visual.raycastTarget=false;
            PlayerAreaTransition.Started+=OnTravelStarted;PlayerAreaTransition.Completed+=OnTravelCompleted;
            notice.gameObject.SetActive(false);card.gameObject.SetActive(false);marker.gameObject.SetActive(false);
        }
        void OnDestroy()
        {
            PlayerAreaTransition.Started-=OnTravelStarted;PlayerAreaTransition.Completed-=OnTravelCompleted;
            if(instance==this)instance=null;
        }
        void OnTravelStarted(string loaded,string unloaded)
        {
            travelling=true;areaAge=10;notice.gameObject.SetActive(false);card.gameObject.SetActive(false);marker.gameObject.SetActive(false);
        }
        void OnTravelCompleted(string loaded,string unloaded)
        {
            travelling=false;ShowArea(loaded=="SealRoom_B1");PickStep();
        }
        void ShowArea(bool basement)
        {
            bool wasInitialized=initialized;initialized=true;inBasement=basement;areaAge=0;
            areaAlpha.alpha=0;notice.gameObject.SetActive(false);
            lastRouteFrom=new Vector3(float.PositiveInfinity,0,0);route=System.Array.Empty<Vector3>();
            areaTitle.text=basement?"封印之间":"地下酒馆";areaSubtitle.text=basement?"B1 · 地下层":"F1 · 酒馆大厅";
            if(wasInitialized&&!basement)Complete(GuideStep.Exit);
        }
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
                PickStep();
            }
            // Check before showing anything, every frame. A low-frequency scene poll let the old banner flash after a fade.
            bool basement=SceneManager.GetSceneByName("SealRoom_B1").isLoaded;
            if(player&&!travelling&&!ui.TransitionVisible&&(!initialized||basement!=inBasement))
            {ShowArea(basement);PickStep();}
            bool blocked=travelling||!player||!narrative||GamePauseMenu.IsPaused||ui.TransitionVisible||TavernUI.WindowOpen||narrative.State==Day1FlowState.Dialogue;
            if(!blocked)areaAge+=Time.deltaTime;
            notice.gameObject.SetActive(initialized&&!blocked&&areaAge<3.4f);
            areaAlpha.alpha=areaAge<.4f?Ease(areaAge/.4f):1-Mathf.Clamp01((areaAge-2.8f)/.6f);
            notice.anchoredPosition=new Vector2(-235,-132+8*(1-areaAlpha.alpha));
            bool actionable=!blocked&&current.HasValue&&!completed.Contains(current.Value)&&(player.MovementInputEnabled||current==GuideStep.Awaken);
            if(actionable)elapsed[(int)current.Value]+=Time.deltaTime;
            float age=current.HasValue?elapsed[(int)current.Value]:0;
            card.gameObject.SetActive(actionable&&age>=textDelay&&areaAge>=3.4f);
            cardAlpha.alpha=Ease((age-textDelay)/.3f);
            bool showRoute=actionable&&current!=GuideStep.Awaken&&age>=routeDelay&&target&&camera;
            marker.gameObject.SetActive(showRoute);
            if(showRoute)
            {
                if(Time.unscaledTime>=nextRoute&&((player.transform.position-lastRouteFrom).sqrMagnitude>.01f||(target.position-lastRouteTo).sqrMagnitude>.01f||route.Length==0))
                { nextRoute=Time.unscaledTime+.25f;lastRouteFrom=player.transform.position;lastRouteTo=target.position;route=GuidancePath.Calculate(lastRouteFrom,lastRouteTo,inBasement,player.transform); }

            }

        }
        static float Ease(float t){t=Mathf.Clamp01(t);return 1-Mathf.Pow(1-t,3);}
        void PickStep()
        {
            current=null;if(!player||!hands||!narrative)return;
            if(lever&&lever.IsOn)Complete(GuideStep.Lever);
            if(narrative.State==Day1FlowState.Awakening)
            {Set(GuideStep.Awaken,player.transform,"站起身来","撑起身体，看看周围的情况。","W A S D","起身");return;}
            if(inBasement)
            {
                if(narrative.State==Day1FlowState.AwaitingStorageReturn)
                    Set(GuideStep.Exit,FindAnyObjectByType<AdditiveScenePortal>()?.transform,"寻找出口","沿石阶返回楼上的酒馆。","W A S D","移动");
                return;
            }
            if(!narrative.ManagementUnlocked)return;
            if(!completed.Contains(GuideStep.Lever))
            {if(narrative.CanOpenTavern)Set(GuideStep.Lever,lever?.transform,"开始营业","到伊芙身旁的拉杆处打开酒馆。","F","拉动拉杆");return;}
            var guests=FindObjectsByType<CustomerServicePoint>();
            var settling=guests.FirstOrDefault(c=>c.State==CustomerOrderState.AwaitingSettlement);
            if(!completed.Contains(GuideStep.Settle)&&settling){Set(GuideStep.Settle,settling.transform,"为客人结账","走到已用餐完毕的客人身旁。","F","结账");return;}
            if(menu&&menu.PendingOrderCount>0&&!completed.Contains(GuideStep.Menu))
            {Set(GuideStep.Menu,FindAnyObjectByType<TavernMenuPoint>()?.transform,"查看订单","打开账簿查看客人的需求。","M","查看订单 · 靠近菜单也可按 F");return;}
            if(hands.CurrentItem==HeldItem.EmptyCup)
            {Set(GuideStep.Fill,FindAnyObjectByType<DrinkBarrelPoint>()?.transform,"接取酒水","带着空杯走到酒桶旁。","F","接取酒水");return;}
            if(hands.CurrentItem is HeldItem.TestDrink or HeldItem.MainDish or HeldItem.SideDish)
            {
                var guest=guests.Where(c=>c.Order!=null&&c.Order.Needs(hands.CurrentItem)&&c.State is CustomerOrderState.WaitingForFood or CustomerOrderState.Eating).OrderBy(c=>(c.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault();
                if(guest)Set(GuideStep.Serve,guest.transform,"为客人上菜","靠近需要这份餐点的客人。","F","送上餐点");return;
            }
            var cups=FindAnyObjectByType<CupDispenserPoint>();
            if(cups&&cups.CupReady)Set(GuideStep.Cup,cups.transform,"寻找酒杯","取杯器已升起酒杯，走近取杯器。","F","拿取酒杯");
        }
        void Set(GuideStep step,Transform destination,string heading,string instruction,string key,string action)
        {
            if(completed.Contains(step)||!destination)return;
            if(target!=destination){route=System.Array.Empty<Vector3>();nextRoute=0;}
            current=step;target=destination;title.text=heading;body.text=instruction;
            keyLabel.text=key;keyAction.text=action;float width=key.Length>1?122:38;
            keycap.sizeDelta=new Vector2(width,34);keyAction.rectTransform.anchoredPosition=new Vector2(92+width,-104);
        }
        void LateUpdate()
        {
            if(!RouteVisible||!player||!target||!camera)return;
            visual.Draw(ui,camera,route,player.transform.position,target.position,current??GuideStep.Exit);
        }
    }
}

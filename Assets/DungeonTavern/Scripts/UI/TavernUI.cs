using System.Collections.Generic;
using System.Linq;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Tavern25D.Narrative;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static DungeonTavern.UI.TavernUiTheme;

namespace DungeonTavern.UI
{
    [DefaultExecutionOrder(1000)]
    public sealed class TavernUI : MonoBehaviour
    {
        public static TavernUI Instance { get; private set; }
        public static bool WindowOpen => Instance != null && Instance.menu != null && Instance.menu.IsOpen;
        public bool TransitionVisible => fade != null && fade.color.a > .01f;
        public RectTransform BubbleLayer { get; private set; }
        readonly List<RectTransform> safeAreas = new();
        readonly Dictionary<Object,float> fades = new();
        readonly Vector3[] worldCorners = new Vector3[4];
        Rect lastSafe; Vector2Int lastScreen;
        RawImage world; AspectRatioFitter worldAspect;
        RectTransform hud, windows, dialogue, pauseLayer, confirmLayer, ledger, ledgerContent, dialogueContent, pausePanel;
        ScrollRect ledgerScroll, dialogueScroll;
        Image fade;
        Text money, customers, held, prompt, dialogueSpeaker, dialogueHint;
        GameObject hintPanel, heldPanel, pauseMain, controlsPage, quitPage, dialogueBox, cinematic;
        Text cinematicText;
        Button continueButton, summaryTab, ordersTab;
        TavernMenuSystem menu; BusinessDayController day; PlayerInteractionController interaction;
        Day1NarrativeController narrative; GamePauseMenu pause;
        string ledgerKey, dialogueKey;
        float nextLookup, revealAge;
        bool statusRevealed;
        RectTransform status, coinGroup, guestGroup, historyPage, historyContent, choicesContent;
        CanvasGroup statusAlpha, coinAlpha, guestAlpha;
        Text dayStatus;
        ScrollRect historyScroll;
        int historyCount=-1;
        TavernPortrait portrait;
        public float StatusOpacity => statusAlpha.alpha;
        public bool StatusRevealed => statusRevealed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() => Instance=null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(Instance!=null)return;
            var go=new GameObject("TavernUI");DontDestroyOnLoad(go);go.AddComponent<TavernUI>();
        }
        void Awake()
        {
            if(Instance!=null){Destroy(gameObject);return;} Instance=this;
            var output=Layer("00_GameOutput",-100,false);var black=Image("Letterbox",output,Color.black);Fill(black.rectTransform);
            var r=Rect("WorldTexture",output);Fill(r);world=r.gameObject.AddComponent<RawImage>();world.raycastTarget=false;
            worldAspect=r.gameObject.AddComponent<AspectRatioFitter>();worldAspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            BubbleLayer=Layer("10_WorldBubbles",10,false);
            hud=Layer("20_HUD",20);windows=Layer("40_Ledger",40);dialogue=Layer("60_Dialogue",60);
            var transition=Layer("80_Transition",80,false);fade=Image("Fade",transition,Color.clear,true);Fill(fade.rectTransform);
            pauseLayer=Layer("100_Pause",100);confirmLayer=Layer("110_Confirmation",110);
            BuildHud();BuildLedger();BuildDialogue();BuildPause();
            foreach(var view in new[]{windows.gameObject,dialogue.gameObject,pauseLayer.gameObject,confirmLayer.gameObject,
                hintPanel,heldPanel,pausePanel.gameObject,controlsPage,historyPage.gameObject,hud.gameObject,BubbleLayer.gameObject})TavernFadeIn.Add(view);
            gameObject.AddComponent<TavernGuidance>().Initialize(this,hud);
            if(EventSystem.current==null)
            {
                var events=new GameObject("UI_EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform);
            }
            UpdateSafeArea();
        }
        RectTransform Layer(string name,int order,bool safe=true)
        {
            var r=Rect(name,transform);var canvas=r.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=order;
            var scaler=r.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            r.gameObject.AddComponent<GraphicRaycaster>();
            if(!safe)return r;
            var area=Rect("SafeArea",r);Fill(area);safeAreas.Add(area);return area;
        }
        void OnDestroy() { if(Instance==this)Instance=null; }
        void UpdateSafeArea()
        {
            if(Screen.width==0||Screen.height==0)return;
            lastSafe=Screen.safeArea;lastScreen=new Vector2Int(Screen.width,Screen.height);
            foreach(var r in safeAreas){r.anchorMin=lastSafe.min/new Vector2(Screen.width,Screen.height);r.anchorMax=lastSafe.max/new Vector2(Screen.width,Screen.height);r.offsetMin=r.offsetMax=Vector2.zero;}
        }
        public void SetWorldTexture(RenderTexture texture)
        { if(world==null)return;world.texture=texture;world.enabled=texture!=null;if(texture!=null)worldAspect.aspectRatio=(float)texture.width/texture.height; }
        public void SetFade(Object owner,float alpha)
        { if(alpha>0)fades[owner]=alpha;else fades.Remove(owner); }
        public void ClearFade(Object owner)=>fades.Remove(owner);
        public Vector2 WorldToUI(Vector3 position,Camera camera)
        {
            var viewport=camera.WorldToViewportPoint(position);
            var corners=worldCorners;world.rectTransform.GetWorldCorners(corners);
            Vector2 screen=new(corners[0].x+viewport.x*(corners[2].x-corners[0].x),corners[0].y+viewport.y*(corners[2].y-corners[0].y));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(BubbleLayer,screen,null,out var local);return local;
        }
        void BuildHud()
        {
            var menuButton=Button("PauseButton",hud,"",()=>pause?.SetPaused(true));Place((RectTransform)menuButton.transform,32,28,62,62);
            TavernIcon.Add(menuButton.transform,TavernGlyph.Menu,14,14,34);
            Tooltip(menuButton.gameObject,"暂停菜单 · Esc",new Vector2(0,1),0,74,220);
            status=PanelRect("TavernStatus",hud);Place(status,-344,28,312,62,new Vector2(1,1));statusAlpha=status.gameObject.AddComponent<CanvasGroup>();statusAlpha.alpha=0;
            coinGroup=Rect("Coins",status);Place(coinGroup,16,8,157,46);coinAlpha=coinGroup.gameObject.AddComponent<CanvasGroup>();
            TavernIcon.Add(coinGroup,TavernGlyph.Coin,0,7,32);money=Label("Money",coinGroup,"0",29,Gold);Place(money.rectTransform,44,0,110,46);
            var divider=Image("Divider",status,Copper);Place(divider.rectTransform,184,14,1,34);
            guestGroup=Rect("Guests",status);Place(guestGroup,202,8,98,46);guestAlpha=guestGroup.gameObject.AddComponent<CanvasGroup>();
            TavernIcon.Add(guestGroup,TavernGlyph.Guests,0,7,32,Cream);customers=Label("ActiveCustomers",guestGroup,"0",29);Place(customers.rectTransform,44,0,52,46);
            Tooltip(status.gameObject,"金币 / 在店客人",new Vector2(1,1),-244,74,244);
            var hint=PanelRect("InteractionHint",hud);Place(hint,-430,-104,398,76,new Vector2(1,0));hintPanel=hint.gameObject;
            Key(hint,"F",18,19);prompt=Label("Action",hint,"交互",26);Place(prompt.rectTransform,80,12,298,52);
            var hand=PanelRect("HeldItem",hud);Place(hand,-354,-190,322,68,new Vector2(1,0));heldPanel=hand.gameObject;
            held=Label("Item",hand,"",24,Cream);Place(held.rectTransform,22,12,278,44);

        }
        void Tooltip(GameObject source,string caption,Vector2 anchor,float x,float y,float width)
        {
            var tip=PanelRect("Tooltip",source.transform);Place(tip,x,y,width,44,anchor);
            var label=Label("Text",tip,caption,20,Cream,TextAnchor.MiddleCenter);Fill(label.rectTransform,6);TavernFadeIn.Add(tip.gameObject);tip.gameObject.SetActive(false);
            source.GetComponent<TavernPanelGraphic>().raycastTarget=true;source.AddComponent<TavernTooltip>().View=tip.gameObject;
        }
        static float RevealEase(float t){t=Mathf.Clamp01(t);return 1-Mathf.Pow(1-t,3);}
        void RefreshStatus(bool available)
        {
            if(available&&!statusRevealed){statusRevealed=true;revealAge=0;}
            status.gameObject.SetActive(statusRevealed);
            if(!statusRevealed)return;
            if(hud.gameObject.activeInHierarchy&&!TransitionVisible)revealAge+=Time.unscaledDeltaTime;
            statusAlpha.alpha=RevealEase(revealAge/.55f);status.anchoredPosition=new Vector2(-344+12*(1-statusAlpha.alpha),-28);
            coinAlpha.alpha=RevealEase((revealAge-.18f)/.4f);guestAlpha.alpha=RevealEase((revealAge-.30f)/.4f);
            coinGroup.anchoredPosition=new Vector2(16,-8-4*(1-coinAlpha.alpha));guestGroup.anchoredPosition=new Vector2(202,-8-4*(1-guestAlpha.alpha));
        }
        void BuildLedger()
        {
            var dim=Image("Scrim",windows,new Color(0,0,0,.58f),true);Fill(dim.rectTransform);
            ledger=PanelRect("Ledger",windows,raycast:true);Place(ledger,-580,-355,1160,710,new Vector2(.5f,.5f));
            var title=Label("Title",ledger,"酒馆账簿",38,Gold);Place(title.rectTransform,42,30,580,54);
            dayStatus=Label("DayAndStatus",ledger,"Day 1 · 今日尚未开始营业",23,Muted);Place(dayStatus.rectTransform,44,90,970,34);
            var close=Button("Close",ledger,"×",()=>menu?.Close());Place((RectTransform)close.transform,1060,32,58,54);
            summaryTab=Button("SummaryTab",ledger,"菜品总览",()=>{if(menu!=null)menu.SelectedTab=0;},true);Place((RectTransform)summaryTab.transform,42,146,530,58);
            ordersTab=Button("OrdersTab",ledger,"具体订单",()=>{if(menu!=null)menu.SelectedTab=1;});Place((RectTransform)ordersTab.transform,586,146,532,58);
            ledgerScroll=Scroll("Entries",ledger,out ledgerContent);Place((RectTransform)ledgerScroll.transform,42,230,1076,398);
            Rule(ledger,42,650,1076);var foot=Label("Footer",ledger,"M / Esc  收起账簿",20,Muted);Place(foot.rectTransform,44,663,900,30);
            windows.gameObject.SetActive(false);
        }
        void BuildDialogue()
        {
            var box=PanelRect("DialogueBox",dialogue,raycast:true);dialogueBox=box.gameObject;
            box.anchorMin=new Vector2(.08f,0);box.anchorMax=new Vector2(.92f,0);box.pivot=new Vector2(.5f,0);box.anchoredPosition=new Vector2(0,30);box.sizeDelta=new Vector2(0,254);
            var tab=PanelRect("SpeakerPlate",box,new Color(.25f,.20f,.13f));Place(tab,28,-23,260,54);
            dialogueSpeaker=Label("Speaker",tab,"对话",25,Gold,TextAnchor.MiddleCenter);Fill(dialogueSpeaker.rectTransform,14);
            dialogueScroll=Scroll("Transcript",box,out dialogueContent);
            var r=(RectTransform)dialogueScroll.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(34,78);r.offsetMax=new Vector2(-350,-52);
            dialogueHint=Label("DialogueHint",box,"Enter / Space  继续",19,Muted);Place(dialogueHint.rectTransform,34,-55,650,32,new Vector2(0,0));
            continueButton=Button("Continue",box,"继续  ›",()=>narrative?.ContinueDialogue(),true);Place((RectTransform)continueButton.transform,-198,-65,164,48,new Vector2(1,0));
            var recall=Button("DialogueHistory",box,"回顾",()=>{pause?.SetPaused(true);pause?.ShowHistory(true);});Place((RectTransform)recall.transform,-320,-65,110,48,new Vector2(1,0));
            choicesContent=Rect("DialogueChoices",dialogue);var cr=choicesContent;
            cr.anchorMin=cr.anchorMax=new Vector2(.08f,0);cr.pivot=new Vector2(0,0);
            cr.anchoredPosition=new Vector2(0,318);cr.sizeDelta=new Vector2(900,320);
            portrait=gameObject.AddComponent<TavernPortrait>();portrait.Initialize(box);
            var cinema=Image("Cinematic",dialogue,new Color(.045f,.055f,.06f),true);Fill(cinema.rectTransform);cinematic=cinema.gameObject;
            var label=Label("Chapter",cinema.transform,"地 下 酒 馆",28,Gold,TextAnchor.MiddleCenter);Place(label.rectTransform,-400,-220,800,80,new Vector2(.5f,.5f));
            cinematicText=Label("Narration",cinema.transform,"",32,Cream,TextAnchor.MiddleCenter);Place(cinematicText.rectTransform,-600,-65,1200,200,new Vector2(.5f,.5f));
            var next=Button("Continue",cinema.transform,"继续  ›",()=>narrative?.ContinueDialogue(),true);Place((RectTransform)next.transform,-100,200,200,56,new Vector2(.5f,.5f));
            dialogue.gameObject.SetActive(false);
        }
        void BuildPause()
        {
            var dim=Image("Scrim",pauseLayer,new Color(.025f,.035f,.045f,.82f),true);Fill(dim.rectTransform);
            pausePanel=PanelRect("PausePanel",pauseLayer,raycast:true);Place(pausePanel,-300,-405,600,810,new Vector2(.5f,.5f));
            var emblem=Label("Emblem",pausePanel,"◆",30,Gold,TextAnchor.MiddleCenter);Place(emblem.rectTransform,240,26,120,46);
            var title=Label("Title",pausePanel,"地 下 酒 馆",36,Cream,TextAnchor.MiddleCenter);Place(title.rectTransform,40,84,520,65);
            var sub=Label("Paused",pausePanel,"炉火未熄 · 片刻休息",21,Muted,TextAnchor.MiddleCenter);Place(sub.rectTransform,40,153,520,38);Rule(pausePanel,50,214,500);
            pauseMain=Rect("MainPage",pausePanel).gameObject;Fill((RectTransform)pauseMain.transform);
            string[] labels={"保存进度    ·    暂不可用","读取进度    ·    暂不可用","操作说明","退出游戏","对话历史"};
            for(int i=0;i<labels.Length;i++)
            {
                int index=i;var b=Button("Option"+i,pauseMain.transform,labels[i],()=>{if(index==2)pause?.ShowControls();if(index==3)pause?.ShowQuit();if(index==4)pause?.ShowHistory();});Place((RectTransform)b.transform,66,238+i*82,468,64);b.interactable=i>=2;
            }
            var resume=Button("Resume",pauseMain.transform,"Esc   返回酒馆",()=>pause?.SetPaused(false),true);Place((RectTransform)resume.transform,66,704,468,54);
            controlsPage=PanelRect("Controls",pauseLayer,raycast:true).gameObject;Place((RectTransform)controlsPage.transform,-420,-395,840,790,new Vector2(.5f,.5f));
            var controlsTitle=Label("Title",controlsPage.transform,"操作说明",36,Gold);Place(controlsTitle.rectTransform,44,28,680,65);Rule(controlsPage.transform,44,111,752);
            string[] keys={"WASD","Q / E","F","Space","M","C","Enter","鼠标","Esc"};
            string[] descriptions={"移动角色","切换四个观察方向","与附近人物或物品交互","朝吧台移动时翻越","打开 / 收起酒馆账簿","跟随排队客人，再按返回","继续对话，也可按 Space","点击选择对话选项","暂停游戏 / 返回上一页"};
            for(int i=0;i<keys.Length;i++){Key(controlsPage.transform,keys[i],44,139+i*55,112);var t=Label("Control"+i,controlsPage.transform,descriptions[i],24);Place(t.rectTransform,181,133+i*55,610,50);}
            var back=Button("Back",controlsPage.transform,"返回",()=>pause?.Back(),true);Place((RectTransform)back.transform,44,699,752,56);
            historyPage=PanelRect("DialogueHistoryPage",pauseLayer,raycast:true);Place(historyPage,-570,-400,1140,800,new Vector2(.5f,.5f));
            var historyTitle=Label("Title",historyPage,"对话历史",36,Gold);Place(historyTitle.rectTransform,42,24,950,65);Rule(historyPage,42,103,1056);
            historyScroll=Scroll("History",historyPage,out historyContent);Place((RectTransform)historyScroll.transform,42,130,1056,556);
            var historyBack=Button("HistoryBack",historyPage,"返回",()=>pause?.Back(),true);Place((RectTransform)historyBack.transform,42,715,1056,54);
            var confirmDim=Image("Scrim",confirmLayer,new Color(0,0,0,.54f),true);Fill(confirmDim.rectTransform);
            quitPage=PanelRect("QuitConfirmation",confirmLayer,raycast:true).gameObject;Place((RectTransform)quitPage.transform,-320,-204,640,408,new Vector2(.5f,.5f));
            var qt=Label("Title",quitPage.transform,"要离开酒馆吗？",34,Gold);Place(qt.rectTransform,40,28,560,65);Rule(quitPage.transform,40,106,560);
            var message=Label("Message",quitPage.transform,"当前 Demo 暂不支持保存进度。\n退出后，本次进度不会保留。",26);Place(message.rectTransform,40,128,560,126);
            var cancel=Button("Cancel",quitPage.transform,"留在酒馆",()=>pause?.Back(),true);Place((RectTransform)cancel.transform,40,300,270,62);
            var quit=Button("ConfirmQuit",quitPage.transform,"确认退出",()=>pause?.ConfirmExit());Place((RectTransform)quit.transform,330,300,270,62);
            pauseLayer.gameObject.SetActive(false);confirmLayer.gameObject.SetActive(false);
        }
        void LateUpdate()
        {
            if(lastSafe!=Screen.safeArea||lastScreen!=new Vector2Int(Screen.width,Screen.height))UpdateSafeArea();
            if(Time.unscaledTime>=nextLookup)
            {
                nextLookup=Time.unscaledTime+.5f;
                if(menu==null)menu=FindAnyObjectByType<TavernMenuSystem>();if(day==null)day=FindAnyObjectByType<BusinessDayController>();
                if(interaction==null)interaction=FindAnyObjectByType<PlayerInteractionController>();if(narrative==null)narrative=FindAnyObjectByType<Day1NarrativeController>();if(pause==null)pause=FindAnyObjectByType<GamePauseMenu>();
            }
            bool talking=narrative!=null&&narrative.isActiveAndEnabled&&narrative.State==Day1FlowState.Dialogue;
            bool paused=GamePauseMenu.IsPaused;
            TavernFadeIn.Show(hud.gameObject,!talking&&!paused);TavernFadeIn.Show(BubbleLayer.gameObject,!talking);
            RefreshStatus(narrative!=null&&narrative.ManagementUnlocked);
            money.text=menu==null?"—":menu.Balance.ToString("N0");customers.text=(day==null?0:day.ActiveCustomers).ToString();
            string action=interaction==null||!interaction.enabled?"":interaction.CurrentPrompt;
            TavernFadeIn.Show(hintPanel,!string.IsNullOrEmpty(action)&&!WindowOpen);if(!string.IsNullOrEmpty(action))prompt.text=action.Replace("F：","").Replace("F:","").Trim();
            var item=interaction==null?HeldItem.None:interaction.CurrentItem;TavernFadeIn.Show(heldPanel,item!=HeldItem.None&&!WindowOpen);if(item!=HeldItem.None)held.text="手持  ·  "+TavernMenuSystem.GetLabel(item);
            TavernFadeIn.Show(windows.gameObject,WindowOpen&&!talking);if(WindowOpen)RefreshLedger();
            TavernFadeIn.Show(dialogue.gameObject,talking);if(talking)RefreshDialogue();else dialogueKey=null;
            TavernFadeIn.Show(pauseLayer.gameObject,paused);TavernFadeIn.Show(confirmLayer.gameObject,paused&&pause!=null&&pause.ConfirmingQuit);
            TavernFadeIn.Show(pausePanel.gameObject,pause==null||(!pause.ControlsVisible&&!pause.HistoryVisible));TavernFadeIn.Show(controlsPage,pause!=null&&pause.ControlsVisible);
            TavernFadeIn.Show(historyPage.gameObject,pause!=null&&pause.HistoryVisible);if(paused&&pause!=null&&pause.HistoryVisible)RefreshHistory();
            float alpha=0;foreach(var pair in fades)if(pair.Key!=null)alpha=Mathf.Max(alpha,pair.Value);
            fade.color=new Color(0,0,0,alpha);fade.raycastTarget=alpha>0;fade.gameObject.SetActive(alpha>0);
        }
        void RefreshLedger()
        {
            var lever=FindAnyObjectByType<FloorLeverPoint>(FindObjectsInactive.Include);
            string state=lever==null?"今日尚未开始营业":lever.IsSwitching?(lever.IsOn?"准备营业":"正在打烊"):lever.IsOn?"营业中":lever.HasOpened?"今日已结束营业":"今日尚未开始营业";
            dayStatus.text="Day 1 · "+state;
            string key=menu.SelectedTab+":"+string.Join("|",menu.Dishes.Select(d=>$"{d.label}/{d.price}/{menu.Count(d.item)}/{menu.GetCustomers(d.item).Count}"));
            if(key==ledgerKey)return;ledgerKey=key;Clear(ledgerContent);
            summaryTab.GetComponent<TavernPanelGraphic>().color=menu.SelectedTab==0?new Color(.31f,.24f,.145f):Panel;
            ordersTab.GetComponent<TavernPanelGraphic>().color=menu.SelectedTab==1?new Color(.31f,.24f,.145f):Panel;
            summaryTab.GetComponentInChildren<Text>().color=menu.SelectedTab==0?Gold:Muted;
            ordersTab.GetComponentInChildren<Text>().color=menu.SelectedTab==1?Gold:Muted;
            if(menu.SelectedTab==1)
            {
                var icon=Label("Seal",ledgerContent,"◇",80,Gold,TextAnchor.MiddleCenter);Place(icon.rectTransform,420,30,200,100);
                var empty=Label("EmptyOrders",ledgerContent,"订单记录\n\n详细订单将在后续版本开放。",27,Muted,TextAnchor.MiddleCenter);Place(empty.rectTransform,120,140,836,150);ledgerContent.sizeDelta=new Vector2(0,330);return;
            }
            float[] columns={20,530,710,900};string[] headings={"菜品","单价","待上份数","点单人数"};
            for(int i=0;i<4;i++){var t=Label("Heading"+i,ledgerContent,headings[i],22,Muted);Place(t.rectTransform,columns[i],0,i==0?470:156,48);}
            Rule(ledgerContent,0,56,1070);int row=0;
            foreach(var dish in menu.Dishes)
            {
                float y=72+row*78;var stripe=Image("Row"+row,ledgerContent,row%2==0?new Color(1,1,1,.035f):Color.clear);Place(stripe.rectTransform,0,y,1070,70);
                TavernIcon.Add(stripe.transform,dish.item==HeldItem.TestDrink?TavernGlyph.Cup:dish.item==HeldItem.SideDish?TavernGlyph.SideDish:TavernGlyph.Dish,16,16,36);
                string[] values={dish.label,dish.price+" G",menu.Count(dish.item).ToString(),menu.GetCustomers(dish.item).Count.ToString()};
                for(int i=0;i<4;i++){var t=Label("Value"+i,stripe.transform,values[i],27,i==1?Gold:Cream);Place(t.rectTransform,i==0?72:columns[i],8,i==0?416:156,54);}row++;
            }
            ledgerContent.sizeDelta=new Vector2(0,Mathf.Max(398,80+row*78));ledgerScroll.verticalNormalizedPosition=1;
        }
        void RefreshHistory()
        {
            int count=narrative==null?0:narrative.FullHistory.Count;if(count==historyCount)return;historyCount=count;Clear(historyContent);
            Canvas.ForceUpdateCanvases();float width=Mathf.Max(300,historyContent.rect.width)-20;
            var t=Label("HistoryText",historyContent,count==0?"还没有对话记录。":string.Join("\n\n",narrative.FullHistory),26);
            Place(t.rectTransform,0,0,width,100);float height=Mathf.Max(100,t.preferredHeight+24);t.rectTransform.sizeDelta=new Vector2(width,height);historyContent.sizeDelta=new Vector2(0,height);historyScroll.verticalNormalizedPosition=0;
        }
        internal static string StripOuterQuotes(string text)
        {
            text=text.Trim();
            return text.Length>=2&&((text[0]=='“'&&text[^1]=='”')||(text[0]=='"'&&text[^1]=='"'))?text.Substring(1,text.Length-2):text;
        }
        void RefreshDialogue()
        {
            bool isCinema=narrative.IsCinematic;cinematic.SetActive(isCinema);dialogueBox.SetActive(!isCinema);
            choicesContent.gameObject.SetActive(!isCinema&&narrative.ChoiceTexts.Count>0);
            cinematicText.text=narrative.CurrentLine;
            string key=narrative.PresentedLine+"|"+string.Join("|",narrative.ChoiceTexts);
            if(dialogueKey==key)return;dialogueKey=key;Clear(dialogueContent);Clear(choicesContent);
            Canvas.ForceUpdateCanvases();float width=Mathf.Max(300,dialogueContent.rect.width);float y=0;
            string text=narrative.PresentedLine;dialogueSpeaker.text=narrative.SpeakerLabel;
            int colon=text.IndexOf('：');
            if(colon>0&&colon<12)text=text.Substring(colon+1).Trim();
            text=StripOuterQuotes(text);
            var speakerPlate=(RectTransform)dialogueSpeaker.transform.parent;
            speakerPlate.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Max(64,dialogueSpeaker.preferredWidth+40));
            if(!string.IsNullOrWhiteSpace(text))
            {
                var t=Label("Speech",dialogueContent,text,28);Place(t.rectTransform,0,0,width-20,100);
                float height=Mathf.Max(60,t.preferredHeight+12);t.rectTransform.sizeDelta=new Vector2(width-20,height);y=height+16;
            }
            dialogueContent.sizeDelta=new Vector2(0,y);dialogueScroll.verticalNormalizedPosition=1;
            width=Mathf.Min(900,dialogue.rect.width*.58f);y=0;var choices=narrative.ChoiceTexts;
            for(int i=0;i<choices.Count;i++)
            {
                int index=i;var b=Button("Choice"+i,choicesContent,StripOuterQuotes(choices[i]),()=>narrative?.SelectChoice(index));
                var label=b.GetComponentInChildren<Text>();label.alignment=TextAnchor.MiddleCenter;label.fontSize=26;
                float height=Mathf.Max(72,Mathf.Ceil(label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text,label.GetGenerationSettings(new Vector2(width-60,0)))/label.pixelsPerUnit)+22);
                Place((RectTransform)b.transform,0,y,width-20,height);y+=height+16;TavernFadeIn.Add(b.gameObject);
            }
            choicesContent.sizeDelta=new Vector2(width,Mathf.Max(72,y-16));
            continueButton.gameObject.SetActive(choices.Count==0);dialogueHint.text=choices.Count==0?"Enter / Space  继续":"点击选项回应";
            Transform actor=narrative.SpeakerLabel=="你"?interaction?.transform:narrative.DialogueActor;
            portrait.Show(actor,!isCinema);
        }
    }
}

using System;
using DungeonTavern.Tavern25D;
using System.Collections;
using System.Collections.Generic;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using Ink.Runtime;
using Ink.UnityIntegration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTavern.Tavern25D.Narrative
{
    public enum Day1FlowState
    {
        Uninitialized,
        Dialogue,
        AwaitingStorageReturn,
        AwaitingEveInteraction,
        AwaitingOpeningSwitch,
        ServingBran,
        AwaitingClosingSwitch,
        Completed,
        Awakening
    }

    public sealed class Day1NarrativeController : MonoBehaviour
    {
        private readonly struct DialogueEntry
        {
            public DialogueEntry(string text, bool isPlayer) { Text = text; IsPlayer = isPlayer; }
            public string Text { get; }
            public bool IsPlayer { get; }
        }

        [Header("Narrative")]
        [SerializeField] private InkFile chapterOne;
        [SerializeField] private bool skipOpeningCinematic = true;

        [Header("Scene References")]
        [SerializeField] private PrototypePlayerMover player;
        [SerializeField] private Transform storageArrival;
        [SerializeField] private BusinessDayController businessDay;
        [SerializeField] private Day1EveActor eve;
        [SerializeField] private Transform eveOpeningGuidePoint;
        [SerializeField, Min(0.5f)] private float storageArrivalRadius = 1.6f;

        private readonly List<Choice> choices = new();
        private readonly List<DialogueEntry> dialogueHistory = new();
        private Story story;
        private Story choicePreview;
        private string pendingChoiceState;
        private bool singleChoicePresented;
        private readonly List<string> fullHistory = new();
        public IReadOnlyList<string> FullHistory => fullHistory;
        public bool ManagementUnlocked { get; private set; }
        public Transform DialogueActor { get; private set; }
        public string PresentedLine => !string.IsNullOrEmpty(CurrentLine) ? CurrentLine : dialogueHistory.Count > 0 ? dialogueHistory[^1].Text : string.Empty;
        private CustomerServicePoint bran;
        private string currentLine;
        private bool openingCinematic = true;
        private bool showingCinematic;
        private bool closeDialogueActive;
        private PrototypeCameraOrbit cameraOrbit;
        private string pendingBranBubble;
        private string pendingClosingBubble;

        public Day1FlowState State { get; private set; }
        public bool CanOpenTavern => State == Day1FlowState.AwaitingOpeningSwitch
            && eve != null
            && eve.IsOpeningGuidanceReady;
        public bool ClosingTimeAnnounced { get; private set; }
        public bool CanCloseTavern => State == Day1FlowState.AwaitingClosingSwitch
            && businessDay != null
            && businessDay.State == BusinessDayState.Completed && ClosingTimeAnnounced;
        public int CurrentChoiceCount => choices.Count>1?choices.Count:0;
        public bool IsCinematic => showingCinematic;
        public string CurrentLine => currentLine ?? string.Empty;
        public string DisplayText => dialogueHistory.Count > 0
            ? string.Join("\n\n", dialogueHistory.ConvertAll(entry=>entry.Text)) : CurrentLine;
        public IReadOnlyList<string> ChoiceTexts => choices.Count>1?choices.ConvertAll(choice=>choice.text):Array.Empty<string>();
        public string SpeakerLabel
        {
            get
            {
                string line=PresentedLine;
                if(string.IsNullOrEmpty(line)&&dialogueHistory.Count>0)line=dialogueHistory[dialogueHistory.Count-1].Text;
                int colon=line.IndexOf('：');
                return colon>0&&colon<12 ? line.Substring(0,colon) : "酒馆纪事";
            }
        }
        public void ContinueDialogue()
        {
            if(GamePauseMenu.IsPaused||State!=Day1FlowState.Dialogue||choices.Count>1)return;
            if(choices.Count==1)
            {if(singleChoicePresented)Choose(0);else PresentSingleChoice();}
            else ShowNextContent();
        }
        public void SelectChoice(int index) { if(!GamePauseMenu.IsPaused&&State==Day1FlowState.Dialogue&&index>=0&&index<choices.Count)Choose(index); }

        public void Configure(
            InkFile storyAsset,
            PrototypePlayerMover playerMover,
            Transform storagePoint,
            BusinessDayController day,
            Day1EveActor eveActor = null,
            Transform openingGuidePoint = null)
        {
            chapterOne = storyAsset;
            player = playerMover;
            storageArrival = storagePoint;
            businessDay = day;
            eve = eveActor;
            eveOpeningGuidePoint = openingGuidePoint;
        }

        public void SetOpeningGuidePoint(Transform guidePoint)
        {
            eveOpeningGuidePoint = guidePoint;
        }

        private IEnumerator Start()
        {
            if (chapterOne == null || string.IsNullOrWhiteSpace(chapterOne.storyJson))
            {
                Debug.LogError("Day 1 narrative requires a compiled Chapter01 InkFile.", this);
                enabled = false;
                yield break;
            }
            while (player == null)
            {
                player = FindAnyObjectByType<PrototypePlayerMover>();
                yield return null;
            }
            if (storageArrival == null || businessDay == null || eve == null)
            {
                Debug.LogError("Day 1 narrative scene references are incomplete.", this);
                enabled = false;
                yield break;
            }

            businessDay.CustomerSpawned += OnCustomerSpawned;
            businessDay.DayCompleted += OnBusinessDayCompleted;
            eve.ConversationRequested += OnEveConversationRequested;
            player.GetComponentInChildren<CharacterModelMotion>()?.BeginProne();
            story = new Story(chapterOne.storyJson);
            openingCinematic = !skipOpeningCinematic;
            story.ChoosePathString(skipOpeningCinematic ? "day01_open" : "prologue");
            ShowNextContent();
        }

        private IEnumerator AwakenPlayer()
        {
            var motion = player.GetComponentInChildren<CharacterModelMotion>();
            player.MovementInputEnabled = false;
            if (motion)
            {
                motion.BeginProne();
                while (GamePauseMenu.IsPaused || DungeonTavern.UI.TavernUI.WindowOpen || Keyboard.current == null || !(Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed || Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed))
                    yield return null;
                DungeonTavern.UI.TavernGuidance.Complete(DungeonTavern.UI.GuideStep.Awaken);
                yield return motion.WakeAndStand();
            }
            player.MovementInputEnabled = true;
            State = Day1FlowState.AwaitingStorageReturn;
        }

        private void OnDestroy()
        {
            if (businessDay != null)
            {
                businessDay.CustomerSpawned -= OnCustomerSpawned;
                businessDay.DayCompleted -= OnBusinessDayCompleted;
            }
            if (bran != null)
                bran.SettlementRequested -= OnSettlementRequested;
            if (eve != null)
                eve.ConversationRequested -= OnEveConversationRequested;
        }

        private void Update()
        {
            if (DungeonTavern.Gameplay.Interaction.GamePauseMenu.IsPaused) return;
            TryAnnounceClosingTime();
            if (State == Day1FlowState.AwaitingStorageReturn
                && storageArrival.gameObject.activeInHierarchy
                && (player.transform.position - storageArrival.position).sqrMagnitude
                    <= storageArrivalRadius * storageArrivalRadius)
            {
                State = Day1FlowState.AwaitingEveInteraction;
                eve.BeginArrival();
                return;
            }

            if (State != Day1FlowState.Dialogue)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                ContinueDialogue();

        }

        public bool TryUseBusinessSwitch()
        {
            if (CanOpenTavern)
            {
                eve.CompleteOpeningSwitchGuidance();
                SelectExternalGate();
                return true;
            }
            if (CanCloseTavern)
            {
                businessDay.StopAcceptingCustomers();
                SelectExternalGate();
                State = Day1FlowState.Completed;
                SetDialogueActive(false);
                Debug.Log("Day 1 narrative complete.", this);
                return true;
            }
            return false;
        }

        public bool AdvanceForValidation(bool chooseLastChoice = true)
        {
            if (State != Day1FlowState.Dialogue)
                return false;

            if (choices.Count == 0)
                ShowNextContent();
            else
                Choose(chooseLastChoice ? choices.Count - 1 : 0);
            return true;
        }

        private void OnBusinessDayCompleted() => TryAnnounceClosingTime();
        private void TryAnnounceClosingTime()
        {
            if(ClosingTimeAnnounced||string.IsNullOrEmpty(pendingClosingBubble)
                ||State!=Day1FlowState.AwaitingClosingSwitch||businessDay.State!=BusinessDayState.Completed
                ||!eve.gameObject.activeInHierarchy||GamePauseMenu.IsPaused
                ||DungeonTavern.UI.TavernUI.WindowOpen||DungeonTavern.UI.TavernUI.Instance.TransitionVisible)return;
            var lever=FindAnyObjectByType<FloorLeverPoint>();
            if(lever==null||!lever.IsOn||lever.IsSwitching)return;
            eve.ShowBubble(pendingClosingBubble);
            pendingClosingBubble=null;ClosingTimeAnnounced=true;
        }

        private void OnCustomerSpawned(CustomerServicePoint customer)
        {
            if (!string.Equals(customer.CustomerName, "Bran", StringComparison.OrdinalIgnoreCase))
                return;

            bran = customer;
            if (bran.GetComponent<WorldSpeechBubble>() == null)
                bran.gameObject.AddComponent<WorldSpeechBubble>();
            if (!string.IsNullOrWhiteSpace(pendingBranBubble))
            {
                bran.GetComponent<WorldSpeechBubble>().Show(pendingBranBubble);
                pendingBranBubble = string.Empty;
            }
            bran.SettlementRequested += OnSettlementRequested;
        }

        private void OnEveConversationRequested()
        {
            if (State == Day1FlowState.AwaitingEveInteraction)
            {
                BeginCloseDialogue(eve.transform);
                SelectExternalGate();
            }
        }

        private bool OnSettlementRequested(CustomerServicePoint customer)
        {
            if (State != Day1FlowState.ServingBran || customer != bran)
                return false;

            customer.GetComponent<WorldSpeechBubble>()?.Hide();
            BeginCloseDialogue(customer.transform);
            SelectExternalGate();
            return true;
        }

        private void BeginCloseDialogue(Transform speaker)
        {
            dialogueHistory.Clear();
            DialogueActor = speaker;
            closeDialogueActive = true;
            SetDialogueActive(true);
            cameraOrbit ??= FindAnyObjectByType<PrototypeCameraOrbit>();
            cameraOrbit?.BeginDialogueFraming(player.transform, speaker);
            StartCoroutine(player.TurnToward(speaker.GetComponent<CustomerServicePoint>() is {} guest?guest.ServicePosition:speaker.position));
            StartCoroutine(TurnSpeaker(speaker));
        }

        private IEnumerator TurnSpeaker(Transform speaker)
        {
            var guest=speaker.GetComponent<CustomerServicePoint>();
            if(guest)guest.DialogueFacing=true;
            speaker.GetComponent<NpcNavigator>()?.Stop();
            var direction=Vector3.ProjectOnPlane(player.transform.position-(guest?guest.ServicePosition:speaker.position),Vector3.up);
            if(direction.sqrMagnitude>.001f)
            {
                var from=speaker.rotation;var to=Quaternion.LookRotation(direction);
                float duration=Mathf.Clamp(Quaternion.Angle(from,to)/180f,.35f,1f);
                for(float t=0;t<duration;t+=Time.deltaTime)
                {if(!speaker)yield break;if(!closeDialogueActive)break;speaker.rotation=Quaternion.Slerp(from,to,Mathf.SmoothStep(0,1,t/duration));yield return null;}
                if(closeDialogueActive)speaker.rotation=to;
            }
            while(closeDialogueActive && State==Day1FlowState.Dialogue)yield return null;
            if(guest)guest.DialogueFacing=false;
        }

        private void ShowNextContent()
        {
            choices.Clear();pendingChoiceState=null;singleChoicePresented=false;
            currentLine = string.Empty;

            if (story.canContinue)
            {
                currentLine = story.Continue().Trim();
                if (currentLine.Length == 0)
                {
                    ShowNextContent();
                    return;
                }
                fullHistory.Add(currentLine);
                if (TryPresentAsBubble(currentLine))
                {
                    ShowNextContent();
                    return;
                }
                showingCinematic = openingCinematic || currentLine.Contains("黑斗篷遮住了你的脸", StringComparison.Ordinal);
                if (closeDialogueActive && !showingCinematic)
                    dialogueHistory.Add(new DialogueEntry(currentLine, IsPlayerLine(currentLine)));
                State = Day1FlowState.Dialogue;
                SetDialogueActive(true);
                PrepareUpcomingChoices();
                return;
            }

            choices.AddRange(story.currentChoices);
            if (TryEnterExternalGate())
                return;

            if (choices.Count > 0)
            {
                State = Day1FlowState.Dialogue;SetDialogueActive(true);
                if(choices.Count==1)PresentSingleChoice();
            }
        }

        private void PrepareUpcomingChoices()
        {
            // Preview in an independent Ink state: inspect choices without executing branches or changing live variables.
            choicePreview??=new Story(chapterOne.storyJson);
            choicePreview.state.LoadJson(story.state.ToJson());
            while(choicePreview.canContinue)
                if(!string.IsNullOrWhiteSpace(choicePreview.Continue()))return;
            var upcoming=choicePreview.currentChoices;
            if(upcoming.Count==0||(upcoming.Count==1&&upcoming[0].text.Contains("Inky 预览",StringComparison.Ordinal)))return;
            pendingChoiceState=choicePreview.state.ToJson();choices.AddRange(upcoming);
        }
        private void PresentSingleChoice()
        {
            singleChoicePresented=true;
            currentLine=PlayerChoiceLine(choices[0].text);
            fullHistory.Add(currentLine);
            if(closeDialogueActive)dialogueHistory.Add(new DialogueEntry(currentLine,true));
            showingCinematic=false;State=Day1FlowState.Dialogue;SetDialogueActive(true);
        }
        private static string PlayerChoiceLine(string text)
        {
            text=text.Trim();
            return text.StartsWith("你：",StringComparison.Ordinal)||text.StartsWith("你:",StringComparison.Ordinal)?text:$"你：{text}";
        }

        private bool TryEnterExternalGate()
        {
            if (choices.Count != 1 || !choices[0].text.Contains("Inky 预览", StringComparison.Ordinal))
                return false;

            string gate = choices[0].text;
            currentLine = string.Empty;
            SetDialogueActive(false);
            closeDialogueActive = false;

            if (gate.Contains("主角起身", StringComparison.Ordinal))
            {
                openingCinematic = false;
                showingCinematic = false;
                State = Day1FlowState.Awakening;
                StartCoroutine(AwakenPlayer());
            }
            else if (gate.Contains("营业拉杆", StringComparison.Ordinal))
            {
                ManagementUnlocked = true;
                State = Day1FlowState.AwaitingOpeningSwitch;
            }
            else if (gate.Contains("完成第一日营业", StringComparison.Ordinal))
            {
                State = Day1FlowState.ServingBran;
                if (!businessDay.BeginDay())
                    Debug.LogError("Day 1 could not start the business day.", this);
            }
            else if (gate.Contains("结束第一日营业", StringComparison.Ordinal))
            {
                State = Day1FlowState.AwaitingClosingSwitch;
                bran?.CompleteSettlement();
            }
            else
            {
                Debug.LogError($"Unknown Day 1 external Ink gate: {gate}", this);
            }
            return true;
        }

        private bool TryPresentAsBubble(string line)
        {
            if (line.Contains("然后我们重新开始营业吧", StringComparison.Ordinal))
            {
                eve.BeginOpeningSwitchGuidance(eveOpeningGuidePoint, ExtractBubbleText(line));
                return true;
            }
            if (line.Contains("门口的牌子终于翻回来了", StringComparison.Ordinal))
            {
                string bubbleText = ExtractBubbleText(line);
                if (bran == null)
                    pendingBranBubble = bubbleText;
                else
                    bran.GetComponent<WorldSpeechBubble>()?.Show(bubbleText);
                return true;
            }
            if (line.Contains("今天差不多了，就到这里吧", StringComparison.Ordinal)||line.Contains("最后一位客人也走了", StringComparison.Ordinal))
            {
                pendingClosingBubble = "最后一位客人也走了，今晚不会再有客人来了。到歇业的时候了，去拉下拉杆关门吧。";
                return true;
            }
            return false;
        }

        private static string ExtractBubbleText(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return string.Empty;
            string text = source.Trim();
            int separator = text.IndexOf('：');
            if (separator < 0)
                separator = text.IndexOf(':');
            if (separator >= 0 && separator + 1 < text.Length)
                text = text[(separator + 1)..].Trim();
            return text.Trim('“', '”', '"', ' ', '\t', '\r', '\n');
        }

        private void SelectExternalGate()
        {
            if (story.currentChoices.Count != 1)
            {
                Debug.LogError("Day 1 expected exactly one external Ink gate.", this);
                return;
            }

            story.ChooseChoiceIndex(0);
            ShowNextContent();
        }

        private void Choose(int index)
        {
            if (index < 0 || index >= choices.Count)
                return;
            if (closeDialogueActive&&!singleChoicePresented)
            {
                string selectedText=PlayerChoiceLine(choices[index].text);
                dialogueHistory.Add(new DialogueEntry(selectedText,true));fullHistory.Add(selectedText);
            }
            if(pendingChoiceState!=null)story.state.LoadJson(pendingChoiceState);
            story.ChooseChoiceIndex(choices[index].index);
            ShowNextContent();
        }

        private void SetDialogueActive(bool active)
        {
            if(active)FindAnyObjectByType<TavernMenuSystem>()?.Close();
            if (player != null)
                player.MovementInputEnabled = !active;
            if (!active)
            {
                cameraOrbit ??= FindAnyObjectByType<PrototypeCameraOrbit>();
                cameraOrbit?.EndDialogueFraming();
            }
        }



        private static bool IsPlayerLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            string trimmed = line.TrimStart();
            return trimmed.StartsWith("你：", StringComparison.Ordinal)
                || trimmed.StartsWith("你:", StringComparison.Ordinal);
        }


    }
}

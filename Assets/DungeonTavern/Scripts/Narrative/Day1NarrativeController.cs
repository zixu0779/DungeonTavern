using System;
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
        Completed
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
        private CustomerServicePoint bran;
        private string currentLine;
        private GUIStyle dialoguePanelStyle;
        private GUIStyle dialogueStyle;
        private GUIStyle choiceStyle;
        private GUIStyle cinematicStyle;
        private bool openingCinematic = true;
        private bool showingCinematic;
        private bool closeDialogueActive;
        private PrototypeCameraOrbit cameraOrbit;
        private Vector2 dialogueScroll;
        private int renderedHistoryCount = -1;
        private int renderedChoiceCount = -1;
        private string pendingBranBubble;

        public Day1FlowState State { get; private set; }
        public bool CanOpenTavern => State == Day1FlowState.AwaitingOpeningSwitch
            && eve != null
            && eve.IsOpeningGuidanceReady;
        public bool CanCloseTavern => State == Day1FlowState.AwaitingClosingSwitch
            && businessDay != null
            && businessDay.State == BusinessDayState.Completed;
        public int CurrentChoiceCount => choices.Count;

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
            eve.ConversationRequested += OnEveConversationRequested;
            story = new Story(chapterOne.storyJson);
            story.ChoosePathString("prologue");
            ShowNextContent();
        }

        private void OnDestroy()
        {
            if (businessDay != null)
                businessDay.CustomerSpawned -= OnCustomerSpawned;
            if (bran != null)
                bran.SettlementRequested -= OnSettlementRequested;
            if (eve != null)
                eve.ConversationRequested -= OnEveConversationRequested;
        }

        private void Update()
        {
            if (State == Day1FlowState.AwaitingStorageReturn
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

            if (choices.Count == 0 && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                ShowNextContent();
            else if (choices.Count > 0)
                TryKeyboardChoice(keyboard);
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
            dialogueScroll = Vector2.zero;
            renderedHistoryCount = -1;
            renderedChoiceCount = -1;
            closeDialogueActive = true;
            SetDialogueActive(true);
            cameraOrbit ??= FindAnyObjectByType<PrototypeCameraOrbit>();
            cameraOrbit?.BeginDialogueFraming(player.transform, speaker);
        }

        private void ShowNextContent()
        {
            choices.Clear();
            currentLine = string.Empty;

            if (story.canContinue)
            {
                currentLine = story.Continue().Trim();
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
                return;
            }

            choices.AddRange(story.currentChoices);
            if (TryEnterExternalGate())
                return;

            if (choices.Count > 0)
            {
                State = Day1FlowState.Dialogue;
                SetDialogueActive(true);
            }
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
                State = Day1FlowState.AwaitingStorageReturn;
            }
            else if (gate.Contains("营业按钮", StringComparison.Ordinal)
                     || gate.Contains("营业吊绳", StringComparison.Ordinal))
                State = Day1FlowState.AwaitingOpeningSwitch;
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
            if (line.Contains("今天差不多了，就到这里吧", StringComparison.Ordinal))
            {
                eve.ShowBubble(ExtractBubbleText(line));
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
            if (closeDialogueActive)
            {
                string selectedText = choices[index].text.Trim();
                if (!selectedText.StartsWith("你：", StringComparison.Ordinal)
                    && !selectedText.StartsWith("你:", StringComparison.Ordinal))
                {
                    selectedText = $"你：{selectedText}";
                }
                dialogueHistory.Add(new DialogueEntry(selectedText, true));
                renderedHistoryCount = -1;
            }
            story.ChooseChoiceIndex(choices[index].index);
            ShowNextContent();
        }

        private void TryKeyboardChoice(Keyboard keyboard)
        {
            if (choices.Count > 0 && keyboard.digit1Key.wasPressedThisFrame) Choose(0);
            else if (choices.Count > 1 && keyboard.digit2Key.wasPressedThisFrame) Choose(1);
            else if (choices.Count > 2 && keyboard.digit3Key.wasPressedThisFrame) Choose(2);
            else if (choices.Count > 3 && keyboard.digit4Key.wasPressedThisFrame) Choose(3);
            else if (choices.Count > 4 && keyboard.digit5Key.wasPressedThisFrame) Choose(4);
        }

        private void SetDialogueActive(bool active)
        {
            if (player != null)
                player.MovementInputEnabled = !active;
            if (!active)
            {
                cameraOrbit ??= FindAnyObjectByType<PrototypeCameraOrbit>();
                cameraOrbit?.EndDialogueFraming();
            }
        }

        private void OnGUI()
        {
            if (State != Day1FlowState.Dialogue)
                return;

            if (showingCinematic)
            {
                DrawCinematicPlaceholder();
                return;
            }

            dialoguePanelStyle ??= new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(30, 30, 24, 24)
            };
            dialogueStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = Mathf.Max(28, Mathf.RoundToInt(Screen.height / 28f)),
                wordWrap = true,
                padding = new RectOffset(8, 8, 6, 6),
                normal = { textColor = Color.white }
            };
            choiceStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = Mathf.Max(28, Mathf.RoundToInt(Screen.height / 28f)),
                wordWrap = true,
                padding = new RectOffset(18, 18, 8, 8),
                normal = { textColor = new Color(1f, 0.9f, 0.58f) },
                hover = { textColor = Color.white },
                active = { textColor = new Color(1f, 0.78f, 0.3f) }
            };

            float margin = Mathf.Max(34f, Screen.height * 0.045f);
            float width = Screen.width - margin * 2f;
            float height = Mathf.Clamp(Screen.height * 0.42f, 300f, 480f);
            float x = margin;
            float y = Screen.height - height - margin;
            Rect panel = new(x, y, width, height);
            GUI.Box(panel, GUIContent.none, dialoguePanelStyle);

            Rect viewport = new(x + 34f, y + 28f, width - 68f, height - 56f);
            float contentWidth = Mathf.Max(100f, viewport.width - 24f);
            float contentHeight = 12f;
            if (dialogueHistory.Count > 0)
            {
                for (int index = 0; index < dialogueHistory.Count; index++)
                    contentHeight += dialogueStyle.CalcHeight(new GUIContent(dialogueHistory[index].Text), contentWidth) + 12f;
            }
            else if (!string.IsNullOrEmpty(currentLine))
            {
                contentHeight += dialogueStyle.CalcHeight(new GUIContent(currentLine), contentWidth) + 12f;
            }

            for (int index = 0; index < choices.Count; index++)
                contentHeight += choiceStyle.CalcHeight(new GUIContent($"› {index + 1}. {choices[index].text}"), contentWidth) + 8f;
            if (choices.Count == 0)
                contentHeight += 40f;

            bool contentChanged = renderedHistoryCount != dialogueHistory.Count
                || renderedChoiceCount != choices.Count;
            Rect content = new(0f, 0f, contentWidth, Mathf.Max(viewport.height, contentHeight));
            dialogueScroll = GUI.BeginScrollView(
                viewport,
                dialogueScroll,
                content,
                false,
                false,
                GUIStyle.none,
                GUI.skin.verticalScrollbar);
            float contentY = 8f;
            if (dialogueHistory.Count > 0)
            {
                for (int index = 0; index < dialogueHistory.Count; index++)
                {
                    string text = dialogueHistory[index].Text;
                    float lineHeight = dialogueStyle.CalcHeight(new GUIContent(text), contentWidth);
                    GUI.Label(new Rect(0f, contentY, contentWidth, lineHeight), text, dialogueStyle);
                    contentY += lineHeight + 12f;
                }
            }
            else if (!string.IsNullOrEmpty(currentLine))
            {
                float lineHeight = dialogueStyle.CalcHeight(new GUIContent(currentLine), contentWidth);
                GUI.Label(new Rect(0f, contentY, contentWidth, lineHeight), currentLine, dialogueStyle);
                contentY += lineHeight + 12f;
            }

            int clickedChoice = -1;
            for (int index = 0; index < choices.Count; index++)
            {
                string text = $"› {index + 1}. {choices[index].text}";
                float choiceHeight = choiceStyle.CalcHeight(new GUIContent(text), contentWidth);
                if (GUI.Button(new Rect(0f, contentY, contentWidth, choiceHeight), text, choiceStyle))
                    clickedChoice = index;
                contentY += choiceHeight + 8f;
            }
            if (choices.Count == 0)
                GUI.Label(new Rect(0f, contentY, contentWidth, 36f), "Enter / Space ▶", choiceStyle);
            GUI.EndScrollView();

            if (contentChanged)
            {
                dialogueScroll.y = Mathf.Max(0f, contentHeight - viewport.height);
                renderedHistoryCount = dialogueHistory.Count;
                renderedChoiceCount = choices.Count;
            }
            if (clickedChoice >= 0)
                Choose(clickedChoice);
        }

        private static bool IsPlayerLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            string trimmed = line.TrimStart();
            return trimmed.StartsWith("你：", StringComparison.Ordinal)
                || trimmed.StartsWith("你:", StringComparison.Ordinal);
        }

        private void DrawCinematicPlaceholder()
        {
            GUI.depth = -5000;
            Color old = GUI.color;
            GUI.color = new Color(0.11f, 0.075f, 0.055f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            Rect paper = new(Screen.width * 0.12f, Screen.height * 0.1f, Screen.width * 0.76f, Screen.height * 0.7f);
            GUI.color = new Color(0.76f, 0.66f, 0.48f, 1f);
            GUI.DrawTexture(paper, Texture2D.whiteTexture);
            GUI.color = new Color(0.22f, 0.16f, 0.12f, 0.9f);
            GUI.DrawTexture(new Rect(paper.x + paper.width * 0.1f, paper.y + paper.height * 0.2f, paper.width * 0.28f, paper.height * 0.58f), Texture2D.whiteTexture);
            GUI.color = new Color(0.45f, 0.35f, 0.24f, 0.85f);
            GUI.DrawTexture(new Rect(paper.x + paper.width * 0.55f, paper.y + paper.height * 0.3f, paper.width * 0.3f, paper.height * 0.36f), Texture2D.whiteTexture);

            cinematicStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(28, Mathf.RoundToInt(Screen.height / 28f)),
                wordWrap = true,
                padding = new RectOffset(44, 44, 24, 24),
                normal = { textColor = new Color(0.94f, 0.88f, 0.75f) }
            };
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width * 0.16f, Screen.height * 0.82f, Screen.width * 0.68f, Screen.height * 0.12f), currentLine + "\nEnter / Space ▶", cinematicStyle);
            GUI.color = old;
        }
    }
}

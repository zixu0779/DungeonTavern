using System;
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
        AwaitingOpeningSwitch,
        ServingBran,
        AwaitingClosingSwitch,
        Completed
    }

    public sealed class Day1NarrativeController : MonoBehaviour
    {
        [Header("Narrative")]
        [SerializeField] private InkFile chapterOne;

        [Header("Scene References")]
        [SerializeField] private PrototypePlayerMover player;
        [SerializeField] private Transform storageArrival;
        [SerializeField] private BusinessDayController businessDay;
        [SerializeField, Min(0.5f)] private float storageArrivalRadius = 1.6f;

        private readonly List<Choice> choices = new();
        private Story story;
        private CustomerServicePoint bran;
        private string currentLine;
        private GUIStyle dialogueStyle;
        private GUIStyle choiceStyle;

        public Day1FlowState State { get; private set; }
        public bool CanOpenTavern => State == Day1FlowState.AwaitingOpeningSwitch;
        public bool CanCloseTavern => State == Day1FlowState.AwaitingClosingSwitch
            && businessDay != null
            && businessDay.State == BusinessDayState.Completed;
        public int CurrentChoiceCount => choices.Count;

        public void Configure(
            InkFile storyAsset,
            PrototypePlayerMover playerMover,
            Transform storagePoint,
            BusinessDayController day)
        {
            chapterOne = storyAsset;
            player = playerMover;
            storageArrival = storagePoint;
            businessDay = day;
        }

        private void Start()
        {
            if (chapterOne == null || string.IsNullOrWhiteSpace(chapterOne.storyJson))
            {
                Debug.LogError("Day 1 narrative requires a compiled Chapter01 InkFile.", this);
                enabled = false;
                return;
            }
            if (player == null || storageArrival == null || businessDay == null)
            {
                Debug.LogError("Day 1 narrative scene references are incomplete.", this);
                enabled = false;
                return;
            }

            businessDay.CustomerSpawned += OnCustomerSpawned;
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
        }

        private void Update()
        {
            if (State == Day1FlowState.AwaitingStorageReturn
                && (player.transform.position - storageArrival.position).sqrMagnitude
                    <= storageArrivalRadius * storageArrivalRadius)
            {
                SelectExternalGate();
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
            bran.SettlementRequested += OnSettlementRequested;
        }

        private void OnSettlementRequested(CustomerServicePoint customer)
        {
            if (State != Day1FlowState.ServingBran || customer != bran)
                return;

            SelectExternalGate();
        }

        private void ShowNextContent()
        {
            choices.Clear();
            currentLine = string.Empty;

            if (story.canContinue)
            {
                currentLine = story.Continue().Trim();
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

            if (gate.Contains("主角起身", StringComparison.Ordinal))
                State = Day1FlowState.AwaitingStorageReturn;
            else if (gate.Contains("营业按钮", StringComparison.Ordinal))
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
        }

        private void OnGUI()
        {
            if (State != Day1FlowState.Dialogue)
                return;

            dialogueStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 20,
                wordWrap = true,
                padding = new RectOffset(24, 24, 20, 20),
                normal = { textColor = Color.white }
            };
            choiceStyle ??= new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 17,
                wordWrap = true,
                padding = new RectOffset(18, 18, 10, 10)
            };

            float width = Mathf.Min(760f, Screen.width - 48f);
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - 250f;
            GUI.Box(new Rect(x, y, width, 210f), currentLine, dialogueStyle);

            if (choices.Count == 0)
            {
                GUI.Label(new Rect(x + width - 170f, y + 168f, 150f, 28f), "Enter / Space ▶");
                return;
            }

            float choiceY = y + 18f;
            for (int index = 0; index < choices.Count; index++)
            {
                if (GUI.Button(
                    new Rect(x + 18f, choiceY, width - 36f, 36f),
                    $"{index + 1}. {choices[index].text}",
                    choiceStyle))
                {
                    Choose(index);
                }
                choiceY += 42f;
            }
        }
    }
}

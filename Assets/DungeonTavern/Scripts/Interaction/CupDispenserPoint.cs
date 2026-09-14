using DungeonTavern.Tavern25D;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class CupDispenserPoint : InteractionPoint
    {
        [SerializeField] private CupDispenserActivation activation;
        [SerializeField] private Transform cupVisual;
        [SerializeField, Min(0)] private float bobHeight = .06f;
        [SerializeField, Min(.1f)] private float cupAppearDuration = .5f;
        private Vector3 restPosition, restScale;
        private float appearance;
        private TavernMenuSystem menu;
        private PlayerHands playerHands;
        public bool CupReady { get; private set; }
        public Vector3 GuidancePosition => activation ? activation.GuidancePosition : transform.position;

        // Delivered cups remain at their customers until settlement. They cover the
        // delivered drink portions, so only unserved drinks minus the held cup remain.
        public int MissingCups => Mathf.Max(0, (menu == null ? 0 : menu.MissingServingCups)
            - (playerHands != null && playerHands.CurrentItem is HeldItem.EmptyCup or HeldItem.TestDrink ? 1 : 0));

        private void Awake()
        {
            restPosition = cupVisual.localPosition;
            restScale = cupVisual.localScale;
            cupVisual.gameObject.SetActive(false);
            activation.SetActivated(false);
        }

        public override string GetPrompt(PlayerHands hands) =>
            CupReady && hands != null && hands.CurrentItem == HeldItem.None ? "F：取杯" : string.Empty;

        public override bool Interact(PlayerHands hands)
        {
            if (!CupReady || hands == null || !hands.TryHold(HeldItem.EmptyCup)) return false;
            DungeonTavern.UI.TavernGuidance.Complete(DungeonTavern.UI.GuideStep.Cup);
            playerHands = hands;
            appearance = 0;
            CupReady = false;
            cupVisual.gameObject.SetActive(false);
            activation.SetActivated(MissingCups > 0);
            return true;
        }

        private void LateUpdate()
        {
            if (menu == null) menu = FindAnyObjectByType<TavernMenuSystem>();
            if (playerHands == null) playerHands = FindAnyObjectByType<PlayerHands>();
            bool needed = MissingCups > 0;
            activation.SetActivated(needed);
            appearance = needed ? Mathf.MoveTowards(appearance, 1, Time.deltaTime / cupAppearDuration) : 0;
            CupReady = needed && appearance >= 1;
            cupVisual.gameObject.SetActive(needed);
            if (!needed) return;
            float amount = Mathf.SmoothStep(0, 1, appearance);
            cupVisual.localScale = restScale * amount;
            cupVisual.localPosition = restPosition + Vector3.up * (Mathf.Sin(Time.time * 2f) * bobHeight - .15f * (1 - amount));
        }
    }
}

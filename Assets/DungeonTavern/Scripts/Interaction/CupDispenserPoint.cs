using DungeonTavern.Tavern25D;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class CupDispenserPoint : InteractionPoint
    {
        [SerializeField] private CupDispenserActivation activation;
        [SerializeField] private Transform cupVisual;
        [SerializeField, Min(0)] private float bobHeight = .06f;
        private Vector3 restPosition;
        private bool cupReady;
        private TavernMenuSystem menu;
        public bool CupReady => cupReady;

        private void Awake()
        {
            restPosition = cupVisual.localPosition;
            cupVisual.gameObject.SetActive(false);
            activation.SetActivated(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            menu = FindAnyObjectByType<TavernMenuSystem>();
            if (menu != null) menu.OrderRegistered += Activate;
        }

        protected override void OnDisable()
        {
            if (menu != null) menu.OrderRegistered -= Activate;
            base.OnDisable();
        }

        private void Activate() => SetReady(true);

        private void SetReady(bool value)
        {
            cupReady = value;
            cupVisual.gameObject.SetActive(value);
            activation.SetActivated(value);
        }

        public override string GetPrompt(PlayerHands hands) =>
            cupReady && hands != null && hands.CurrentItem == HeldItem.None ? "F：取杯" : string.Empty;

        public override bool Interact(PlayerHands hands)
        {
            if (!cupReady || hands == null || !hands.TryHold(HeldItem.EmptyCup)) return false;
            SetReady(false);
            return true;
        }

        private void Update()
        {
            if (cupReady)
                cupVisual.localPosition = restPosition + Vector3.up * (Mathf.Sin(Time.time * 2f) * bobHeight);
        }
    }
}

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
        public bool CupReady => cupReady;

        private void Awake()
        {
            restPosition = cupVisual.localPosition;
            cupVisual.gameObject.SetActive(false);
            activation.SetActivated(false);
        }

        public override string GetPrompt(PlayerHands hands) =>
            hands != null && hands.CurrentItem == HeldItem.None
                ? (cupReady ? "F：取杯" : "F：启动取杯器") : string.Empty;

        public override bool Interact(PlayerHands hands)
        {
            if (hands == null || hands.CurrentItem != HeldItem.None) return false;
            if (cupReady && !hands.TryHold(HeldItem.EmptyCup)) return false;
            cupReady = !cupReady;
            cupVisual.gameObject.SetActive(cupReady);
            activation.SetActivated(cupReady);
            return true;
        }

        private void Update()
        {
            if (cupReady)
                cupVisual.localPosition = restPosition + Vector3.up * (Mathf.Sin(Time.time * 2f) * bobHeight);
        }
    }
}

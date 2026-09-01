using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class FloorLeverPoint : InteractionPoint
    {
        [SerializeField] private Transform handlePivot;
        [SerializeField] private TwoStateProp statusSign;
        [SerializeField] private Day1NarrativeController narrative;
        [SerializeField] private float switchAngle = 50f;
        [SerializeField, Min(1)] private float degreesPerSecond = 180f;
        private Quaternion restingRotation;
        public bool IsOn { get; private set; }
        private void Awake() => restingRotation = handlePivot.localRotation;
        public override string GetPrompt(PlayerHands hands) => IsOn ? "F：关闭拉杆" : "F：开启拉杆";
        public override bool Interact(PlayerHands hands)
        {
            IsOn = !IsOn;
            statusSign?.SetOpen(IsOn);
            if (narrative != null && ((IsOn && narrative.CanOpenTavern) || (!IsOn && narrative.CanCloseTavern)))
                narrative.TryUseBusinessSwitch();
            return true;
        }
        private void Update()
        {
            var target = restingRotation * Quaternion.Euler(IsOn ? switchAngle : 0, 0, 0);
            handlePivot.localRotation = Quaternion.RotateTowards(handlePivot.localRotation, target, degreesPerSecond * Time.deltaTime);
        }
    }
}

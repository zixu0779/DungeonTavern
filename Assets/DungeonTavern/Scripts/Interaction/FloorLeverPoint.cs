using System.Collections;
using DungeonTavern.Prototypes.Rotation25D;
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
        [SerializeField] private DoorStateController entranceDoor;
        [SerializeField] private Transform entranceView;
        [SerializeField, Min(1)] private float entranceViewSize = 3.5f;
        [SerializeField, Min(.1f)] private float cameraPanDuration = 1.3f;
        [SerializeField] private float switchAngle = 50f;
        [SerializeField, Min(1)] private float degreesPerSecond = 180f;
        private Quaternion restingRotation;
        private PrototypeCameraOrbit orbit;
        private PrototypePlayerMover player;
        private bool previousMovement, previousInteraction;
        private PlayerInteractionController interaction;
        public bool IsOn { get; private set; }
        public bool IsSwitching { get; private set; }
        private bool CanSwitch => !IsSwitching && (narrative == null || !narrative.isActiveAndEnabled
            || (IsOn ? narrative.CanCloseTavern : narrative.CanOpenTavern));
        private void Awake() => restingRotation = handlePivot.localRotation;
        public override string GetPrompt(PlayerHands hands) => !CanSwitch ? string.Empty : IsOn ? "F：结束营业" : "F：开始营业";
        public override bool Interact(PlayerHands hands)
        {
            if (!CanSwitch) return false;
            if (entranceDoor == null || statusSign == null || entranceView == null)
            {
                Debug.LogError("Business lever requires entrance door, sign and camera view references.", this);
                return false;
            }
            IsOn = !IsOn;
            if (!IsOn) FindAnyObjectByType<BusinessDayController>()?.StopAcceptingCustomers();
            StartCoroutine(SwitchBusiness());
            return true;
        }
        private IEnumerator SwitchBusiness()
        {
            IsSwitching = true;
            player = FindAnyObjectByType<PrototypePlayerMover>();
            orbit = FindAnyObjectByType<PrototypeCameraOrbit>();
            if (player != null) { previousMovement = player.MovementInputEnabled; player.MovementInputEnabled = false; }
            interaction = player == null ? null : player.GetComponent<PlayerInteractionController>();
            if (interaction != null) { previousInteraction = interaction.enabled; interaction.enabled = false; }
            if (orbit != null) yield return orbit.FrameEntrance(entranceView, entranceViewSize, cameraPanDuration);
            entranceDoor.SetOpen(IsOn);
            yield return null;
            while (entranceDoor.IsTransitioning) yield return null;
            statusSign.SetOpen(IsOn);
            yield return null;
            while (statusSign.IsTransitioning) yield return null;
            if (orbit != null) yield return orbit.ReturnFromEntrance(cameraPanDuration);
            RestoreControl();
            if (narrative != null && narrative.isActiveAndEnabled) narrative.TryUseBusinessSwitch();
        }
        private void RestoreControl()
        {
            if (!IsSwitching) return;
            orbit?.CancelEntranceFraming();
            if (player != null) player.MovementInputEnabled = previousMovement;
            if (interaction != null) interaction.enabled = previousInteraction;
            IsSwitching = false;
        }
        protected override void OnDisable()
        {
            StopAllCoroutines();
            RestoreControl();
            base.OnDisable();
        }
        private void Update()
        {
            var target = restingRotation * Quaternion.Euler(IsOn ? switchAngle : 0, 0, 0);
            handlePivot.localRotation = Quaternion.RotateTowards(handlePivot.localRotation, target, degreesPerSecond * Time.deltaTime);
        }
    }
}

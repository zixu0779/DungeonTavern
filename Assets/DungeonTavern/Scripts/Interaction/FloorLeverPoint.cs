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
        private bool previousMovement, previousInteraction, hasOpened;
        private bool temporaryClosing, inputRestored;
        private BusinessDayController day;
        private PlayerInteractionController interaction;
        public bool IsOn { get; private set; }
        public bool IsSwitching { get; private set; }
        private bool CanSwitch => !IsSwitching && Time.timeScale > 0 && (IsOn || hasOpened
            || narrative == null || !narrative.isActiveAndEnabled || narrative.CanOpenTavern);
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
            day = FindAnyObjectByType<BusinessDayController>();
            temporaryClosing = !IsOn && day != null && day.ActiveCustomers > 0;
            if (!IsOn) day?.PauseAdmissions();
            StartCoroutine(SwitchBusiness());
            return true;
        }
        private IEnumerator SwitchBusiness()
        {
            IsSwitching = true;
            inputRestored = false;
            player = FindAnyObjectByType<PrototypePlayerMover>();
            orbit = FindAnyObjectByType<PrototypeCameraOrbit>();
            if (player != null) { previousMovement = player.MovementInputEnabled; player.MovementInputEnabled = false; }
            interaction = player == null ? null : player.GetComponent<PlayerInteractionController>();
            if (interaction != null) { previousInteraction = interaction.enabled; interaction.enabled = false; }
            if (temporaryClosing)
            {
                var bubble = player.GetComponent<WorldSpeechBubble>();
                if (bubble == null) bubble = player.gameObject.AddComponent<WorldSpeechBubble>();
                bubble.Show("酒馆要临时关闭了，请各位先离开！", 3f);
                yield return new WaitForSeconds(1f);
                day.DismissCustomers();
                while (day.ActiveCustomers > 0) yield return null;
            }
            else if (orbit != null) yield return orbit.FrameEntrance(entranceView, entranceViewSize, cameraPanDuration);
            entranceDoor.SetOpen(IsOn);
            yield return null;
            while (entranceDoor.IsTransitioning) yield return null;
            statusSign.SetOpen(IsOn);
            yield return null;
            while (statusSign.IsTransitioning) yield return null;
            if (!temporaryClosing && orbit != null) yield return orbit.ReturnFromEntrance(cameraPanDuration);
            RestoreControl();
            if (IsOn)
            {
                if (!hasOpened && narrative != null && narrative.isActiveAndEnabled) narrative.TryUseBusinessSwitch();
                day?.ResumeAdmissions();
                hasOpened = true;
            }
            else if (!temporaryClosing && narrative != null && narrative.isActiveAndEnabled && narrative.CanCloseTavern)
                narrative.TryUseBusinessSwitch();
        }
        private void RestoreControl()
        {
            if (!IsSwitching) return;
            orbit?.CancelEntranceFraming();
            RestorePlayerInput();
            IsSwitching = false;
        }
        private void RestorePlayerInput()
        {
            if (inputRestored) return;
            inputRestored = true;
            if (player != null) player.MovementInputEnabled = previousMovement;
            if (interaction != null) interaction.enabled = previousInteraction;
        }
        protected override void OnDisable()
        {
            StopAllCoroutines();
            RestoreControl();
            base.OnDisable();
        }
        private void Update()
        {
            if (IsSwitching && !temporaryClosing && orbit != null && orbit.EntranceWasCanceled && !orbit.EntranceFraming)
                RestorePlayerInput();
            var target = restingRotation * Quaternion.Euler(IsOn ? switchAngle : 0, 0, 0);
            handlePivot.localRotation = Quaternion.RotateTowards(handlePivot.localRotation, target, degreesPerSecond * Time.deltaTime);
        }
    }
}

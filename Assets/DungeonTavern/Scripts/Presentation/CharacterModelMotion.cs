using System.Collections;
using DungeonTavern.Gameplay.Interaction;
using UnityEngine;

namespace DungeonTavern.Tavern25D
{
    [RequireComponent(typeof(Animator))]
    [DefaultExecutionOrder(200)]
    public sealed class CharacterModelMotion : MonoBehaviour
    {
        [SerializeField] private Transform motionRoot;
        [SerializeField] private bool handIk;
        [SerializeField] private Vector3 cupHandPosition = new Vector3(.22f, .96f, .3f);
        [SerializeField] private Vector3 mouthOffset = new Vector3(0, .025f, .07f);
        [SerializeField, HideInInspector] private float drinkBlend;
        private Animator animator;
        private CustomerServicePoint customer;
        private HeldCupVisual cupVisual;
        private bool seated;
        private Vector3 modelRestPosition;
        private float seatBlend;
        private NpcNavigator navigator;
        private bool fullBodyAction;
        private bool vaultPose;
        private float vaultClipLength = 1;
        private Vector3 vaultHandPoint;
        public bool IsFullBodyAction => fullBodyAction;
        public Vector3 BodyPosition => animator&&animator.isHuman
            ? (animator.GetBoneTransform(HumanBodyBones.Hips).position+animator.GetBoneTransform(HumanBodyBones.Head).position)*.5f
            : motionRoot.position+Vector3.up;
        private float standUntil;
        public bool IsStandingUp => !seated && (Time.time < standUntil ||
            animator.GetCurrentAnimatorStateInfo(0).IsName("SitDown") ||
            animator.GetCurrentAnimatorStateInfo(0).IsName("SeatedIdle") ||
            animator.GetCurrentAnimatorStateInfo(0).IsName("StandUp"));
        private PlayerHands hands;
        private Vector3 previousPosition;
        private HeldItem previousItem;
        private float gripWeight;
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int HoldingCup = Animator.StringToHash("HoldingCup");
        private static readonly int PickUp = Animator.StringToHash("PickUp");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            modelRestPosition = transform.localPosition;
            if (!motionRoot) motionRoot = transform.parent ? transform.parent : transform;
            hands = motionRoot.GetComponent<PlayerHands>();
            cupVisual = motionRoot.GetComponent<HeldCupVisual>();
            customer = motionRoot.GetComponent<CustomerServicePoint>();
            navigator = motionRoot.GetComponent<NpcNavigator>();
            animator.applyRootMotion = false;
            if(customer)animator.keepAnimatorStateOnDisable=true;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == "Vault") vaultClipLength = clip.length;
        }
        private void OnEnable()
        {
            previousPosition = motionRoot.position;
            if (customer) customer.StateChanged += OnCustomerState;
            if (hands) { hands.ItemChanged += OnItemChanged; previousItem = hands.CurrentItem; UpdateHeldState(previousItem); }
        }
        private void OnDisable()
        {
            if (hands) hands.ItemChanged -= OnItemChanged;
            if (customer) customer.StateChanged -= OnCustomerState;
            if (cupVisual) cupVisual.DrinkTilt = 0;
            gripWeight = 0;
        }
        private void Update()
        {
            Vector3 delta = motionRoot.position - previousPosition;
            previousPosition = motionRoot.position;
            delta.y = 0;
            float speed = Time.deltaTime > 0 ? delta.magnitude / Time.deltaTime : 0;
            // Scene travel must not appear as a burst of walking.
            if (delta.sqrMagnitude > 1f) speed = 0;
            animator.SetFloat(Speed, speed, .1f, Time.deltaTime);
            if (cupVisual) cupVisual.DrinkTilt = drinkBlend;
            bool holding = hands && IsCup(hands.CurrentItem);
            gripWeight = Mathf.MoveTowards(gripWeight, holding ? 1f : 0f, Time.deltaTime * 3f);
        }
        private void LateUpdate()
        {
            if (!customer || !animator.isHuman || customer.AssignedSeat == null || customer.AssignedSeat.IsStanding) return;
            seatBlend = Mathf.MoveTowards(seatBlend, seated ? 1f : 0f, Time.deltaTime / .65f);
            transform.localPosition = modelRestPosition;
            if (seatBlend > 0 || IsStandingUp)
            {
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                var target = customer.AssignedSeat.SittingSurface + Vector3.up * .12f;
                transform.position += (target - hips.position) * Mathf.SmoothStep(0, 1, seatBlend);
                // The approach marker stays on the NavMesh; the visible body and collider sit on the stool.
                navigator?.SetSeatedBody(hips.position, head.position);
            }
            else navigator?.ResumeStandingBody();
        }

        private static bool IsCup(HeldItem item) => item == HeldItem.EmptyCup || item == HeldItem.TestDrink;
        private void UpdateHeldState(HeldItem item) => animator.SetBool(HoldingCup, IsCup(item));
        private void OnItemChanged(HeldItem item)
        {
            UpdateHeldState(item);
            if (previousItem == HeldItem.None && IsCup(item)) animator.SetTrigger(PickUp);
            previousItem = item;
        }
        // Presentation only: inventory consumption remains owned by gameplay.
        [ContextMenu("Preview Drink (Play Mode, filled cup)")]
        public void PlayDrink()
        {
            if (Application.isPlaying && hands && hands.CurrentItem == HeldItem.TestDrink)
                animator.SetTrigger("Drink");
        }
        [ContextMenu("Preview Sit (Play Mode)")]
        private void PreviewSit() { if (Application.isPlaying) SetSeated(true); }
        [ContextMenu("Preview Stand (Play Mode)")]
        private void PreviewStand() { if (Application.isPlaying) SetSeated(false); }
        public void SetSeated(bool value)
        {
            if (seated == value) return;
            if (!value) standUntil = Time.time + 1.1f;
            seated = value;
            animator.SetBool("Seated", value);
        }
        private void OnCustomerState(CustomerOrderState state)
        {
            SetSeated(customer.AssignedSeat && !customer.AssignedSeat.IsStanding &&
                (state == CustomerOrderState.WaitingForFood || state == CustomerOrderState.Eating || state == CustomerOrderState.AwaitingSettlement));
        }
        public void BeginProne()
        {
            fullBodyAction = true;
            animator.SetLayerWeight(1, 0);
            animator.Play("Prone", 0, 0);
        }
        public IEnumerator WakeAndStand()
        {
            fullBodyAction = true;
            animator.CrossFadeInFixedTime("WakeUp", .12f, 0);
            yield return null;
            // Wait for the actual state chain, not a second independent duration.
            while (animator && (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") || animator.IsInTransition(0)))
                yield return null;
            EndFullBodyAction();
        }
        public void BeginVaultPose(Vector3 handPoint, float seconds)
        {
            fullBodyAction = vaultPose = true;
            vaultHandPoint = handPoint;
            animator.SetLayerWeight(1, 0);
            animator.SetFloat("VaultSpeed", vaultClipLength / seconds);
            animator.Play("Vault", 0, 0);
        }
        public void EndFullBodyAction()
        {
            fullBodyAction = vaultPose = false;
            animator.SetLayerWeight(1, 1);
            animator.CrossFadeInFixedTime("Idle", .12f, 0);
        }
        private void OnAnimatorIK(int layerIndex)
        {
            if (!handIk || !animator.isHuman) return;
            if (fullBodyAction)
            {
                if (!vaultPose || layerIndex != 0) return;
                float t = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                float w = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .12f)) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.32f, .56f, t)));
                for (int hand = 0; hand < 2; hand++)
                {
                    var side = hand == 0 ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand;
                    animator.SetIKPositionWeight(side, w);
                    animator.SetIKPosition(side, vaultHandPoint + motionRoot.right * (side == AvatarIKGoal.LeftHand ? -.23f : .23f));
                }
                return;
            }
            if (layerIndex != animator.layerCount - 1) return;
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, gripWeight);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            Vector3 rest = motionRoot.TransformPoint(cupHandPosition);
            if (seated) rest.y = head.position.y - .35f;
            Vector3 mouth = head.position + motionRoot.TransformDirection(mouthOffset);
            animator.SetIKPosition(AvatarIKGoal.RightHand, Vector3.Lerp(rest, mouth, drinkBlend));
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }
    }
}

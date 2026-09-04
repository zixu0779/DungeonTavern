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
        [SerializeField, HideInInspector] private float drinkBlend;
        private Animator animator;
        private CustomerServicePoint customer;
        private HeldCupVisual cupVisual;
        private bool seated;
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
            if (!motionRoot) motionRoot = transform.parent ? transform.parent : transform;
            hands = motionRoot.GetComponent<PlayerHands>();
            cupVisual = motionRoot.GetComponent<HeldCupVisual>();
            customer = motionRoot.GetComponent<CustomerServicePoint>();
            animator.applyRootMotion = false;
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
        private void OnAnimatorIK(int layerIndex)
        {
            if (!handIk || !animator.isHuman || layerIndex != animator.layerCount - 1) return;
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, gripWeight);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            Vector3 rest = motionRoot.TransformPoint(cupHandPosition);
            if (seated) rest.y = head.position.y - .35f;
            Vector3 mouth = head.position + motionRoot.forward * .07f + Vector3.up * .025f;
            animator.SetIKPosition(AvatarIKGoal.RightHand, Vector3.Lerp(rest, mouth, drinkBlend));
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }
    }
}

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
        private Animator animator;
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
            animator.applyRootMotion = false;
        }
        private void OnEnable()
        {
            previousPosition = motionRoot.position;
            if (hands) { hands.ItemChanged += OnItemChanged; previousItem = hands.CurrentItem; UpdateHeldState(previousItem); }
        }
        private void OnDisable()
        {
            if (hands) hands.ItemChanged -= OnItemChanged;
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
        private void OnAnimatorIK(int layerIndex)
        {
            if (!handIk || !animator.isHuman || layerIndex != animator.layerCount - 1) return;
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, gripWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, motionRoot.TransformPoint(cupHandPosition));
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }
    }
}

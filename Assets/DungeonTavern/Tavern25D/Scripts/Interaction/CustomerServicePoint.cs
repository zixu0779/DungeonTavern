using System;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public enum CustomerOrderState
    {
        Inactive,
        Entering,
        Ordering,
        WaitingToOrder,
        WaitingForDrink,
        Served,
        ApproachingSettlement,
        QueueingForSettlement,
        AwaitingSettlement,
        Leaving,
        Finished
    }

    public sealed class CustomerServicePoint : InteractionPoint
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 3.4f;
        [SerializeField, Min(0f)] private float orderingDuration = 1.25f;
        [SerializeField, Min(0f)] private float servedPauseDuration = 1f;
        [SerializeField, Min(0.1f)] private float arrivalTolerance = 0.75f;

        private Renderer[] customerRenderers;
        private Transform guestEntry;
        private SeatPoint assignedSeat;
        private float stateTimer;
        private string customerName = "Customer";
        private HeldItem requiredItem = HeldItem.TestDrink;
        private bool isInitialized;
        private SettlementQueue settlementQueue;
        private CharacterController movementController;
        private NpcNavigator navigator;
        private WorldSpeechBubble bubble;

        public CustomerOrderState State { get; private set; }

        public string CustomerName => customerName;

        public HeldItem RequiredItem => requiredItem;

        public bool IsServed => State is CustomerOrderState.Served
            or CustomerOrderState.Leaving
            or CustomerOrderState.Finished;

        public event Action<CustomerOrderState> StateChanged;

        public event Action<CustomerServicePoint> Finished;
        public event Action<CustomerServicePoint> SettlementRequested;

        private void Awake()
        {
            customerRenderers = GetComponentsInChildren<Renderer>(true);
            moveSpeed = Mathf.Max(moveSpeed, 3.4f);
            bubble = GetComponent<WorldSpeechBubble>();
            if (bubble == null)
                bubble = gameObject.AddComponent<WorldSpeechBubble>();
            movementController = GetComponent<CharacterController>();
            if (movementController == null)
                movementController = gameObject.AddComponent<CharacterController>();
            movementController.radius = 0.28f;
            movementController.height = 1.5f;
            movementController.center = new Vector3(0f, 0.75f, 0f);
            movementController.stepOffset = 0.25f;
            navigator = GetComponent<NpcNavigator>();
            if (navigator == null)
                navigator = gameObject.AddComponent<NpcNavigator>();
            navigator.Configure(moveSpeed, arrivalTolerance);
            if (GetComponent<DungeonTavern.Tavern25D.DoorPassageAgent>() == null)
                gameObject.AddComponent<DungeonTavern.Tavern25D.DoorPassageAgent>();
            SetCustomerVisible(false);
            ChangeState(CustomerOrderState.Inactive, true);
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            switch (State)
            {
                case CustomerOrderState.Entering:
                    if (MoveTowards(assignedSeat.Position))
                        ChangeState(CustomerOrderState.Ordering);
                    break;

                case CustomerOrderState.Ordering:
                    if (TickTimer())
                        ChangeState(CustomerOrderState.WaitingToOrder);
                    break;

                case CustomerOrderState.Served:
                    if (TickTimer())
                    {
                        if (settlementQueue != null)
                            settlementQueue.Enqueue(this);
                        ChangeState(settlementQueue == null
                            ? CustomerOrderState.Leaving
                            : CustomerOrderState.ApproachingSettlement);
                    }
                    break;

                case CustomerOrderState.ApproachingSettlement:
                case CustomerOrderState.QueueingForSettlement:
                    if (MoveTowards(settlementQueue.GetPosition(this)))
                        ChangeState(settlementQueue.IsFirst(this)
                            ? CustomerOrderState.AwaitingSettlement
                            : CustomerOrderState.QueueingForSettlement);
                    break;

                case CustomerOrderState.Leaving:
                    if (MoveTowards(guestEntry.position))
                    {
                        ChangeState(CustomerOrderState.Finished);
                        SetCustomerVisible(false);
                        Debug.Log($"Customer complete: {customerName} exited the tavern.", this);
                        Finished?.Invoke(this);
                    }
                    break;
            }
        }

        public void Initialize(
            string displayName,
            HeldItem orderItem,
            Transform entry,
            SeatPoint seat,
            Color tint,
            SettlementQueue billSettlementQueue = null)
        {
            if (isInitialized)
                throw new InvalidOperationException($"{name} has already been initialized.");

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (seat == null)
                throw new ArgumentNullException(nameof(seat));

            customerName = string.IsNullOrWhiteSpace(displayName) ? "Customer" : displayName;
            requiredItem = orderItem;
            guestEntry = entry;
            assignedSeat = seat;
            settlementQueue = billSettlementQueue;
            gameObject.name = $"Customer_{customerName}";
            ApplyTint(tint);

            transform.position = guestEntry.position;
            SetCustomerVisible(true);
            isInitialized = true;
            ChangeState(CustomerOrderState.Entering, true);
        }

        public override string GetPrompt(PlayerHands hands)
        {
            if (State == CustomerOrderState.AwaitingSettlement)
                return $"F：和{customerName}结账";

            if (State == CustomerOrderState.WaitingToOrder)
                return $"F：记下{customerName}的订单";

            if (State != CustomerOrderState.WaitingForDrink)
                return string.Empty;

            if (hands == null || hands.CurrentItem == HeldItem.None)
                return $"{customerName}正在等待：{GetItemLabel(RequiredItem)}";

            return hands.CurrentItem == RequiredItem
                ? $"F：送上{GetItemLabel(RequiredItem)}"
                : "这不是这位客人的订单";
        }

        public override bool Interact(PlayerHands hands)
        {
            if (State == CustomerOrderState.AwaitingSettlement)
            {
                SettlementRequested?.Invoke(this);
                return true;
            }

            if (State == CustomerOrderState.WaitingToOrder)
            {
                if (hands == null || !hands.OrderBook.TryAccept(this, RequiredItem))
                    return false;
                ChangeState(CustomerOrderState.WaitingForDrink);
                Debug.Log($"Order accepted: {customerName} requested {GetItemLabel(RequiredItem)}.", this);
                return true;
            }

            if (State != CustomerOrderState.WaitingForDrink
                || hands == null
                || hands.CurrentItem != RequiredItem
                || !hands.OrderBook.TryComplete(this, RequiredItem))
            {
                return false;
            }

            hands.Clear();
            ChangeState(CustomerOrderState.Served);
            Debug.Log($"Order served: {customerName} received {GetItemLabel(RequiredItem)}.", this);
            return true;
        }

        public bool CompleteSettlement()
        {
            if (State != CustomerOrderState.AwaitingSettlement)
                return false;

            settlementQueue?.Remove(this);
            ChangeState(CustomerOrderState.Leaving);
            return true;
        }

        private bool MoveTowards(Vector3 destination)
        {
            navigator.MoveTo(destination, arrivalTolerance);
            return navigator.HasArrived(arrivalTolerance);
        }

        private bool TickTimer()
        {
            stateTimer -= Time.deltaTime;
            return stateTimer <= 0f;
        }

        private void ChangeState(CustomerOrderState nextState, bool force = false)
        {
            if (!force && State == nextState)
                return;

            State = nextState;
            stateTimer = nextState switch
            {
                CustomerOrderState.Ordering => orderingDuration,
                CustomerOrderState.Served => servedPauseDuration,
                _ => 0f
            };

            UpdateBubble(nextState);

            Debug.Log($"Customer {customerName} state: {State}", this);
            StateChanged?.Invoke(State);
        }

        private void ApplyTint(Color tint)
        {
            for (int index = 0; index < customerRenderers.Length; index++)
            {
                if (customerRenderers[index] is SpriteRenderer spriteRenderer)
                    spriteRenderer.color = tint;
            }
        }

        private static string GetItemLabel(HeldItem item)
        {
            return item switch
            {
                HeldItem.TestDrink => "麦芽饮料",
                _ => item.ToString()
            };
        }

        private void UpdateBubble(CustomerOrderState state)
        {
            if (bubble == null)
                return;
            switch (state)
            {
                case CustomerOrderState.WaitingToOrder:
                    bubble.Show("老板，我想要一杯麦芽饮料。");
                    break;
                case CustomerOrderState.WaitingForDrink:
                    bubble.Show("一杯麦芽饮料，谢谢。");
                    break;
                case CustomerOrderState.Served:
                    bubble.Show("味道不错。");
                    break;
                case CustomerOrderState.ApproachingSettlement:
                case CustomerOrderState.QueueingForSettlement:
                case CustomerOrderState.AwaitingSettlement:
                    bubble.Show("老板，结账。");
                    break;
                case CustomerOrderState.Leaving:
                case CustomerOrderState.Finished:
                    bubble.Hide();
                    break;
            }
        }

        private void SetCustomerVisible(bool visible)
        {
            for (int index = 0; index < customerRenderers.Length; index++)
                customerRenderers[index].enabled = visible;
        }
    }
}

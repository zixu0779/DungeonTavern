using System;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public enum CustomerOrderState
    {
        Inactive,
        Entering,
        Ordering,
        WaitingForDrink,
        Served,
        Leaving,
        Finished
    }

    public sealed class CustomerServicePoint : InteractionPoint
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.2f;
        [SerializeField, Min(0f)] private float orderingDuration = 1.25f;
        [SerializeField, Min(0f)] private float servedPauseDuration = 1f;

        private Renderer[] customerRenderers;
        private Transform guestEntry;
        private SeatPoint assignedSeat;
        private float stateTimer;
        private string customerName = "Customer";
        private HeldItem requiredItem = HeldItem.TestDrink;
        private bool isInitialized;

        public CustomerOrderState State { get; private set; }

        public string CustomerName => customerName;

        public HeldItem RequiredItem => requiredItem;

        public bool IsServed => State is CustomerOrderState.Served
            or CustomerOrderState.Leaving
            or CustomerOrderState.Finished;

        public event Action<CustomerOrderState> StateChanged;

        public event Action<CustomerServicePoint> Finished;

        private void Awake()
        {
            customerRenderers = GetComponentsInChildren<Renderer>(true);
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
                        ChangeState(CustomerOrderState.WaitingForDrink);
                    break;

                case CustomerOrderState.Served:
                    if (TickTimer())
                        ChangeState(CustomerOrderState.Leaving);
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
            Color tint)
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
            gameObject.name = $"Customer_{customerName}";
            ApplyTint(tint);

            transform.position = guestEntry.position;
            SetCustomerVisible(true);
            isInitialized = true;
            ChangeState(CustomerOrderState.Entering, true);
        }

        public override string GetPrompt(PlayerHands hands)
        {
            if (State != CustomerOrderState.WaitingForDrink)
                return string.Empty;

            if (hands == null || hands.CurrentItem == HeldItem.None)
                return $"{customerName} orders: {GetItemLabel(RequiredItem)}";

            return hands.CurrentItem == RequiredItem
                ? $"F: Serve {GetItemLabel(RequiredItem)}"
                : "Wrong item for this order";
        }

        public override bool Interact(PlayerHands hands)
        {
            if (State != CustomerOrderState.WaitingForDrink
                || hands == null
                || hands.CurrentItem != RequiredItem)
            {
                return false;
            }

            hands.Clear();
            ChangeState(CustomerOrderState.Served);
            Debug.Log($"Order served: {customerName} received {GetItemLabel(RequiredItem)}.", this);
            return true;
        }

        private bool MoveTowards(Vector3 destination)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                moveSpeed * Time.deltaTime);

            Vector3 direction = destination - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            if ((transform.position - destination).sqrMagnitude > 0.0004f)
                return false;

            transform.position = destination;
            return true;
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
                HeldItem.TestDrink => "Test Drink",
                _ => item.ToString()
            };
        }

        private void SetCustomerVisible(bool visible)
        {
            for (int index = 0; index < customerRenderers.Length; index++)
                customerRenderers[index].enabled = visible;
        }
    }
}

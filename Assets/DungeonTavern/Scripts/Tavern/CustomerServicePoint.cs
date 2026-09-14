using System;
using DungeonTavern.Prototypes.Rotation25D;
using DungeonTavern.Tavern25D;
using DungeonTavern.Tavern25D.Narrative;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public enum CustomerOrderState
    {
        Inactive, Entering, QueueingForOrder, Ordering, FindingSeat,
        MovingToSeat, WaitingForFood, Eating, AwaitingSettlement, Leaving, Finished, ShowingOrder
    }

    public sealed class CustomerServicePoint : InteractionPoint
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.25f;
        [SerializeField, Min(.1f)] private float orderingDuration = 3f;
        [SerializeField, Min(.1f)] private float orderDisplayDuration = 1.5f;
        [SerializeField, Min(0.1f)] private float arrivalTolerance = 0.25f;
        private Renderer[] customerRenderers;
        private Transform guestEntry;
        private SeatPoint assignedSeat;
        private SeatRegistry seatRegistry;
        private float stateTimer;
        private string customerName = "Customer";
        private bool isInitialized;
        private bool settlementPending;
        private bool queueArrived;
        [SerializeField] private ServiceOrderQueue serviceQueue;
        [SerializeField] private TavernMenuSystem menuSystem;
        [SerializeField] private Transform menuPoint;
        private CharacterController movementController;
        private NpcNavigator navigator;
        private CharacterModelMotion modelMotion;
        private WorldSpeechBubble bubble;
        public CustomerOrderState State { get; private set; }
        public string CustomerName => customerName;
        public CustomerSeatingKind SeatingKind { get; private set; }
        public int PartyId { get; private set; }
        public bool DialogueFacing { get; set; }
        public bool UsesDemoMenu { get; set; }
        public void BeginGroupOrdering() => ChangeState(CustomerOrderState.Ordering);
        public void FinishGroupOrdering(CustomerOrder order)
        { Order=order;menuSystem.RegisterOrder(this,Order);serviceQueue.Remove(this);ChangeState(CustomerOrderState.FindingSeat); }

        public SeatPoint AssignedSeat => assignedSeat;
        public Vector3 ServicePosition => modelMotion ? modelMotion.BodyPosition : transform.position+Vector3.up;

        public void ConfigureSeating(CustomerSeatingKind kind, int partyId = 0)
        {
            if (isInitialized) throw new InvalidOperationException("Configure seating before initialization.");
            SeatingKind = kind; PartyId = partyId;
        }
        public CustomerOrder Order { get; private set; }
        public bool IsServed => Order != null && Order.AllDelivered;
        public event Action<CustomerOrderState> StateChanged;
        public event Action<CustomerServicePoint> Finished;
        // Return true only when a narrative actually takes ownership of settlement.
        public event Func<CustomerServicePoint, bool> SettlementRequested;

        private void Awake()
        {
            customerRenderers = GetComponentsInChildren<Renderer>(true);
            modelMotion = GetComponentInChildren<CharacterModelMotion>(true);
            PrototypePlayerMover player = FindAnyObjectByType<PrototypePlayerMover>();
            float playerSpeed = player == null ? 3.25f : player.MoveSpeed;
            moveSpeed = playerSpeed;
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
            if (!isInitialized) return;
            bool arrived = false;
            if (State is CustomerOrderState.Entering or CustomerOrderState.QueueingForOrder)
            {
                var destination = serviceQueue.GetPosition(this);
                var offset = destination - transform.position;
                offset.y = 0;
                queueArrived = arrived = offset.sqrMagnitude <= arrivalTolerance * arrivalTolerance;
                if (arrived) navigator.Stop();
                else queueArrived = arrived = MoveTowards(destination);
            }
            else if (State == CustomerOrderState.MovingToSeat)
            {
                var offset=Vector3.ProjectOnPlane(assignedSeat.Position-transform.position,Vector3.up);
                arrived=offset.sqrMagnitude<=arrivalTolerance*arrivalTolerance;
                if(arrived)navigator.Stop();else arrived=MoveTowards(assignedSeat.Position);
            }
            else if (State == CustomerOrderState.Leaving && (modelMotion == null || !modelMotion.IsStandingUp))
            {
                assignedSeat?.Release(this);
                // The doorway is an area, not one shared point that every solid NPC must occupy.
                arrived = navigator.MoveTo(guestEntry.position, .7f) && navigator.HasArrived(.7f);
            }
            Tick(Time.deltaTime, arrived);
        }

        private void LateUpdate()
        {
            if(DialogueFacing)return;
            if (State is CustomerOrderState.Ordering or CustomerOrderState.ShowingOrder
                || (State == CustomerOrderState.QueueingForOrder && queueArrived))
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(serviceQueue.GetFacing(this)), 540f * Time.deltaTime);
                return;
            }
            if (assignedSeat?.Table == null || State is not (CustomerOrderState.WaitingForFood
                or CustomerOrderState.Eating or CustomerOrderState.AwaitingSettlement)) return;
            var facing = assignedSeat.Facing;
            facing.y = 0;
            if (facing.sqrMagnitude > .001f) transform.rotation = Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(facing),180f*Time.deltaTime);
        }

        // Movement reports arrival; this method owns the service flow, independent of pathfinding.
        internal void Tick(float seconds, bool arrived)
        {
            if (!isInitialized) return;
            switch (State)
            {
                case CustomerOrderState.Entering:
                    ChangeState(CustomerOrderState.QueueingForOrder);
                    break;
                case CustomerOrderState.QueueingForOrder:
                    if (arrived && serviceQueue.IsFirst(this)) { if(UsesDemoMenu)serviceQueue.TryBeginGroup(this);else ChangeState(CustomerOrderState.Ordering); }
                    break;
                case CustomerOrderState.Ordering:
                    if(UsesDemoMenu)break;
                    stateTimer -= seconds;
                    if (stateTimer <= 0 && menuSystem.RegisterOrder(this, Order))
                    {
                        ChangeState(CustomerOrderState.ShowingOrder);
                    }
                    break;
                case CustomerOrderState.ShowingOrder:
                    stateTimer -= seconds;
                    if (stateTimer <= 0)
                    {
                        serviceQueue.Remove(this);
                        ChangeState(CustomerOrderState.FindingSeat);
                    }
                    break;
                case CustomerOrderState.FindingSeat:
                    if (assignedSeat != null || seatRegistry.TryReserve(this, out assignedSeat))
                        ChangeState(CustomerOrderState.MovingToSeat);
                    break;
                case CustomerOrderState.MovingToSeat:
                    if (arrived)
                    {
                        var facing=assignedSeat.Facing;
                        if(facing.sqrMagnitude>.001f)
                        {
                            var desired=Quaternion.LookRotation(facing);
                            transform.rotation=Quaternion.RotateTowards(transform.rotation,desired,180f*seconds);
                            if(Quaternion.Angle(transform.rotation,desired)>2f)break;
                        }
                        ChangeState(CustomerOrderState.WaitingForFood);
                    }
                    break;
                case CustomerOrderState.WaitingForFood:
                    if(Order.HasFood)ChangeState(CustomerOrderState.Eating);
                    else if(Order.AllConsumed)ChangeState(CustomerOrderState.AwaitingSettlement);
                    break;
                case CustomerOrderState.Eating:
                    Order.Eat(seconds);
                    if (Order.AllConsumed) ChangeState(CustomerOrderState.AwaitingSettlement);
                    else if (!Order.HasFood) ChangeState(CustomerOrderState.WaitingForFood);
                    break;
                case CustomerOrderState.Leaving:
                    if (arrived)
                    {
                        ChangeState(CustomerOrderState.Finished);
                        SetCustomerVisible(false);
                        Finished?.Invoke(this);
                    }
                    break;
            }
        }

        public void Initialize(string displayName, System.Collections.Generic.IEnumerable<OrderRequest> requests,
            Transform entry, SeatRegistry seats, Color tint, SeatPoint reservedSeat = null)
        {
            if (isInitialized) throw new InvalidOperationException("Customer already initialized.");
            if (entry == null || seats == null) throw new ArgumentException("Entry and seat registry are required.");
            if (menuSystem == null) menuSystem = FindAnyObjectByType<TavernMenuSystem>();
            if (serviceQueue == null) serviceQueue = FindAnyObjectByType<ServiceOrderQueue>();
            if (menuPoint == null) menuPoint = serviceQueue != null ? serviceQueue.MenuAnchor : null;
            if (menuPoint == null) menuPoint = GameObject.Find("MenuApproach")?.transform;
            if (menuSystem == null || serviceQueue == null || menuPoint == null)
                throw new InvalidOperationException("Customer needs TavernMenuSystem, ServiceOrderQueue and MenuApproach.");
            Order = new CustomerOrder(requests, menuSystem.FindDish);
            customerName = string.IsNullOrWhiteSpace(displayName) ? "Customer" : displayName;
            guestEntry = entry;
            seatRegistry = seats;
            assignedSeat = reservedSeat;
            serviceQueue.BindMenu(menuPoint);
            gameObject.name = $"Customer_{customerName}";
            ApplyTint(tint);
            transform.position = entry.position;
            serviceQueue.Enqueue(this);
            SetCustomerVisible(true);
            isInitialized = true;
            ChangeState(CustomerOrderState.Entering, true);
        }

        private bool CanAcceptFood => State is CustomerOrderState.WaitingForFood or CustomerOrderState.Eating;
        public override string GetPrompt(PlayerHands hands)
        {
            if (State == CustomerOrderState.AwaitingSettlement)
                return settlementPending ? "正在结账对话中" : $"F：和{customerName}结账";
            if (!CanAcceptFood) return string.Empty;
            if (hands == null || hands.CurrentItem == HeldItem.None)
                return Order.AllDelivered ? $"{customerName}正在用餐" : $"{customerName}正在等待上菜";
            return Order.Needs(hands.CurrentItem)
                ? $"F：送上{menuSystem.FindDish(Order.NextDelivery(hands.CurrentItem).Item).label}" : "这不是这位客人待上的菜品";
        }

        public override bool Interact(PlayerHands hands)
        {
            if (State == CustomerOrderState.AwaitingSettlement)
            {
                if (settlementPending) return false;
                if (SettlementRequested != null)
                    foreach (Func<CustomerServicePoint, bool> handler in SettlementRequested.GetInvocationList())
                        if (handler(this)) { settlementPending = true; return true; }
                return CompleteSettlement();
            }
            if (!CanAcceptFood || hands == null || !menuSystem.TryServe(this, hands.CurrentItem)) return false;
            hands.Clear();
            ChangeState(CustomerOrderState.Eating);
            return true;
        }

        public void DismissWithoutPayment()
        {
            if (State is CustomerOrderState.Inactive or CustomerOrderState.Finished) return;
            serviceQueue?.Remove(this);
            menuSystem?.CancelOrder(this);
            settlementPending = false;
            ChangeState(CustomerOrderState.Leaving);
            const string symbols = "@#$%&*!?";
            var complaint = new char[UnityEngine.Random.Range(5, 9)];
            for (int i = 0; i < complaint.Length; i++) complaint[i] = symbols[UnityEngine.Random.Range(0, symbols.Length)];
            bubble?.Show(new string(complaint));
        }

        public bool CompleteSettlement()
        {
            if (State != CustomerOrderState.AwaitingSettlement || !menuSystem.CompleteSale(this)) return false;
            settlementPending = false;
            ChangeState(CustomerOrderState.Leaving);
            return true;
        }

        private bool MoveTowards(Vector3 destination) => navigator.MoveTo(destination, arrivalTolerance)
            && navigator.HasArrived(arrivalTolerance);

        private void ChangeState(CustomerOrderState nextState, bool force = false)
        {
            if (!force && State == nextState) return;
            State = nextState;
            if (nextState == CustomerOrderState.Ordering)
            {
                stateTimer = orderingDuration;
                transform.rotation = menuPoint.rotation;
            }
            if (nextState == CustomerOrderState.ShowingOrder) stateTimer = orderDisplayDuration;
            if (nextState is CustomerOrderState.Ordering or CustomerOrderState.ShowingOrder or CustomerOrderState.WaitingForFood
                or CustomerOrderState.Eating or CustomerOrderState.AwaitingSettlement) navigator.Stop();
            UpdateBubble(nextState);
            StateChanged?.Invoke(State);
        }

        private void OnDestroy()
        {
            serviceQueue?.Remove(this);
            assignedSeat?.Release(this);
            if (seatRegistry != null) seatRegistry.Release(this);
            if (menuSystem != null) menuSystem.CancelOrder(this);
        }

        private void ApplyTint(Color tint)
        {
            foreach (var renderer in customerRenderers)
                if (renderer is SpriteRenderer sprite) sprite.color = tint;
        }

        private void UpdateBubble(CustomerOrderState state)
        {
            if (bubble == null) return;
            if (state is CustomerOrderState.ShowingOrder or CustomerOrderState.FindingSeat
                or CustomerOrderState.MovingToSeat or CustomerOrderState.WaitingForFood or CustomerOrderState.Eating)
            {
                bubble.ShowOrder(Order, state == CustomerOrderState.Eating);
                return;
            }
            if (state == CustomerOrderState.Ordering) bubble.Show("...");
            else if (state == CustomerOrderState.AwaitingSettlement) bubble.Show("结账");
            else bubble.Hide();
        }

        private void SetCustomerVisible(bool visible)
        {
            foreach (var renderer in customerRenderers) renderer.enabled = visible;
        }
    }
}

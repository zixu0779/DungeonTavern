using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public enum BusinessDayState
    {
        Preparing,
        Serving,
        Completed
    }

    public enum CustomerArrivalKind { Solitary, Sociable, Party, Random }

    [Serializable]
    public sealed class CustomerScheduleEntry
    {
        [SerializeField] private string displayName = "Guest";
        [SerializeField, Min(0f)] private float arrivalTime;
        [SerializeField] private HeldItem orderItem = HeldItem.TestDrink;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private List<OrderRequest> orderItems = new();
        [SerializeField] private CustomerArrivalKind arrivalKind;
        public CustomerArrivalKind ArrivalKind => arrivalKind;
        public void SetArrivalKind(CustomerArrivalKind kind) => arrivalKind = kind;
        public IEnumerable<OrderRequest> OrderItems => orderItems.Count > 0
            ? orderItems : new[] { new OrderRequest { item = orderItem } };

        public string DisplayName => displayName;
        public float ArrivalTime => arrivalTime;
        public Color Tint => tint;

        public void Configure(
            string name,
            float time,
            HeldItem item,
            Color customerTint)
        {
            displayName = name;
            arrivalTime = Mathf.Max(0f, time);
            orderItem = item;
            tint = customerTint;
            orderItems.Clear();
        }
    }

    public sealed class BusinessDayController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private Transform guestEntry;
        [SerializeField] private SeatRegistry seatRegistry;
        [SerializeField] private bool autoStart = true;

        [Header("Authored Schedule")]
        [SerializeField] private List<CustomerScheduleEntry> customers = new();

        [Header("Ordinary customers (in addition to the authored story schedule)")]
        [SerializeField, Min(0)] private int ordinaryArrivals;
        [SerializeField, Min(1)] private float ordinaryArrivalInterval = 12f;
        [Tooltip("Relative weights. Party weight is excluded when no empty large round table is available.")]
        [SerializeField] private Vector3 arrivalWeights = new(6, 3, 1);
        private int additionalPartyMembers;
        private int nextPartyId;
        private readonly List<CustomerScheduleEntry> pendingCustomers = new();
        private readonly List<CustomerServicePoint> activeCustomers = new();
        private float elapsedTime;
        private int nextCustomerIndex;

        public BusinessDayState State { get; private set; } = BusinessDayState.Preparing;
        public int TotalCustomers => (State == BusinessDayState.Preparing ? customers.Count + ordinaryArrivals : pendingCustomers.Count) + additionalPartyMembers;
        public int CompletedCustomers { get; private set; }
        public int ActiveCustomers => activeCustomers.Count;
        public int WaitingCustomers => pendingCustomers.Count - nextCustomerIndex;

        public event Action ProgressChanged;
        public event Action DayCompleted;
        public event Action<CustomerServicePoint> CustomerSpawned;

        private void Start()
        {
            if (autoStart)
                BeginDay();
        }

        public bool BeginDay()
        {
            if (State != BusinessDayState.Preparing || !enabled)
                return false;

            // The tavern content is hidden during the opening in B1.
            // Validate its active service points when business actually begins.
            if (!ValidateConfiguration())
                return false;

            pendingCustomers.Clear();
            pendingCustomers.AddRange(customers);
            for (int i = 0; i < ordinaryArrivals; i++)
            {
                var guest = new CustomerScheduleEntry();
                guest.Configure($"Guest_{i + 1}", (i + 1) * ordinaryArrivalInterval, HeldItem.TestDrink, Color.white);
                guest.SetArrivalKind(CustomerArrivalKind.Random);
                pendingCustomers.Add(guest);
            }
            additionalPartyMembers = 0;
            nextPartyId = 0;
            pendingCustomers.Sort((left, right) => left.ArrivalTime.CompareTo(right.ArrivalTime));
            elapsedTime = 0f;
            nextCustomerIndex = 0;
            CompletedCustomers = 0;
            State = BusinessDayState.Serving;
            Debug.Log($"Business day started: {TotalCustomers} customers scheduled.", this);
            ProgressChanged?.Invoke();
            return true;
        }

        public void ConfigureDayOne()
        {
            autoStart = false;
            customers.Clear();
            CustomerScheduleEntry bran = new();
            bran.Configure("Bran", 0.5f, HeldItem.TestDrink, new Color(1f, 0.78f, 0.62f));
            customers.Add(bran);
        }

        private void Update()
        {
            if (State != BusinessDayState.Serving)
                return;

            elapsedTime += Time.deltaTime;

            while (nextCustomerIndex < pendingCustomers.Count
                   && pendingCustomers[nextCustomerIndex].ArrivalTime <= elapsedTime)
            {
                if (!TrySpawn(pendingCustomers[nextCustomerIndex]))
                    break;

                nextCustomerIndex++;
            }

            TryCompleteDay();
        }

        public CustomerSeatingKind ChooseOrdinaryKind(float roll)
        {
            float solitary = Mathf.Max(0, arrivalWeights.x), sociable = Mathf.Max(0, arrivalWeights.y);
            float party = seatRegistry.CanSeatParty(2) ? Mathf.Max(0, arrivalWeights.z) : 0;
            float total = solitary + sociable + party;
            if (total <= 0) return CustomerSeatingKind.Solitary;
            float value = Mathf.Clamp(roll, 0, .999999f) * total;
            return value < solitary ? CustomerSeatingKind.Solitary
                : value < solitary + sociable ? CustomerSeatingKind.Sociable : CustomerSeatingKind.Party;
        }

        private bool TrySpawn(CustomerScheduleEntry entry)
        {
            if (!seatRegistry.HasAvailableSeat) return false;
            var kind = entry.ArrivalKind == CustomerArrivalKind.Random ? ChooseOrdinaryKind(UnityEngine.Random.value)
                : (CustomerSeatingKind)entry.ArrivalKind;
            int count = 1;
            if (kind == CustomerSeatingKind.Party)
            {
                int capacity = 4;
                while (capacity >= 2 && !seatRegistry.CanSeatParty(capacity)) capacity--;
                if (capacity < 2) return false;
                count = UnityEngine.Random.Range(2, capacity + 1);
            }
            var group = new List<CustomerServicePoint>();
            SeatPoint[] reserved = null;
            try
            {
                int partyId = count > 1 ? ++nextPartyId : 0;
                for (int i = 0; i < count; i++)
                {
                    var instance = Instantiate(customerPrefab, guestEntry.position, Quaternion.identity);
                    var customer = instance.GetComponent<CustomerServicePoint>();
                    if (customer == null) customer = instance.AddComponent<CustomerServicePoint>();
                    customer.ConfigureSeating(kind, partyId);
                    group.Add(customer);
                }
                if (count > 1)
                {
                    if (!seatRegistry.TryReserveParty(group, out reserved)) return false;
                }
                else
                {
                    if (!seatRegistry.TryReserve(group[0], out var seat)) return false;
                    reserved = new[] { seat };
                }
                for (int i = 0; i < count; i++)
                    group[i].Initialize(count > 1 ? $"{entry.DisplayName}_{i + 1}" : entry.DisplayName,
                        entry.OrderItems, guestEntry, seatRegistry, entry.Tint, reserved[i]);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
                reserved = null;
                return false;
            }
            finally
            {
                if (reserved == null)
                    foreach (var customer in group) { seatRegistry.Release(customer); Destroy(customer.gameObject); }
            }
            additionalPartyMembers += count - 1;
            foreach (var customer in group)
            {
                activeCustomers.Add(customer);
                customer.Finished += OnCustomerFinished;
                CustomerSpawned?.Invoke(customer);
            }
            ProgressChanged?.Invoke();
            return true;
        }

        private void OnCustomerFinished(CustomerServicePoint customer)
        {
            customer.Finished -= OnCustomerFinished;
            seatRegistry.Release(customer);
            activeCustomers.Remove(customer);
            CompletedCustomers++;
            Debug.Log($"Business day progress: {CompletedCustomers}/{TotalCustomers} customers complete.", this);
            ProgressChanged?.Invoke();
            Destroy(customer.gameObject);
            TryCompleteDay();
        }

        private void TryCompleteDay()
        {
            if (State != BusinessDayState.Serving
                || nextCustomerIndex < pendingCustomers.Count
                || activeCustomers.Count > 0)
            {
                return;
            }

            State = BusinessDayState.Completed;
            Debug.Log($"Business day complete: {CompletedCustomers}/{TotalCustomers} customers served.", this);
            ProgressChanged?.Invoke();
            DayCompleted?.Invoke();
        }

        private bool ValidateConfiguration()
        {
            if (customerPrefab == null || guestEntry == null || seatRegistry == null)
            {
                Debug.LogError("Business day requires a customer prefab, GuestEntry, and SeatRegistry.", this);
                return false;
            }

            if (customers.Count == 0 && ordinaryArrivals == 0)
            {
                Debug.LogError("Business day requires at least one scheduled customer.", this);
                return false;
            }

            var menu = FindAnyObjectByType<TavernMenuSystem>();
            if (menu == null || FindAnyObjectByType<ServiceOrderQueue>() == null || GameObject.Find("MenuApproach") == null)
            {
                Debug.LogError("Business day requires a menu, ordering queue and MenuApproach.", this);
                return false;
            }
            try
            {
                foreach (var entry in customers) _ = new CustomerOrder(entry.OrderItems, menu.FindDish);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"Invalid customer menu configuration: {exception.Message}", this);
                return false;
            }

            seatRegistry.RefreshSeats();
            if (seatRegistry.SeatCount == 0)
            {
                Debug.LogError("Business day requires at least one SeatPoint.", this);
                return false;
            }

            return true;
        }
    }
}

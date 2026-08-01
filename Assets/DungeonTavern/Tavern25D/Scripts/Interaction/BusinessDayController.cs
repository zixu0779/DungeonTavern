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

    [Serializable]
    public sealed class CustomerScheduleEntry
    {
        [SerializeField] private string displayName = "Guest";
        [SerializeField, Min(0f)] private float arrivalTime;
        [SerializeField] private HeldItem orderItem = HeldItem.TestDrink;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private bool requiresSettlement;

        public string DisplayName => displayName;
        public float ArrivalTime => arrivalTime;
        public HeldItem OrderItem => orderItem;
        public Color Tint => tint;
        public bool RequiresSettlement => requiresSettlement;

        public void Configure(
            string name,
            float time,
            HeldItem item,
            Color customerTint,
            bool settlement)
        {
            displayName = name;
            arrivalTime = Mathf.Max(0f, time);
            orderItem = item;
            tint = customerTint;
            requiresSettlement = settlement;
        }
    }

    public sealed class BusinessDayController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private Transform guestEntry;
        [SerializeField] private SeatRegistry seatRegistry;
        [SerializeField] private Transform settlementPoint;
        [SerializeField] private bool autoStart = true;

        [Header("Authored Schedule")]
        [SerializeField] private List<CustomerScheduleEntry> customers = new();

        private readonly List<CustomerScheduleEntry> pendingCustomers = new();
        private readonly List<CustomerServicePoint> activeCustomers = new();
        private float elapsedTime;
        private int nextCustomerIndex;

        public BusinessDayState State { get; private set; } = BusinessDayState.Preparing;
        public int TotalCustomers => customers.Count;
        public int CompletedCustomers { get; private set; }
        public int ActiveCustomers => activeCustomers.Count;
        public int WaitingCustomers => TotalCustomers - nextCustomerIndex;

        public event Action ProgressChanged;
        public event Action DayCompleted;
        public event Action<CustomerServicePoint> CustomerSpawned;

        private void Start()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            if (autoStart)
                BeginDay();
        }

        public bool BeginDay()
        {
            if (State != BusinessDayState.Preparing || !enabled)
                return false;

            pendingCustomers.Clear();
            pendingCustomers.AddRange(customers);
            pendingCustomers.Sort((left, right) => left.ArrivalTime.CompareTo(right.ArrivalTime));
            elapsedTime = 0f;
            nextCustomerIndex = 0;
            CompletedCustomers = 0;
            State = BusinessDayState.Serving;
            Debug.Log($"Business day started: {TotalCustomers} customers scheduled.", this);
            ProgressChanged?.Invoke();
            return true;
        }

        public void ConfigureDayOne(Transform billSettlementPoint)
        {
            autoStart = false;
            settlementPoint = billSettlementPoint;
            customers.Clear();
            CustomerScheduleEntry bran = new();
            bran.Configure("Bran", 0.5f, HeldItem.TestDrink, new Color(1f, 0.78f, 0.62f), true);
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

        private bool TrySpawn(CustomerScheduleEntry entry)
        {
            if (!seatRegistry.HasAvailableSeat)
                return false;

            GameObject instance = Instantiate(customerPrefab, guestEntry.position, Quaternion.identity);
            CustomerServicePoint customer = instance.GetComponent<CustomerServicePoint>();
            if (customer == null)
                customer = instance.AddComponent<CustomerServicePoint>();

            if (!seatRegistry.TryReserve(customer, out SeatPoint seat))
            {
                Destroy(instance);
                return false;
            }

            activeCustomers.Add(customer);
            customer.Finished += OnCustomerFinished;
            customer.Initialize(
                entry.DisplayName,
                entry.OrderItem,
                guestEntry,
                seat,
                entry.Tint,
                entry.RequiresSettlement ? settlementPoint : null);
            Debug.Log($"Customer spawned: {entry.DisplayName}; active {ActiveCustomers}, pending {WaitingCustomers}.", this);
            CustomerSpawned?.Invoke(customer);
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

            if (customers.Count == 0)
            {
                Debug.LogError("Business day requires at least one scheduled customer.", this);
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

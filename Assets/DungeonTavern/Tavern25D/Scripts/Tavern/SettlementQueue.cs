using System.Collections.Generic;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class SettlementQueue : MonoBehaviour
    {
        [SerializeField] private Vector3 queueDirection = new(-1f, 0f, 0f);
        [SerializeField, Min(0.6f)] private float spacing = 1.1f;

        private readonly List<CustomerServicePoint> customers = new();

        public void Enqueue(CustomerServicePoint customer)
        {
            if (customer != null && !customers.Contains(customer))
                customers.Add(customer);
        }

        public void Remove(CustomerServicePoint customer)
        {
            customers.Remove(customer);
        }

        public bool IsFirst(CustomerServicePoint customer)
        {
            RemoveMissing();
            return customers.Count > 0 && customers[0] == customer;
        }

        public Vector3 GetPosition(CustomerServicePoint customer)
        {
            RemoveMissing();
            int index = Mathf.Max(0, customers.IndexOf(customer));
            Vector3 direction = queueDirection.sqrMagnitude < 0.01f ? Vector3.left : queueDirection.normalized;
            direction.y = 0f;
            return transform.position + direction * spacing * index;
        }

        private void RemoveMissing()
        {
            customers.RemoveAll(customer => customer == null);
        }
    }
}

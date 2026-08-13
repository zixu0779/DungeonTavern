using System.Collections.Generic;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class ServiceOrderQueue : MonoBehaviour
    {
        [SerializeField] private List<Transform> queuePoints = new();
        private readonly List<CustomerServicePoint> customers = new();

        public void Configure(IEnumerable<Transform> points)
        {
            queuePoints.Clear();
            queuePoints.AddRange(points);
        }

        public void Enqueue(CustomerServicePoint customer)
        {
            RemoveMissing();
            if (customer != null && !customers.Contains(customer))
                customers.Add(customer);
        }

        public void Remove(CustomerServicePoint customer) => customers.Remove(customer);

        public bool IsFirst(CustomerServicePoint customer)
        {
            RemoveMissing();
            return customers.Count > 0 && customers[0] == customer;
        }

        public Vector3 GetPosition(CustomerServicePoint customer)
        {
            RemoveMissing();
            int index = Mathf.Max(0, customers.IndexOf(customer));
            if (queuePoints.Count == 0)
                return transform.position;
            return queuePoints[Mathf.Min(index, queuePoints.Count - 1)].position;
        }

        private void RemoveMissing() => customers.RemoveAll(customer => customer == null);
    }
}

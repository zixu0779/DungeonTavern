using System.Collections.Generic;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class ServiceOrderQueue : MonoBehaviour
    {
        [SerializeField] private List<Transform> queuePoints = new();
        [SerializeField] private Transform menuAnchor;
        public Transform MenuAnchor => menuAnchor;
        public int Count { get { RemoveMissing(); return customers.Count; } }
        public void BindMenu(Transform anchor) => menuAnchor = anchor;
        private readonly List<CustomerServicePoint> customers = new();

        public void Configure(IEnumerable<Transform> points)
        {
            queuePoints.Clear();
            queuePoints.AddRange(points);
            queuePoints.RemoveAll(point => point == null);
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
            queuePoints.RemoveAll(point => point == null);
            int index = Mathf.Max(0, customers.IndexOf(customer));
            Vector3 anchor = menuAnchor == null ? transform.position : menuAnchor.position;
            if (queuePoints.Count == 0)
                return anchor - (menuAnchor == null ? transform.forward : menuAnchor.forward) * index;
            int authored = Mathf.Min(index, queuePoints.Count - 1);
            // Queue markers are authored in world space; only the head is the menu anchor.
            // Never translate a second time when the menu or its anchor moves.
            Vector3 position = index == 0 ? anchor : queuePoints[authored].position;
            if (index >= queuePoints.Count)
            {
                Vector3 direction = queuePoints.Count > 1
                    ? (queuePoints[^1].position - queuePoints[^2].position).normalized
                    : -(menuAnchor == null ? transform.forward : menuAnchor.forward);
                if (direction.sqrMagnitude < .01f) direction = Vector3.back;
                position += direction * (index - queuePoints.Count + 1);
            }
            return position;
        }

        private void RemoveMissing() => customers.RemoveAll(customer => customer == null);
    }
}

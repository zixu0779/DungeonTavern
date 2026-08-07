using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    [RequireComponent(typeof(CustomerServicePoint))]
    public sealed class PrototypeCustomerArrival : MonoBehaviour
    {
        [SerializeField] private Transform guestEntry;
        [SerializeField] private Transform targetSeat;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.2f;

        private CustomerServicePoint servicePoint;

        public bool HasReachedSeat { get; private set; }

        private void Awake()
        {
            servicePoint = GetComponent<CustomerServicePoint>();
            servicePoint.enabled = false;

            if (guestEntry == null || targetSeat == null)
            {
                Debug.LogError("Customer arrival requires both GuestEntry and TestSeat references.", this);
                enabled = false;
                return;
            }

            transform.position = guestEntry.position;
        }

        private void Update()
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetSeat.position,
                moveSpeed * Time.deltaTime);

            Vector3 direction = targetSeat.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            if ((transform.position - targetSeat.position).sqrMagnitude > 0.0004f)
                return;

            transform.position = targetSeat.position;
            HasReachedSeat = true;
            servicePoint.enabled = true;
            Debug.Log("Day 1 customer arrival complete: test customer reached TestSeat.", this);
            enabled = false;
        }
    }
}

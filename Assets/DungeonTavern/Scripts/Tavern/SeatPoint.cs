using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class SeatPoint : MonoBehaviour
    {
        [SerializeField] private SeatingTable table;
        [SerializeField] private Transform chair;
        [SerializeField] private bool standing;
        [Tooltip("Long table: immediate neighbours on the same side and the seat directly opposite.")]
        [SerializeField] private SeatPoint[] neighbours = System.Array.Empty<SeatPoint>();
        public CustomerServicePoint Occupant { get; private set; }
        public SeatingTable Table => table;
        public Transform Chair => chair;
        public bool IsStanding => standing;
        public bool IsAvailable => Occupant == null;
        public Vector3 Position => transform.position;
        public bool HasPersonalSpace
        {
            get { foreach (var seat in neighbours) if (seat != null && !seat.IsAvailable) return false; return true; }
        }
        public void Configure(SeatingTable owner, Transform stool, bool isStanding, SeatPoint[] nearby)
        { table = owner; chair = stool; standing = isStanding; neighbours = nearby; }
        public bool TryReserve(CustomerServicePoint customer)
        {
            if (!IsAvailable || customer == null) return false;
            Occupant = customer;
            return true;
        }
        public void Release(CustomerServicePoint customer) { if (Occupant == customer) Occupant = null; }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = standing ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(Position, .28f);
            Gizmos.DrawLine(Position, Position + transform.forward * .6f);
            if (chair != null) Gizmos.DrawLine(Position, chair.position);
        }
    }
}

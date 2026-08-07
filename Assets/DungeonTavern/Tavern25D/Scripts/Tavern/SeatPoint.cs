using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class SeatPoint : MonoBehaviour
    {
        public CustomerServicePoint Occupant { get; private set; }

        public bool IsAvailable => Occupant == null;

        public Vector3 Position => transform.position;

        public bool TryReserve(CustomerServicePoint customer)
        {
            if (!IsAvailable || customer == null)
                return false;

            Occupant = customer;
            return true;
        }

        public void Release(CustomerServicePoint customer)
        {
            if (Occupant == customer)
                Occupant = null;
        }
    }
}

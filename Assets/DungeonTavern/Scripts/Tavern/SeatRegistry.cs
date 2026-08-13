using System.Collections.Generic;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class SeatRegistry : MonoBehaviour
    {
        private readonly List<SeatPoint> seats = new();

        public int SeatCount => seats.Count;

        public bool HasAvailableSeat
        {
            get
            {
                for (int index = 0; index < seats.Count; index++)
                {
                    if (seats[index].IsAvailable)
                        return true;
                }

                return false;
            }
        }

        private void Awake()
        {
            RefreshSeats();
        }

        public void RefreshSeats()
        {
            seats.Clear();
            GetComponentsInChildren(true, seats);
        }

        public bool TryReserve(CustomerServicePoint customer, out SeatPoint seat)
        {
            for (int index = 0; index < seats.Count; index++)
            {
                if (seats[index].TryReserve(customer))
                {
                    seat = seats[index];
                    return true;
                }
            }

            seat = null;
            return false;
        }

        public void Release(CustomerServicePoint customer)
        {
            for (int index = 0; index < seats.Count; index++)
                seats[index].Release(customer);
        }
    }
}

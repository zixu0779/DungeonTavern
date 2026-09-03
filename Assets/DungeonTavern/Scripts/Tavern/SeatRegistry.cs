using System.Collections.Generic;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class SeatRegistry : MonoBehaviour
    {
        [SerializeField] private Transform tableRoot;
        private readonly List<SeatPoint> seats = new();
        private readonly List<SeatingTable> tables = new();
        public IReadOnlyList<SeatPoint> Seats => seats;
        public IReadOnlyList<SeatingTable> Tables => tables;
        public int SeatCount => seats.FindAll(s => !s.IsStanding).Count;
        public bool HasAvailableSeat => seats.Exists(s => s != null && s.isActiveAndEnabled && s.IsAvailable
            && Rank(s, CustomerSeatingKind.Sociable) < int.MaxValue);
        private void Awake() => RefreshSeats();
        public void Configure(Transform root) { tableRoot = root; RefreshSeats(); }
        public void RefreshSeats()
        {
            seats.Clear(); tables.Clear();
            GetComponentsInChildren(true, seats);
            if (tableRoot != null)
            {
                tableRoot.GetComponentsInChildren(true, tables);
                foreach (var table in tables)
                    foreach (var seat in table.Seats)
                        if (seat != null && !seats.Contains(seat)) seats.Add(seat);
            }
        }
        public bool CanSeatParty(int count) => FindPartyTable(count) != null;
        private SeatingTable FindPartyTable(int count)
        {
            if (count < 2 || count > 4) return null;
            return tables.Find(t => t.isActiveAndEnabled && t.TableType == SeatingTableType.LargeRound
                && t.IsEmpty && System.Array.FindAll(t.Seats, s => s != null && s.isActiveAndEnabled).Length >= count);
        }
        // Select and reserve every member in one synchronous operation; no partially admitted parties.
        public bool TryReserveParty(IReadOnlyList<CustomerServicePoint> party, out SeatPoint[] assigned)
        {
            assigned = null;
            if (party == null) return false;
            var table = FindPartyTable(party.Count);
            if (table == null) return false;
            var unique = new HashSet<CustomerServicePoint>();
            foreach (var member in party)
                if (member == null || !unique.Add(member) || seats.Exists(s => s.Occupant == member)) return false;
            var available = System.Array.FindAll(table.Seats, s => s != null && s.isActiveAndEnabled);
            assigned = new SeatPoint[party.Count];
            for (int i = 0; i < party.Count; i++)
            {
                assigned[i] = available[i];
                if (assigned[i].TryReserve(party[i])) continue;
                for (int j = 0; j < i; j++) assigned[j].Release(party[j]);
                assigned = null; return false;
            }
            return true;
        }
        public bool TryReserve(CustomerServicePoint customer, out SeatPoint seat)
        {
            seat = null;
            if (customer == null) return false;
            seat = seats.Find(s => s.Occupant == customer);
            if (seat != null) return true;
            if (customer.SeatingKind == CustomerSeatingKind.Party) return false;
            int best = int.MaxValue;
            foreach (var candidate in seats)
            {
                if (!candidate.isActiveAndEnabled || !candidate.IsAvailable) continue;
                int rank = Rank(candidate, customer.SeatingKind);
                if (rank < best) { best = rank; seat = candidate; }
            }
            return seat != null && seat.TryReserve(customer);
        }
        private static int Rank(SeatPoint seat, CustomerSeatingKind kind)
        {
            if (seat.IsStanding) return 5;
            var table = seat.Table;
            if (table == null) return 0; // Legacy authored markers remain usable in isolated test scenes.
            if (!table.isActiveAndEnabled) return int.MaxValue;
            if (table.IsEmpty)
                return table.TableType switch { SeatingTableType.Long => 0, SeatingTableType.SmallRound => 1, _ => 4 };
            if (table.TableType != SeatingTableType.Long) return int.MaxValue;
            return kind == CustomerSeatingKind.Sociable || seat.HasPersonalSpace ? 2 : 3;
        }
        public void Release(CustomerServicePoint customer)
        { foreach (var seat in seats) if (seat != null) seat.Release(customer); }
    }
}

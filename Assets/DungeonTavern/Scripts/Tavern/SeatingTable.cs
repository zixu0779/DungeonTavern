using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public enum SeatingTableType { Long, SmallRound, LargeRound }
    public enum CustomerSeatingKind { Solitary, Sociable, Party }

    public sealed class SeatingTable : MonoBehaviour
    {
        [SerializeField] private SeatingTableType tableType;
        [SerializeField] private SeatPoint[] seats = System.Array.Empty<SeatPoint>();
        public SeatingTableType TableType => tableType;
        public SeatPoint[] Seats => seats;
        public bool IsEmpty
        {
            get { foreach (var seat in seats) if (seat != null && !seat.IsAvailable) return false; return true; }
        }
        public void Configure(SeatingTableType type, SeatPoint[] points) { tableType = type; seats = points; }
    }
}

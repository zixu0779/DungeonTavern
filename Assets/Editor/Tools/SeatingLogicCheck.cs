using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonTavern.Gameplay.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class SeatingLogicCheck
{
    [MenuItem("Tools/Dungeon Tavern/Seating/Check Seating Rules")]
    static void Check()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        GameObject Make(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
        void Assert(bool pass, string message) { if (!pass) throw new Exception(message); }
        try
        {
            var registry = Make("Registry").AddComponent<SeatRegistry>();
            var root = Make("Tables").transform;
            SeatingTable Table(SeatingTableType type, int count)
            {
                var table = Make(type.ToString()).AddComponent<SeatingTable>(); table.transform.SetParent(root);
                var seats = Enumerable.Range(0, count).Select(i => { var s = Make("Seat" + i).AddComponent<SeatPoint>(); s.transform.SetParent(table.transform); return s; }).ToArray();
                for (int i = 0; i < count; i++) seats[i].Configure(table, null, false, type != SeatingTableType.Long ? Array.Empty<SeatPoint>()
                    : seats.Where((s, j) => j == (i + 3) % 6 || (i / 3 == j / 3 && Math.Abs(i - j) == 1)).ToArray());
                table.Configure(type, seats); return table;
            }
            var longTable = Table(SeatingTableType.Long, 6); var small = Table(SeatingTableType.SmallRound, 2);
            var large = Table(SeatingTableType.LargeRound, 4);
            var stand = Make("Standing").AddComponent<SeatPoint>(); stand.transform.SetParent(registry.transform); stand.Configure(null, null, true, Array.Empty<SeatPoint>());
            registry.Configure(root);
            CustomerServicePoint Guest(CustomerSeatingKind kind = CustomerSeatingKind.Solitary)
            { var c = Make("Guest").AddComponent<CustomerServicePoint>(); c.ConfigureSeating(kind); return c; }
            var a = Guest(); Assert(registry.TryReserve(a, out var first) && first.Table == longTable, "Empty long first");
            Assert(registry.TryReserve(a, out var repeat) && repeat == first, "Duplicate reservation must be idempotent");
            var b = Guest(); Assert(registry.TryReserve(b, out var second) && second.Table == small, "Empty small before shared long");
            var c = Guest(); Assert(registry.TryReserve(c, out var quiet) && quiet == longTable.Seats[2], "Solitary must avoid neighbour and opposite");
            var d = Guest(CustomerSeatingKind.Sociable); Assert(registry.TryReserve(d, out var social) && social == longTable.Seats[1], "Sociable doesn't rank personal space");
            // Fill remaining long seats; large round may not be chosen while any long seat remains.
            for (int i = 0; i < 3; i++) Assert(registry.TryReserve(Guest(), out var s) && s.Table == longTable, "Crowded long before large round");
            Assert(registry.TryReserve(Guest(), out var round) && round.Table == large, "Empty large fallback");
            Assert(registry.TryReserve(Guest(), out var standing) && standing == stand, "Do not join occupied round tables");
            Assert(!registry.TryReserve(Guest(), out _), "Full standing area cannot overlap");
            Assert(!registry.CanSeatParty(2), "Occupied large table must disable party generation");
            foreach (var s in registry.Seats) if (s.Occupant != null) registry.Release(s.Occupant);
            var party = new[] { Guest(CustomerSeatingKind.Party), Guest(CustomerSeatingKind.Party) };
            Assert(registry.TryReserveParty(party, out var partySeats), "Reserve two-person party");
            Assert(partySeats.All(s => s.Table == large) && !registry.CanSeatParty(2), "Party owns whole table including unused stools");
            Assert(!registry.TryReserveParty(new[] { Guest(), Guest() }, out _), "Second party cannot share");
            registry.Release(party[0]); Assert(!registry.CanSeatParty(2), "First departure cannot release whole table");
            UnityEngine.Object.DestroyImmediate(party[1].gameObject); Assert(registry.CanSeatParty(4), "Destroyed last member must free table");
            Assert(!registry.TryReserveParty(new[] { a, a }, out _) && large.IsEmpty, "Duplicate member rollback");
            Assert(!registry.TryReserveParty(new CustomerServicePoint[] { a, null }, out _) && large.IsEmpty, "Null member rollback");
            var fullParty = Enumerable.Range(0, 4).Select(i => Guest(CustomerSeatingKind.Party)).ToArray();
            Assert(registry.TryReserveParty(fullParty, out var four) && four.Distinct().Count() == 4, "Four distinct seats reserved atomically");
            var day = Make("Business").AddComponent<BusinessDayController>();
            typeof(BusinessDayController).GetField("seatRegistry", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(day, registry);
            Assert(Enumerable.Range(0, 1000).All(i => day.ChooseOrdinaryKind(i / 1000f) != CustomerSeatingKind.Party), "Full large tables exclude random parties");
            foreach (var member in fullParty) registry.Release(member);
            var counts = Enumerable.Range(0, 1000).Select(i => day.ChooseOrdinaryKind(i / 1000f)).GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count());
            Assert(counts[CustomerSeatingKind.Solitary] > counts[CustomerSeatingKind.Sociable] && counts[CustomerSeatingKind.Sociable] > counts[CustomerSeatingKind.Party], "Weighted frequency order");
            Directory.CreateDirectory((Path.Combine(Path.GetTempPath(), "DungeonTavern/Seating") + Path.DirectorySeparatorChar));
            File.WriteAllText((Path.Combine(Path.GetTempPath(), "DungeonTavern/Seating") + Path.DirectorySeparatorChar) + "rules.txt", "PASS: all solo priorities, personal-space relation, idempotent reservation, occupied round exclusion, standing limit, 2/4-person exclusive party reservation, partial departure, destroyed member, invalid-party rollback, generation capacity gate and weighted frequencies.\n");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}

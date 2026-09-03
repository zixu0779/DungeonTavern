using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonTavern.Gameplay.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

internal static class SeatingSetup
{
    public const string Output = "ArtSource/Previews/Seating/";
    [MenuItem("Tools/Dungeon Tavern/Seating/Audit All Seat Navigation")]
    static void AuditNavigation()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Tavern/Tavern_Main.unity");
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var registry = all.Select(t => t.GetComponent<SeatRegistry>()).Single(c => c != null);
        var menu = all.Select(t => t.GetComponent<ServiceOrderQueue>()).Single(c => c != null).MenuAnchor;
        registry.RefreshSeats();
        var log = new StringBuilder();
        if (!NavMesh.SamplePosition(menu.position, out var origin, .5f, NavMesh.AllAreas))
            log.AppendLine("NO NAVMESH AT MENU: cannot assess seat reachability in this editor state.");
        else foreach (var seat in registry.Seats)
        {
            var path = new NavMeshPath();
            bool sampled = NavMesh.SamplePosition(seat.Position, out var hit, 1.5f, NavMesh.AllAreas);
            bool complete = sampled && NavMesh.CalculatePath(origin.position, hit.position, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete;
            float offset = sampled ? Vector3.Distance(seat.Position, hit.position) : float.PositiveInfinity;
            log.AppendLine($"{(complete && offset <= .6f ? "OK" : "CHECK")} {seat.transform.parent.name}/{seat.name} target={seat.Position} reachable={complete} sampleOffset={offset:F2} sampled={hit.position}");
        }
        Directory.CreateDirectory(Output); File.WriteAllText(Output + "navigation-audit.txt", log.ToString());
    }
    [MenuItem("Tools/Dungeon Tavern/Seating/Validate Seat Navigation")]
    static void Navigation()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play Mode first.");
        var registry = UnityEngine.Object.FindAnyObjectByType<SeatRegistry>(); registry.RefreshSeats();
        var menu = UnityEngine.Object.FindAnyObjectByType<ServiceOrderQueue>().MenuAnchor;
        if (!NavMesh.SamplePosition(menu.position, out var origin, .5f, NavMesh.AllAreas)) throw new Exception("No editor NavMesh at menu");
        var report = new StringBuilder(); var path = new NavMeshPath();
        foreach (var seat in registry.Seats)
        {
            bool Reach(Vector3 p, out NavMeshHit hit) => NavMesh.SamplePosition(p, out hit, .15f, NavMesh.AllAreas)
                && NavMesh.CalculatePath(origin.position, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
            if (Reach(seat.Position, out _)) continue;
            float best = float.MaxValue; Vector3 chosen = seat.Position;
            // Move only the navigation marker to the nearest reachable place beside its authored stool.
            var center = seat.Chair == null ? seat.Position : new Vector3(seat.Chair.position.x, 0, seat.Chair.position.z);
            for (float radius = .4f; radius <= 1.21f; radius += .2f)
                for (int angle = 0; angle < 360; angle += 15)
                {
                    var candidate = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
                    if (!Reach(candidate, out var hit)) continue;
                    if (seat.Table != null && Vector3.Dot(hit.position - center, center - seat.Table.transform.position) < -.1f) continue;
                    if (registry.Seats.Any(s => s != seat && Vector3.Distance(s.Position, hit.position) < .6f)) continue;
                    float cost = Vector3.SqrMagnitude(hit.position - seat.Position);
                    if (cost < best) { best = cost; chosen = hit.position; }
                }
            if (best == float.MaxValue) { report.AppendLine("FAIL no nearby approach: " + seat.transform.parent.name + "/" + seat.name); continue; }
            report.AppendLine($"Adjusted {seat.transform.parent.name}/{seat.name}: {seat.Position} -> {chosen}");
            Undo.RecordObject(seat.transform, "Make seat approach reachable"); seat.transform.position = chosen;
            if (seat.Table != null) { var toward = seat.Table.transform.position - chosen; toward.y = 0; seat.transform.rotation = Quaternion.LookRotation(toward); }
        }
        EditorSceneManager.MarkSceneDirty(registry.gameObject.scene); EditorSceneManager.SaveScene(registry.gameObject.scene);
        File.WriteAllText(Output + "navigation.txt", report.ToString());
    }
    [MenuItem("Tools/Dungeon Tavern/Seating/Bind Placed Tables And Chairs")]
    static void Bind()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play Mode first.");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Tavern/Tavern_Main.unity");
        if (!scene.isLoaded) throw new Exception("Open Tavern_Main first.");
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var root = all.Single(t => t.name == "TableSets");
        var registry = all.Select(t => t.GetComponent<SeatRegistry>()).Single(c => c != null);
        var groups = root.Cast<Transform>().Where(t => t.name.StartsWith("TableSet_")).OrderBy(t => t.name).ToArray();
        if (groups.Length != 9) throw new Exception("Expected nine authored table groups.");
        var preserved = all.ToDictionary(t => t, t => t.localToWorldMatrix);
        Directory.CreateDirectory(Output);
        string backup = "ArtSource/Backups/Seating_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(backup); EditorSceneManager.SaveScene(scene); File.Copy(scene.path, backup + "/Tavern_Main.unity");
        var log = new StringBuilder();
        foreach (var group in groups)
        {
            var stools = group.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Stool_Wood_Round_")).OrderBy(t => t.name).ToArray();
            var type = group.name.Contains("Rectangular") ? SeatingTableType.Long
                : group.name.Contains("Pedestal") ? SeatingTableType.SmallRound : SeatingTableType.LargeRound;
            if (stools.Length != (type == SeatingTableType.Long ? 6 : type == SeatingTableType.SmallRound ? 2 : 4))
                throw new Exception("Unexpected stool count: " + group.name);
            var table = group.GetComponent<SeatingTable>() ?? Undo.AddComponent<SeatingTable>(group.gameObject);
            var points = stools.Select((stool, i) =>
            {
                var t = group.Find("Seat_" + (i + 1).ToString("00"));
                if (t == null) { var go = new GameObject("Seat_" + (i + 1).ToString("00")); Undo.RegisterCreatedObjectUndo(go, "Create seat marker"); t = go.transform; t.SetParent(group, false); }
                var outward = stool.position - group.position; outward.y = 0; outward.Normalize();
                Undo.RecordObject(t, "Align seat marker");
                t.position = new Vector3(stool.position.x, 0, stool.position.z) + outward * .65f;
                t.rotation = Quaternion.LookRotation(-outward);
                return t.GetComponent<SeatPoint>() ?? Undo.AddComponent<SeatPoint>(t.gameObject);
            }).ToArray();
            for (int i = 0; i < points.Length; i++)
            {
                var neighbours = type != SeatingTableType.Long ? Array.Empty<SeatPoint>()
                    : points.Where((p, j) => j == (i + 3) % 6 || (i / 3 == j / 3 && Math.Abs(i - j) == 1)).ToArray();
                Undo.RecordObject(points[i], "Configure seat"); points[i].Configure(table, stools[i], false, neighbours); EditorUtility.SetDirty(points[i]);
                log.AppendLine($"{group.name}/{points[i].name}: {points[i].Position}; neighbours={string.Join(",", neighbours.Select(n => n.name))}");
            }
            Undo.RecordObject(table, "Configure table"); table.Configure(type, points); EditorUtility.SetDirty(table);
        }
        // Retain old marker transforms for external references, but retire their SeatPoint components.
        foreach (var old in registry.GetComponentsInChildren<SeatPoint>(true).Where(s => !s.IsStanding).ToArray()) Undo.DestroyObjectImmediate(old);
        var standing = registry.transform.Find("StandingArea");
        if (standing == null) { var go = new GameObject("StandingArea"); Undo.RegisterCreatedObjectUndo(go, "Create standing area"); standing = go.transform; standing.SetParent(registry.transform, false); }
        for (int i = 0; i < 8; i++)
        {
            var t = standing.Find("Stand_" + (i + 1).ToString("00"));
            if (t == null) { var go = new GameObject("Stand_" + (i + 1).ToString("00")); Undo.RegisterCreatedObjectUndo(go, "Create standing point"); t = go.transform; t.SetParent(standing, false); }
            Undo.RecordObject(t, "Place standing point"); t.position = new Vector3(28 + i % 2 * 2, 0, 8 + i / 2 * 1.5f);
            t.rotation = Quaternion.LookRotation(Vector3.left);
            var point = t.GetComponent<SeatPoint>() ?? Undo.AddComponent<SeatPoint>(t.gameObject);
            Undo.RecordObject(point, "Configure standing point"); point.Configure(null, null, true, Array.Empty<SeatPoint>()); EditorUtility.SetDirty(point);
        }
        Undo.RecordObject(registry, "Bind tables to registry"); registry.Configure(root); EditorUtility.SetDirty(registry);
        foreach (var pair in preserved) if (pair.Key != null && pair.Key.localToWorldMatrix != pair.Value) throw new Exception("Existing transform moved: " + pair.Key.name);
        if (registry.SeatCount != 42) throw new Exception("Expected 42 seats.");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        log.AppendLine("PASS: 9 tables / 42 seats / 8 standing positions. All existing transforms unchanged. Backup: " + backup);
        File.WriteAllText(Output + "setup.txt", log.ToString());
    }
}

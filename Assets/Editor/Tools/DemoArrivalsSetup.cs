using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using DungeonTavern.Gameplay.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class DemoArrivalsSetup
{
    [MenuItem("Tools/Dungeon Tavern/Seating/Configure Six Demo Arrivals")]
    static void Configure()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play Mode first.");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Tavern/Tavern_Main.unity");
        var day = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BusinessDayController>(true)).Single();
        EditorSceneManager.SaveScene(scene);
        string backup = "ArtSource/Backups/DemoArrivals_20260918/Tavern_Main_" + DateTime.Now.ToString("HHmmss") + ".unity";
        File.Copy(scene.path, backup);
        var so = new SerializedObject(day);
        so.FindProperty("ordinaryArrivals").intValue = 6;
        so.FindProperty("ordinaryArrivalInterval").floatValue = 30;
        so.FindProperty("maxConcurrentCustomers").intValue = 5;
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("ArtSource/Previews/Seating/demo-arrivals.txt", "Scene saved: Bran + 6 ordinary batches, 30 seconds between batches, at most 5 active people. One random batch is guaranteed Party. Ordinary arrivals start after authored guests leave. Backup: " + backup + "\n");
    }
    [MenuItem("Tools/Dungeon Tavern/Seating/Check Demo Arrival Schedule")]
    static void Check()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play Mode first.");
        var source = UnityEngine.Object.FindAnyObjectByType<BusinessDayController>();
        var preview = EditorSceneManager.NewPreviewScene(); var randomState = UnityEngine.Random.state;
        object Get(object o, string name) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
        void Set(object o, string name, object value) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(o,value);
        void Assert(bool value, string reason) { if (!value) throw new Exception(reason); }
        try
        {
            for (int seed = 0; seed < 100; seed++)
            {
                UnityEngine.Random.InitState(seed);
                var go = new GameObject("ScheduleCheck"); SceneManager.MoveGameObjectToScene(go, preview);
                var day = go.AddComponent<BusinessDayController>(); EditorUtility.CopySerialized(source, day);
                Assert(day.BeginDay(), "Cannot prepare schedule");
                var list = (List<CustomerScheduleEntry>)Get(day,"pendingCustomers");
                Assert(list.Count == 7 && list[0].DisplayName == "Bran", "Expected Bran plus six batches");
                Assert(list.Skip(1).Count(e=>e.ArrivalKind == CustomerArrivalKind.Party) == 1, "Missing guaranteed party batch");
                Assert(list.Skip(1).All(e=>float.IsPositiveInfinity(e.ArrivalTime)), "Ordinary arrivals started before teaching ended");
                // Pretend the authored guest has left: next frame schedules the first ordinary arrival.
                Set(day,"nextCustomerIndex",1); Set(day,"elapsedTime",100f);
                typeof(BusinessDayController).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(day,null);
                Assert(list[1].ArrivalTime >= 130f && list[1].ArrivalTime < 131f, "First ordinary arrival not delayed 30 seconds");
                // Saturate admission using dummy customers: retry must be deferred, not skipped or burst-spawned.
                var active=(List<CustomerServicePoint>)Get(day,"activeCustomers");
                for (int i=0;i<5;i++) active.Add(go.AddComponent<CustomerServicePoint>());
                Set(day,"elapsedTime",list[1].ArrivalTime+1);
                typeof(BusinessDayController).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(day,null);
                Assert((int)Get(day,"nextCustomerIndex")==1 && list[1].ArrivalTime>=161f,"Full capacity must defer the same batch");
                UnityEngine.Object.DestroyImmediate(go);
            }
            File.AppendAllText("ArtSource/Previews/Seating/demo-arrivals.txt", "PASS: 100 seeds retain Bran + six batches, exactly one guaranteed party slot, teaching gate, 30-second initial delay, five-person capacity deferral without skipping batches.\n");
        }
        finally { UnityEngine.Random.state = randomState; EditorSceneManager.ClosePreviewScene(preview); }
    }
}

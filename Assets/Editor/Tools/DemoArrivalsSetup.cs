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
    [MenuItem("Tools/Dungeon Tavern/Seating/Check Demo Arrival Schedule")]
    static void Check()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
        var source=UnityEngine.Object.FindObjectsByType<BusinessDayController>(FindObjectsInactive.Include).Single();
        var so=new SerializedObject(source);
        if(!so.FindProperty("demoService").boolValue || so.FindProperty("ordinaryArrivalInterval").floatValue!=6
            ||so.FindProperty("ordinaryArrivals").intValue!=6)throw new Exception("Expected six ordinary waves at six-second intervals");
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var go=new GameObject("ScheduleCheck");SceneManager.MoveGameObjectToScene(go,preview);
            var day=go.AddComponent<BusinessDayController>();EditorUtility.CopySerialized(source,day);
            if(!day.BeginDay())throw new Exception("Could not prepare schedule");
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var list=(List<CustomerScheduleEntry>)typeof(BusinessDayController).GetField("pendingCustomers",flags).GetValue(day);
            if(list.Count!=7||list[0].DisplayName!="Bran")throw new Exception("Expected Bran plus six ordinary waves");
            var kinds=new[]{CustomerArrivalKind.Party,CustomerArrivalKind.Sociable,CustomerArrivalKind.Solitary};
            for(int i=0;i<6;i++)
                if(list[i+1].ArrivalTime!=(i+1)*6||(i<3&&list[i+1].ArrivalKind!=kinds[i]))throw new Exception("Timing/type guarantee changed");
            typeof(BusinessDayController).GetField("nextCustomerIndex",flags).SetValue(day,list.Count);
            typeof(BusinessDayController).GetField("elapsedTime",flags).SetValue(day,600f);
            typeof(BusinessDayController).GetMethod("Update",flags).Invoke(day,null);
            if(list.Count!=7||day.State!=BusinessDayState.Completed)throw new Exception("Unexpected replenishment after six waves");
            File.WriteAllText("/tmp/demo-arrivals-config.txt","PASS: Bran plus exactly six ordinary waves; 6s spacing; Party/Sociable/Solitary guaranteed; exhausted schedule completes without generating a seventh wave.\n");
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
    }
}

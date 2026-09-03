using System;
using System.IO;
using System.Linq;
using DungeonTavern.Gameplay.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
internal static class CustomerQueueLogicCheck
{
    [MenuItem("Tools/Dungeon Tavern/Check Queue Edge Cases")]
    static void Check()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        GameObject Make(string name){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);return go;}
        void Assert(bool value,string message){if(!value)throw new Exception(message);}
        try
        {
            var parent=Make("Menu").transform;parent.position=new Vector3(10,0,10);
            var anchor=Make("Anchor").transform;anchor.SetParent(parent,false);
            var queue=Make("Queue").AddComponent<ServiceOrderQueue>();queue.transform.SetParent(parent,false);
            var points=Enumerable.Range(0,3).Select(i=>{var point=Make("Point"+i).transform;point.SetParent(parent,false);point.localPosition=Vector3.right*i*1.2f;return point;}).ToArray();
            queue.Configure(points);queue.BindMenu(anchor);
            var customers=Enumerable.Range(0,6).Select(i=>Make("Guest"+i).AddComponent<CustomerServicePoint>()).ToArray();
            foreach(var c in customers)queue.Enqueue(c);
            queue.Enqueue(customers[0]);Assert(queue.Count==6,"Duplicate enqueue");
            Assert(queue.IsFirst(customers[0])&&!queue.IsFirst(customers[1]),"FIFO head");
            for(int i=1;i<customers.Length;i++)Assert(Vector3.Distance(queue.GetPosition(customers[i]),queue.GetPosition(customers[i-1]))>.8f,"Queue tail overlaps after authored markers run out");
            var before=customers.Select(queue.GetPosition).ToArray();parent.position+=Vector3.forward*4;
            for(int i=0;i<customers.Length;i++)Assert(Vector3.Distance(queue.GetPosition(customers[i]),before[i]+Vector3.forward*4)<.001f,"Menu movement double-offset queue");
            queue.Remove(customers[0]);Assert(queue.IsFirst(customers[1]),"Head cancellation fails to advance");
            UnityEngine.Object.DestroyImmediate(customers[1].gameObject);Assert(queue.IsFirst(customers[2]),"Destroyed head blocks queue");
            queue.Configure(Array.Empty<Transform>());Assert(Vector3.Distance(queue.GetPosition(customers[2]),queue.GetPosition(customers[3]))>.8f,"Fallback points overlap");
            var order=new CustomerOrder(new[]{new OrderRequest{quantity=2}},item=>new DishDefinition{eatingSeconds=8});
            Assert(order.TryDeliver(HeldItem.TestDrink),"First drink delivery");order.Eat(4);
            Assert(Mathf.Abs(order.Portions[0].RemainingFraction-.5f)<.001f&&!order.TryPay(),"Eating progress or premature settlement");
            order.Eat(4);Assert(!order.AllConsumed,"Unserved portion lost");order.TryDeliver(HeldItem.TestDrink);order.Eat(8);
            Assert(order.TryPay()&&!order.TryPay(),"Duplicate settlement");
            Directory.CreateDirectory(CustomerQueueReview.Output);
            File.WriteAllText(CustomerQueueReview.Output+"edgechecks.txt","PASS: duplicate enqueue, FIFO, overflow spacing, menu movement, cancelled/destroyed head, empty marker fallback, icon consumption fraction, unserved portions and one-time settlement.\n");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonTavern.Gameplay.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class CustomerFlowCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    static void Tick(CustomerServicePoint customer, float time = 0, bool arrived = true) => Call(customer, "Tick", time, arrived);

    [MenuItem("Tools/Dungeon Tavern/Check Customer Service Flow")]
    static void Check()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var report = new List<string>();
        GameObject Make(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
        try
        {
            var menu = Make("Test menu").AddComponent<TavernMenuSystem>();
            Set(menu, "dishes", new List<DishDefinition> {
                new() { item=HeldItem.TestDrink, label="Drink", price=8, eatingSeconds=2 },
                new() { item=HeldItem.MainDish, label="Meal", price=12, eatingSeconds=4 }
            });
            Call(menu, "Awake");
            var queue = Make("Test queue").AddComponent<ServiceOrderQueue>();
            var entry = Make("Entry").transform;
            var anchor = Make("MenuApproach").transform;
            var registry = Make("Seats").AddComponent<SeatRegistry>();
            var seat1 = Make("Seat1").AddComponent<SeatPoint>(); seat1.transform.SetParent(registry.transform);
            var seat2 = Make("Seat2").AddComponent<SeatPoint>(); seat2.transform.SetParent(registry.transform);
            registry.RefreshSeats();
            CustomerServicePoint Customer(string name)
            {
                var c = Make(name).AddComponent<CustomerServicePoint>(); Call(c, "Awake");
                Set(c,"menuSystem",menu); Set(c,"serviceQueue",queue); Set(c,"menuPoint",anchor);
                c.Initialize(name, new[]{new OrderRequest{item=HeldItem.TestDrink,quantity=2},new OrderRequest{item=HeldItem.MainDish}},entry,registry,Color.white);
                return c;
            }
            var first = Customer("First"); var second = Customer("Second");
            Tick(first); Tick(second); Tick(second);
            Require(second.State == CustomerOrderState.QueueingForOrder, "Follower skipped queue");
            Tick(first,0,false); Require(first.State == CustomerOrderState.QueueingForOrder,"Ordering before arrival");
            Tick(first); Require(first.State == CustomerOrderState.Ordering,"Head did not order");
            Tick(first,2); Require(first.State == CustomerOrderState.FindingSeat,"Order not confirmed");
            Require(menu.PendingOrderCount==3,"Quantity lost");
            Tick(second); Require(second.State==CustomerOrderState.Ordering,"Queue did not advance");
            Tick(first); Require(first.State==CustomerOrderState.MovingToSeat,"Seat not assigned");
            Tick(first,0,false); Require(first.State==CustomerOrderState.MovingToSeat,"Sat before arrival");
            Tick(first); Require(first.State==CustomerOrderState.WaitingForFood,"Did not wait at seat");
            report.Add("PASS: FIFO before ordering, arrival gates, confirmation then seating.");
            var hands=Make("Hands").AddComponent<PlayerHands>();
            hands.TryHold(HeldItem.SideDish); Require(!first.Interact(hands)&&hands.CurrentItem==HeldItem.SideDish,"Wrong dish consumed"); hands.Clear();
            hands.TryHold(HeldItem.TestDrink); Require(first.Interact(hands),"First portion rejected");
            Tick(first,2); Require(first.State==CustomerOrderState.WaitingForFood&&!first.CompleteSettlement(),"Partial meal completed prematurely");
            hands.TryHold(HeldItem.TestDrink); Require(first.Interact(hands),"Second portion rejected");
            Tick(first,1);
            var pickup=Make("Food pickup").AddComponent<FoodPickupPoint>();
            Set(pickup,"menu",menu);
            Require(pickup.Interact(hands)&&!pickup.Interact(hands),"Food pickup overwrote occupied hands");
            Require(first.Interact(hands),"Delivery while eating rejected");
            Require(menu.PendingOrderCount==0&&first.Order.AllDelivered,"Pending counts wrong");
            Require(!first.CompleteSettlement(),"Paid before finishing food");
            Tick(first,4); Require(first.State==CustomerOrderState.Eating,"Skipped eating time");
            Tick(first,1); Require(first.State==CustomerOrderState.AwaitingSettlement,"Meal not completed");
            Require(seat1.Occupant==first,"Seat released before billing");
            report.Add("PASS: multiple dishes/quantities, wrong dish rejection, eating while awaiting remaining food.");
            int callbacks=0;
            first.SettlementRequested += c => { callbacks++; return true; };
            Require(first.Interact(hands)&&!first.Interact(hands)&&callbacks==1,"Narrative settlement repeated");
            Require(menu.Balance==500,"Balance changed before settlement completed");
            Require(first.CompleteSettlement()&&!first.CompleteSettlement()&&menu.Balance==528,"Payment duplicated or wrong amount");
            Require(seat1.IsAvailable,"Seat not released on departure");
            Tick(first); Require(first.State==CustomerOrderState.Finished,"Departure failed");
            report.Add("PASS: in-place narrative billing, single payment for all portions, seat release and exit.");
            Tick(second,2);Tick(second);Tick(second);
            foreach(var item in new[]{HeldItem.TestDrink,HeldItem.TestDrink,HeldItem.MainDish}) { hands.TryHold(item);Require(second.Interact(hands),"Second customer serve"); }
            Tick(second,8);Require(second.Interact(hands)&&menu.Balance==556,"Ordinary customer settlement needs narrative");
            var third=Customer("Cancelled"); Tick(third);Tick(third);Tick(third,2);Tick(third);
            // Preview-scene objects do not receive the normal Play Mode lifecycle.
            Call(third, "OnDestroy");
            UnityEngine.Object.DestroyImmediate(third.gameObject);
            Require(menu.PendingOrderCount==0&&seat1.IsAvailable,"Cancellation leaked order or seat");
            report.Add("PASS: ordinary billing fallback and destroyed-customer cleanup.");
            bool invalid=false;try { new CustomerOrder(new[]{new OrderRequest{quantity=0}},menu.FindDish); }catch(ArgumentException){invalid=true;}
            Require(invalid,"Invalid quantity accepted");
            report.Add("PASS: invalid order rejected. These are service-flow integration checks with simulated arrival, not a navigation/animation playthrough.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        Directory.CreateDirectory("ArtSource/Previews/CustomerFlow");
        File.WriteAllLines("ArtSource/Previews/CustomerFlow/check.txt",report);
        Debug.Log(string.Join("\n",report));
    }
}

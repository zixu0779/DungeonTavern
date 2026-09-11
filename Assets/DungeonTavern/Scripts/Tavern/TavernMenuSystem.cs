using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTavern.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    public sealed class TavernMenuSystem : MonoBehaviour
    {
        [SerializeField] private List<DishDefinition> dishes = new() { new DishDefinition() };
        private readonly Dictionary<CustomerServicePoint, CustomerOrder> orders = new();
        public IReadOnlyList<DishDefinition> Dishes => dishes;
        public DishDefinition FindDish(HeldItem item) => dishes.FirstOrDefault(d => d.item == item);
        private bool isOpen;
        private int selectedTab;

        [SerializeField, Min(0)] private int startingMoney = 500;
        public int Balance { get; private set; }
        public int PendingOrderCount => orders.Values.Sum(o => o.Portions.Count(p => !p.Delivered));
        public event Action Changed;
        public event Action OrderRegistered;

        private void Awake() => Balance = startingMoney;

        private void Update()
        {
            if (GamePauseMenu.IsPaused) return;
            var narrative=FindAnyObjectByType<DungeonTavern.Tavern25D.Narrative.Day1NarrativeController>();
            if(narrative!=null && narrative.State==DungeonTavern.Tavern25D.Narrative.Day1FlowState.Dialogue)return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard?.mKey.wasPressedThisFrame == true)
                Toggle();
            if (isOpen && keyboard?.escapeKey.wasPressedThisFrame == true)
                isOpen = false;
        }

        public bool IsOpen => isOpen;
        public int SelectedTab { get => selectedTab; set => selectedTab = Mathf.Clamp(value,0,1); }
        public void Close() => isOpen=false;
        public bool CanOpen
        {
            get
            {
                var n=FindAnyObjectByType<DungeonTavern.Tavern25D.Narrative.Day1NarrativeController>();
                return n != null && n.ManagementUnlocked && n.State != DungeonTavern.Tavern25D.Narrative.Day1FlowState.Dialogue;
            }
        }
        public void Toggle()
        {
            if(!isOpen && !CanOpen)return;
            isOpen=!isOpen;
            if(isOpen)DungeonTavern.UI.TavernGuidance.Complete(DungeonTavern.UI.GuideStep.Menu);
        }

        public bool RegisterOrder(CustomerServicePoint customer, CustomerOrder order)
        {
            if (customer == null || order == null || orders.ContainsKey(customer)) return false;
            orders.Add(customer, order);
            OrderRegistered?.Invoke();
            Changed?.Invoke();
            return true;
        }

        public bool TryServe(CustomerServicePoint customer, HeldItem item)
        {
            if (!orders.TryGetValue(customer, out var order) || !order.TryDeliver(item)) return false;
            DungeonTavern.UI.TavernGuidance.Complete(DungeonTavern.UI.GuideStep.Serve);
            Changed?.Invoke();
            return true;
        }

        public void CancelOrder(CustomerServicePoint customer)
        {
            if (orders.Remove(customer)) Changed?.Invoke();
        }

        public int Count(HeldItem item) => orders.Values.Sum(o => o.Portions.Count(p => p.Item == item && !p.Delivered));
        public IReadOnlyList<CustomerServicePoint> GetCustomers(HeldItem item) => orders
            .Where(pair => pair.Key != null && pair.Value.Needs(item)).Select(pair => pair.Key).ToArray();

        public bool CompleteSale(CustomerServicePoint customer)
        {
            if (!orders.TryGetValue(customer, out var order) || !order.TryPay()) return false;
            Balance += order.Total;
            DungeonTavern.UI.TavernGuidance.Complete(DungeonTavern.UI.GuideStep.Settle);
            orders.Remove(customer);
            Changed?.Invoke();
            return true;
        }

        public static string GetLabel(HeldItem item) => item switch
        {
            HeldItem.EmptyCup => "空酒杯", HeldItem.TestDrink => "麦芽饮料", HeldItem.MainDish => "主菜", HeldItem.SideDish => "配菜", _ => item.ToString()
        };




    }
}

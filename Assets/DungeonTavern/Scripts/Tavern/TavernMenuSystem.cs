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
        public int PendingOrderCount => UniquePortions.Count(p => !p.Delivered);
        public event Action Changed;
        public event Action OrderRegistered;

        private void Awake()
        {
            Balance = startingMoney;
            dishes = new List<DishDefinition> {
                new() { item=HeldItem.MainDish,label="铁锅洞菇炖肉",price=24,eatingSeconds=5 },
                new() { item=HeldItem.CaveBoarPlatter,label="炭烤穴猪拼盘",price=28,eatingSeconds=6 },
                new() { item=HeldItem.SideDish,label="盐焗岩薯",price=7,eatingSeconds=3 },
                new() { item=HeldItem.RootBread,label="黑麦根面包",price=6,eatingSeconds=3 },
                new() { item=HeldItem.PickledFern,label="酸渍洞蕨",price=5,eatingSeconds=2.5f },
                new() { item=HeldItem.TestDrink,label="深窖麦芽酒",price=8,eatingSeconds=3 },
                new() { item=HeldItem.GlowcapAle,label="幽菇淡艾尔",price=10,eatingSeconds=3 },
                new() { item=HeldItem.CinderMead,label="余烬蜂蜜酒",price=12,eatingSeconds=3 }
            };
        }
        public static bool IsDrink(HeldItem item) => item is HeldItem.TestDrink or HeldItem.GlowcapAle or HeldItem.CinderMead;
        public static bool IsSharedDish(HeldItem item) => item is HeldItem.MainDish or HeldItem.CaveBoarPlatter;
        private IEnumerable<CustomerOrder.Portion> UniquePortions => orders.Values.SelectMany(o=>o.Portions).Distinct();
        public int MissingServingCups => orders.Values.SelectMany(o=>o.Portions.Where(p=>!p.Delivered&&(o.AllowDrinkSubstitute||IsDrink(p.Item)))).Distinct().Count();

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

        public int Count(HeldItem item) => UniquePortions.Count(p => p.Item == item && !p.Delivered);
        public IReadOnlyList<CustomerServicePoint> GetCustomers(HeldItem item) => orders
            .Where(pair => pair.Key != null && pair.Value.Portions.Any(p=>p.Item==item&&!p.Delivered)).Select(pair => pair.Key).ToArray();

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
            HeldItem.EmptyCup => "空酒杯", HeldItem.TestDrink => "深窖麦芽酒", HeldItem.MainDish => "铁锅洞菇炖肉", HeldItem.SideDish => "盐焗岩薯",
            HeldItem.CaveBoarPlatter=>"炭烤穴猪拼盘",HeldItem.RootBread=>"黑麦根面包",HeldItem.PickledFern=>"酸渍洞蕨",HeldItem.GlowcapAle=>"幽菇淡艾尔",HeldItem.CinderMead=>"余烬蜂蜜酒", _ => item.ToString()
        };




    }
}

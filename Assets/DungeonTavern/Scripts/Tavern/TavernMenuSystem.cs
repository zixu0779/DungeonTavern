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
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle rowStyle;
        private bool isOpen;
        private int selectedTab;
        private Vector2 menuScroll;

        [SerializeField, Min(0)] private int startingMoney = 500;
        public int Balance { get; private set; }
        public int PendingOrderCount => orders.Values.Sum(o => o.Portions.Count(p => !p.Delivered));
        public event Action Changed;
        public event Action OrderRegistered;

        private void Awake() => Balance = startingMoney;

        private void Update()
        {
            if (GamePauseMenu.IsPaused) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard?.mKey.wasPressedThisFrame == true)
                Toggle();
            if (isOpen && keyboard?.escapeKey.wasPressedThisFrame == true)
                isOpen = false;
        }

        public void Toggle() => isOpen = !isOpen;

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
            orders.Remove(customer);
            Changed?.Invoke();
            return true;
        }

        public static string GetLabel(HeldItem item) => item switch
        {
            HeldItem.EmptyCup => "空酒杯", HeldItem.TestDrink => "麦芽饮料", HeldItem.MainDish => "主菜", HeldItem.SideDish => "配菜", _ => item.ToString()
        };

        private void OnGUI()
        {
            if (GamePauseMenu.IsPaused) return;
            GUI.depth = -4500;
            EnsureStyles();
            GUI.Box(new Rect(Screen.width - 286f, 14f, 272f, 64f), $"存款  {Balance} G", titleStyle);
            if (!isOpen) return;
            float scale = Mathf.Clamp(Screen.height / 900f, .6f, 1.4f);
            float width = Mathf.Min(940f * scale, Screen.width - 40f);
            float height = Mathf.Min(540f * scale, Screen.height - 100f);
            Rect panel = new((Screen.width - width) * .5f, (Screen.height - height) * .5f, width, height);
            Color previous = GUI.color;
            GUI.color = new Color(.13f, .11f, .09f, .98f); GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = previous;
            GUI.Label(new Rect(panel.x + 24 * scale, panel.y + 18 * scale, width - 100 * scale, 48 * scale), "酒馆菜单", headerStyle);
            var buttons = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(24 * scale) };
            if (GUI.Button(new Rect(panel.xMax - 65 * scale, panel.y + 18 * scale, 45 * scale, 42 * scale), "×", buttons)) isOpen = false;
            selectedTab = GUI.Toolbar(new Rect(panel.x + 24 * scale, panel.y + 82 * scale, width - 48 * scale, 48 * scale), selectedTab, new[] { "菜品总览", "具体订单" }, buttons);
            Rect content = new(panel.x + 28 * scale, panel.y + 150 * scale, width - 56 * scale, height - 180 * scale);
            if (selectedTab == 1)
            {
                GUI.Label(content, "具体订单\n\n此页将在后续版本中显示逐笔订单。", rowStyle);
                return;
            }
            float inner = content.width - 20 * scale;
            string[] headers = { "菜品", "单价", "待上份数", "点单人数" };
            float[] columns = { 0, .4f, .58f, .8f };
            for (int i = 0; i < headers.Length; i++)
                GUI.Label(new Rect(content.x + inner * columns[i], content.y, inner * (i == 0 ? .4f : .2f), 42 * scale), headers[i], rowStyle);
            Rect viewport = new(content.x, content.y + 54 * scale, content.width, content.height - 54 * scale);
            menuScroll = GUI.BeginScrollView(viewport, menuScroll, new Rect(0, 0, inner, Mathf.Max(viewport.height, dishes.Count * 60 * scale)));
            float y = 0;
            foreach (var dish in dishes)
            {
                string[] values = { dish.label, $"{dish.price} G", Count(dish.item).ToString(), GetCustomers(dish.item).Count.ToString() };
                for (int i = 0; i < values.Length; i++)
                    GUI.Label(new Rect(inner * columns[i], y, inner * (i == 0 ? .4f : .2f), 56 * scale), values[i], rowStyle);
                y += 60 * scale;
            }
            GUI.EndScrollView();
        }

        private void EnsureStyles()
        {
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 36f), 18, 32);
            titleStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter };
            headerStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            rowStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
            titleStyle.fontSize = rowStyle.fontSize = fontSize;
            headerStyle.fontSize = fontSize + 4;
        }
    }
}

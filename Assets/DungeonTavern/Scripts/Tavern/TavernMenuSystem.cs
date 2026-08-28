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
        private GUIStyle panelStyle;
        private GUIStyle headerStyle;
        private GUIStyle rowStyle;
        private bool isOpen;

        [SerializeField, Min(0)] private int startingMoney = 500;
        public int Balance { get; private set; }
        public int PendingOrderCount => orders.Values.Sum(o => o.Portions.Count(p => !p.Delivered));
        public event Action Changed;

        private void Awake() => Balance = startingMoney;

        private void Update()
        {
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
            HeldItem.TestDrink => "麦芽饮料", HeldItem.MainDish => "主菜", HeldItem.SideDish => "配菜", _ => item.ToString()
        };

        private void OnGUI()
        {
            EnsureStyles();
            GUI.Box(new Rect(Screen.width - 286f, 14f, 272f, 64f), $"存款  {Balance} G", titleStyle);
            if (!isOpen)
                return;

            float width = Mathf.Min(920f, Screen.width - 56f);
            Rect panel = new((Screen.width - width) * 0.5f, 92f, width, Mathf.Min(180f + dishes.Count * 60f, Screen.height - 130f));
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 18f, width - 56f, 48f), "酒馆菜单", headerStyle);
            float y = panel.y + 82f;
            float inner = width - 56f;
            GUI.Label(new Rect(panel.x + 28f, y, inner * 0.34f, 42f), "菜名", rowStyle);
            GUI.Label(new Rect(panel.x + 28f + inner * 0.34f, y, inner * 0.18f, 42f), "金额", rowStyle);
            GUI.Label(new Rect(panel.x + 28f + inner * 0.52f, y, inner * 0.20f, 42f), "待上份数", rowStyle);
            GUI.Label(new Rect(panel.x + 28f + inner * 0.72f, y, inner * 0.28f, 42f), "点单人", rowStyle);
            y += 56f;
            foreach (var dish in dishes)
            {
                GUI.Label(new Rect(panel.x + 28f, y, inner * 0.34f, 56f), dish.label, rowStyle);
                GUI.Label(new Rect(panel.x + 28f + inner * 0.34f, y, inner * 0.18f, 56f), $"{dish.price} G", rowStyle);
                GUI.Label(new Rect(panel.x + 28f + inner * 0.52f, y, inner * 0.20f, 56f), Count(dish.item).ToString(), rowStyle);
                DrawCustomerPortraits(new Rect(panel.x + 28f + inner * 0.72f, y, inner * 0.28f, 56f), GetCustomers(dish.item));
                y += 60f;
            }
        }

        private void EnsureStyles()
        {
            int fontSize = Mathf.Max(28, Mathf.RoundToInt(Screen.height / 28f));
            titleStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = fontSize };
            panelStyle ??= new GUIStyle(GUI.skin.box) { padding = new RectOffset(24, 24, 18, 18) };
            headerStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = fontSize + 6, fontStyle = FontStyle.Bold };
            rowStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = fontSize };
        }

        private void DrawCustomerPortraits(Rect area, IReadOnlyList<CustomerServicePoint> customers)
        {
            float size = Mathf.Min(50f, area.height);
            for (int index = 0; index < customers.Count; index++)
            {
                SpriteRenderer renderer = customers[index] == null ? null : customers[index].GetComponentInChildren<SpriteRenderer>();
                Rect target = new(area.x + index * (size + 8f), area.y + (area.height - size) * 0.5f, size, size);
                if (renderer?.sprite?.texture != null)
                {
                    Sprite sprite = renderer.sprite;
                    Rect textureRect = sprite.textureRect;
                    Rect uv = new(textureRect.x / sprite.texture.width, textureRect.y / sprite.texture.height,
                        textureRect.width / sprite.texture.width, textureRect.height / sprite.texture.height);
                    GUI.DrawTextureWithTexCoords(target, sprite.texture, uv, true);
                }
                else
                    GUI.Label(target, "◇", rowStyle);
            }
        }
    }
}

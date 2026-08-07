using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    public sealed class TavernMenuSystem : MonoBehaviour
    {
        private sealed class Order
        {
            public CustomerServicePoint Customer;
            public HeldItem Item;
        }

        private readonly List<Order> orders = new();
        private PlayerHands activeHands;
        private GUIStyle titleStyle;
        private GUIStyle rowStyle;
        private GUIStyle smallStyle;
        private bool isOpen;

        [SerializeField, Min(0)] private int startingMoney = 500;
        public int Balance { get; private set; }
        public int PendingOrderCount => orders.Count;
        public bool IsOpen => isOpen;

        private void Awake() => Balance = startingMoney;

        public bool Toggle(PlayerHands hands)
        {
            if (hands == null)
                return false;
            activeHands = hands;
            isOpen = !isOpen;
            return true;
        }

        public bool RegisterOrder(CustomerServicePoint customer, HeldItem item)
        {
            if (customer == null || item == HeldItem.None || orders.Any(order => order.Customer == customer))
                return false;
            orders.Add(new Order { Customer = customer, Item = item });
            return true;
        }

        public bool TryPrepare(HeldItem item, PlayerHands hands)
        {
            if (hands == null || hands.CurrentItem != HeldItem.None || !orders.Any(order => order.Item == item))
                return false;
            bool held = hands.TryHold(item);
            if (held)
                isOpen = false;
            return held;
        }

        public bool TryServe(CustomerServicePoint customer, HeldItem item)
        {
            Order order = orders.FirstOrDefault(candidate => candidate.Customer == customer && candidate.Item == item);
            if (order == null)
                return false;
            orders.Remove(order);
            return true;
        }

        public int Count(HeldItem item) => orders.Count(order => order.Item == item);

        public void CompleteSale(HeldItem item) => Balance += GetPrice(item);

        public static int GetPrice(HeldItem item) => item == HeldItem.TestDrink ? 8 : 0;
        public static string GetLabel(HeldItem item) => item == HeldItem.TestDrink ? "麦芽饮料" : item.ToString();

        private void OnGUI()
        {
            EnsureStyles();
            GUI.Box(new Rect(Screen.width - 286f, 14f, 272f, 64f), $"存款  {Balance} G", titleStyle);
            if (!isOpen)
                return;

            float width = Mathf.Min(860f, Screen.width - 56f);
            float height = Mathf.Min(560f, Screen.height - 80f);
            Rect panel = new((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 32f, panel.y + 22f, panel.width - 64f, 58f), "酒馆菜单 · 当前订单", titleStyle);
            DrawItemRow(panel, HeldItem.TestDrink, 104f);
            GUI.Label(new Rect(panel.x + 32f, panel.yMax - 68f, panel.width - 64f, 42f), "F 关闭菜单 · 点击菜品即可制作一份", smallStyle);
        }

        private void DrawItemRow(Rect panel, HeldItem item, float offsetY)
        {
            int count = Count(item);
            Rect row = new(panel.x + 32f, panel.y + offsetY, panel.width - 64f, 126f);
            GUI.Box(row, GUIContent.none);
            GUI.Label(new Rect(row.x + 24f, row.y + 16f, 280f, 46f), GetLabel(item), rowStyle);
            GUI.Label(new Rect(row.x + 310f, row.y + 16f, 150f, 46f), $"{GetPrice(item)} G", rowStyle);
            GUI.Label(new Rect(row.x + 470f, row.y + 16f, 180f, 46f), $"待制作 × {count}", rowStyle);

            float avatarX = row.x + 24f;
            foreach (Order order in orders.Where(order => order.Item == item))
            {
                DrawAvatar(new Rect(avatarX, row.y + 70f, 42f, 42f), order.Customer);
                avatarX += 50f;
            }

            GUI.enabled = count > 0 && activeHands != null && activeHands.CurrentItem == HeldItem.None;
            if (GUI.Button(new Rect(row.xMax - 160f, row.y + 68f, 136f, 44f), "制作") && TryPrepare(item, activeHands))
                Debug.Log($"Order prepared from menu: {GetLabel(item)}.", this);
            GUI.enabled = true;
        }

        private static void DrawAvatar(Rect rect, CustomerServicePoint customer)
        {
            SpriteRenderer renderer = customer == null ? null : customer.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                Sprite sprite = renderer.sprite;
                Rect textureRect = sprite.textureRect;
                Rect uv = new(
                    textureRect.x / sprite.texture.width,
                    textureRect.y / sprite.texture.height,
                    textureRect.width / sprite.texture.width,
                    textureRect.height / sprite.texture.height);
                Color previous = GUI.color;
                GUI.color = renderer.color;
                GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
                GUI.color = previous;
                return;
            }
            GUI.Box(rect, customer == null ? "?" : customer.CustomerName[..1]);
        }

        private void EnsureStyles()
        {
            int fontSize = Mathf.Max(28, Mathf.RoundToInt(Screen.height / 28f));
            titleStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = fontSize };
            rowStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = fontSize };
            smallStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(22, fontSize - 6) };
        }
    }
}

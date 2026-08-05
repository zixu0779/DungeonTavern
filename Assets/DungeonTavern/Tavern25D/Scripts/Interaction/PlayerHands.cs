using System;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public enum HeldItem
    {
        None,
        TestDrink
    }

    public sealed class PlayerHands : MonoBehaviour
    {
        private PlayerOrderBook orderBook;

        public HeldItem CurrentItem { get; private set; }
        public PlayerOrderBook OrderBook => orderBook ??= GetComponent<PlayerOrderBook>()
            ?? gameObject.AddComponent<PlayerOrderBook>();

        public event Action<HeldItem> ItemChanged;

        private void Awake()
        {
            orderBook = GetComponent<PlayerOrderBook>();
            if (orderBook == null)
                orderBook = gameObject.AddComponent<PlayerOrderBook>();
        }

        public bool TryHold(HeldItem item)
        {
            if (item == HeldItem.None || CurrentItem != HeldItem.None)
                return false;

            CurrentItem = item;
            ItemChanged?.Invoke(CurrentItem);
            return true;
        }

        public void Clear()
        {
            if (CurrentItem == HeldItem.None)
                return;

            CurrentItem = HeldItem.None;
            ItemChanged?.Invoke(CurrentItem);
        }
    }
}

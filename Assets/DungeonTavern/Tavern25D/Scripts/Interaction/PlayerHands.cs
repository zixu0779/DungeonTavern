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
        public HeldItem CurrentItem { get; private set; }

        public event Action<HeldItem> ItemChanged;

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

using System;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public enum PlayerOrderState
    {
        None,
        Accepted,
        Prepared
    }

    [DisallowMultipleComponent]
    public sealed class PlayerOrderBook : MonoBehaviour
    {
        public CustomerServicePoint Customer { get; private set; }
        public HeldItem OrderedItem { get; private set; }
        public PlayerOrderState State { get; private set; }

        public bool HasOrder => Customer != null && State != PlayerOrderState.None;
        public event Action Changed;

        public bool TryAccept(CustomerServicePoint customer, HeldItem item)
        {
            if (customer == null || item == HeldItem.None || HasOrder)
                return false;
            Customer = customer;
            OrderedItem = item;
            State = PlayerOrderState.Accepted;
            Changed?.Invoke();
            return true;
        }

        public bool TryMarkPrepared(HeldItem item)
        {
            if (!HasOrder || State != PlayerOrderState.Accepted || OrderedItem != item)
                return false;
            State = PlayerOrderState.Prepared;
            Changed?.Invoke();
            return true;
        }

        public bool TryComplete(CustomerServicePoint customer, HeldItem item)
        {
            if (Customer != customer || OrderedItem != item || State != PlayerOrderState.Prepared)
                return false;
            Customer = null;
            OrderedItem = HeldItem.None;
            State = PlayerOrderState.None;
            Changed?.Invoke();
            return true;
        }
    }
}

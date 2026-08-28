using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    [Serializable]
    public sealed class OrderRequest
    {
        public HeldItem item = HeldItem.TestDrink;
        [Min(1)] public int quantity = 1;
    }

    // Each entry represents one portion, so duplicate dishes can arrive and be eaten separately.
    public sealed class CustomerOrder
    {
        public sealed class Portion
        {
            public HeldItem Item { get; }
            public int Price { get; }
            public float SecondsRemaining { get; private set; }
            public bool Delivered { get; private set; }
            public bool Consumed => Delivered && SecondsRemaining <= 0;
            public Portion(HeldItem item, int price, float seconds)
            { Item = item; Price = price; SecondsRemaining = Mathf.Max(.01f, seconds); }
            internal void Deliver() => Delivered = true;
            internal void Eat(float seconds) => SecondsRemaining = Mathf.Max(0, SecondsRemaining - seconds);
        }

        private readonly List<Portion> portions = new();
        public IReadOnlyList<Portion> Portions => portions.AsReadOnly();
        public bool AllDelivered => portions.Count > 0 && portions.All(p => p.Delivered);
        public bool AllConsumed => portions.Count > 0 && portions.All(p => p.Consumed);
        public bool HasFood => portions.Any(p => p.Delivered && !p.Consumed);
        public int Total => portions.Sum(p => p.Price);
        public bool Paid { get; private set; }

        public CustomerOrder(IEnumerable<OrderRequest> requests, Func<HeldItem, DishDefinition> lookup)
        {
            if (requests == null) throw new ArgumentNullException(nameof(requests));
            foreach (var request in requests)
            {
                var dish = request == null ? null : lookup(request.item);
                if (dish == null || request.quantity < 1 || request.item == HeldItem.None || dish.price < 0 || dish.eatingSeconds <= 0)
                    throw new ArgumentException("Order contains an invalid or unavailable dish.");
                for (int i = 0; i < request.quantity; i++)
                    portions.Add(new Portion(request.item, dish.price, dish.eatingSeconds));
            }
            if (portions.Count == 0) throw new ArgumentException("Order must contain at least one portion.");
        }

        public bool Needs(HeldItem item) => portions.Any(p => p.Item == item && !p.Delivered);
        public bool TryDeliver(HeldItem item)
        {
            var portion = portions.FirstOrDefault(p => p.Item == item && !p.Delivered);
            if (portion == null || Paid) return false;
            portion.Deliver();
            return true;
        }
        public void Eat(float seconds)
        {
            if (seconds <= 0) return;
            foreach (var portion in portions.Where(p => p.Delivered && !p.Consumed))
            {
                float elapsed = Mathf.Min(seconds, portion.SecondsRemaining);
                portion.Eat(elapsed);
                seconds -= elapsed;
                if (seconds <= 0) break;
            }
        }
        public bool TryPay()
        {
            if (Paid || !AllConsumed) return false;
            Paid = true;
            return true;
        }
    }

    [Serializable]
    public sealed class DishDefinition
    {
        public HeldItem item = HeldItem.TestDrink;
        public string label = "麦芽饮料";
        [Min(0)] public int price = 8;
        [Min(.01f)] public float eatingSeconds = 3;
    }
}

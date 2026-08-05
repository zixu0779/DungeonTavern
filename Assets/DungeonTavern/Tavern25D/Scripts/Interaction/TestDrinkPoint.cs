using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class TestDrinkPoint : InteractionPoint
    {
        public override string GetPrompt(PlayerHands hands)
        {
            if (hands == null)
                return string.Empty;
            if (hands.CurrentItem != HeldItem.None)
                return "手上已经拿着物品";
            if (!hands.OrderBook.HasOrder)
                return "目前没有待制作的订单";
            if (hands.OrderBook.State == PlayerOrderState.Prepared)
                return "这份订单已经制作完成";
            return "F：制作麦芽饮料";
        }

        public override bool Interact(PlayerHands hands)
        {
            if (hands == null
                || !hands.OrderBook.TryMarkPrepared(HeldItem.TestDrink)
                || !hands.TryHold(HeldItem.TestDrink))
                return false;

            Debug.Log("Order prepared: player made the malt drink.", this);
            return true;
        }
    }
}

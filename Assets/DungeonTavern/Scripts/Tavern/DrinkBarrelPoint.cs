using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class DrinkBarrelPoint : InteractionPoint
    {
        public override string GetPrompt(PlayerHands hands)
        {
            if (hands == null)
                return string.Empty;
            return hands.CurrentItem == HeldItem.None
                ? "F：用木杯接取麦芽饮料"
                : "手上已经拿着物品";
        }

        public override bool Interact(PlayerHands hands)
        {
            if (hands == null || !hands.TryHold(HeldItem.TestDrink))
                return false;
            Debug.Log("Player filled a wooden cup from the oak barrel.", this);
            return true;
        }
    }
}

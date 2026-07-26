using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class TestDrinkPoint : InteractionPoint
    {
        public override string GetPrompt(PlayerHands hands)
        {
            return hands != null && hands.CurrentItem == HeldItem.None
                ? "F: Take test drink"
                : "Hands already occupied";
        }

        public override bool Interact(PlayerHands hands)
        {
            if (hands == null || !hands.TryHold(HeldItem.TestDrink))
                return false;

            Debug.Log("Day 1 interaction complete: player took the test drink.", this);
            return true;
        }
    }
}

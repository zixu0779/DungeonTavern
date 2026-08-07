using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class TestDrinkPoint : InteractionPoint
    {
        public override string GetPrompt(PlayerHands hands)
        {
            return hands == null ? string.Empty : "F：查看菜单与订单";
        }

        public override bool Interact(PlayerHands hands)
        {
            TavernMenuSystem menu = FindAnyObjectByType<TavernMenuSystem>();
            return menu != null && menu.Toggle(hands);
        }
    }
}

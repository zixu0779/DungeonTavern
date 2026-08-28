using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    // Attach to an authored serving point; cooking/stock are separate from pickup and delivery.
    public sealed class FoodPickupPoint : InteractionPoint
    {
        [SerializeField] private HeldItem item = HeldItem.MainDish;
        [SerializeField] private TavernMenuSystem menu;

        private DishDefinition Dish
        {
            get
            {
                if (menu == null) menu = FindAnyObjectByType<TavernMenuSystem>();
                return menu == null ? null : menu.FindDish(item);
            }
        }

        public override string GetPrompt(PlayerHands hands)
        {
            var dish = Dish;
            if (dish == null) return "此菜品尚未加入菜单";
            return hands == null || hands.CurrentItem != HeldItem.None
                ? "手上已经拿着物品" : $"F：取走{dish.label}";
        }

        public override bool Interact(PlayerHands hands) => Dish != null
            && hands != null && hands.TryHold(item);
    }
}

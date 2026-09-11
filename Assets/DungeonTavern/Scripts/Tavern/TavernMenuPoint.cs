namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class TavernMenuPoint : InteractionPoint
    {
        public override string GetPrompt(PlayerHands hands) => FindAnyObjectByType<TavernMenuSystem>() is { CanOpen: true } ? "F：查看订单" : string.Empty;

        public override bool Interact(PlayerHands hands)
        {
            TavernMenuSystem menu = FindAnyObjectByType<TavernMenuSystem>();
            if (menu == null || !menu.CanOpen)
                return false;
            menu.Toggle();
            return true;
        }
    }
}

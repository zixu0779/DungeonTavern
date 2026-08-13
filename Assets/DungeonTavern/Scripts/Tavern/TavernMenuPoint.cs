namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class TavernMenuPoint : InteractionPoint
    {
        public override string GetPrompt(PlayerHands hands) => "F：查看酒馆菜单（也可按 M）";

        public override bool Interact(PlayerHands hands)
        {
            TavernMenuSystem menu = FindAnyObjectByType<TavernMenuSystem>();
            if (menu == null)
                return false;
            menu.Toggle();
            return true;
        }
    }
}

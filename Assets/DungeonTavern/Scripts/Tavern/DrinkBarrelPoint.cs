namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class DrinkBarrelPoint : InteractionPoint
    {
        public override string GetPrompt(PlayerHands hands) =>
            hands != null && hands.CurrentItem == HeldItem.EmptyCup ? "F：接酒" : string.Empty;

        public override bool Interact(PlayerHands hands) => hands != null && hands.TryFillCup();
    }
}

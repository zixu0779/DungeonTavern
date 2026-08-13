namespace DungeonTavern.Gameplay.Interaction
{
    public interface IInteractable
    {
        string GetPrompt(PlayerHands hands);

        bool Interact(PlayerHands hands);
    }
}

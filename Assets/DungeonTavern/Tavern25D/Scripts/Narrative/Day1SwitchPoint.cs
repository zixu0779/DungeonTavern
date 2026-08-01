using DungeonTavern.Gameplay.Interaction;
using UnityEngine;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class Day1SwitchPoint : InteractionPoint
    {
        [SerializeField] private Day1NarrativeController narrative;

        public void Configure(Day1NarrativeController controller)
        {
            narrative = controller;
        }

        public override string GetPrompt(PlayerHands hands)
        {
            if (narrative == null)
                return string.Empty;
            if (narrative.CanOpenTavern)
                return "F: Open the tavern";
            if (narrative.CanCloseTavern)
                return "F: Close the tavern";
            return string.Empty;
        }

        public override bool Interact(PlayerHands hands)
        {
            return narrative != null && narrative.TryUseBusinessSwitch();
        }
    }
}

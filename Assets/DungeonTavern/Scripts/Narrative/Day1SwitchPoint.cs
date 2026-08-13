using DungeonTavern.Gameplay.Interaction;
using UnityEngine;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class Day1SwitchPoint : InteractionPoint
    {
        [SerializeField] private Day1NarrativeController narrative;
        [SerializeField] private BusinessRopeMechanism mechanism;

        public void Configure(Day1NarrativeController controller)
        {
            narrative = controller;
            mechanism = GetComponent<BusinessRopeMechanism>();
        }

        public override string GetPrompt(PlayerHands hands)
        {
            if (narrative == null)
                return string.Empty;
            if (narrative.CanOpenTavern)
                return "F：拉下吊绳，开始营业";
            if (narrative.CanCloseTavern)
                return "F：拉下吊绳，结束营业";
            return string.Empty;
        }

        public override bool Interact(PlayerHands hands)
        {
            if (narrative == null)
                return false;
            bool opening = narrative.CanOpenTavern;
            bool closing = narrative.CanCloseTavern;
            if (!narrative.TryUseBusinessSwitch())
                return false;
            mechanism ??= GetComponent<BusinessRopeMechanism>();
            mechanism?.PullAndSetOpen(opening && !closing);
            return true;
        }
    }
}

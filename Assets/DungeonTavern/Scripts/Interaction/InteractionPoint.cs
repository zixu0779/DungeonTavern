using System.Collections.Generic;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public abstract class InteractionPoint : MonoBehaviour, IInteractable
    {
        private static readonly List<InteractionPoint> ActivePoints = new();

        public static IReadOnlyList<InteractionPoint> Instances => ActivePoints;

        protected virtual void OnEnable()
        {
            if (!ActivePoints.Contains(this))
                ActivePoints.Add(this);
        }

        protected virtual void OnDisable()
        {
            ActivePoints.Remove(this);
        }

        public abstract string GetPrompt(PlayerHands hands);

        public abstract bool Interact(PlayerHands hands);
    }
}

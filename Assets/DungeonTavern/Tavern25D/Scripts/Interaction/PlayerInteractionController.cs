using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTavern.Gameplay.Interaction
{
    [RequireComponent(typeof(PlayerHands))]
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private Transform interactionOrigin;
        [SerializeField, Min(0.1f)] private float interactionRadius = 1.6f;

        private PlayerHands hands;
        private InteractionPoint currentTarget;

        public string CurrentPrompt => currentTarget == null
            ? string.Empty
            : currentTarget.GetPrompt(hands);

        public HeldItem CurrentItem => hands == null ? HeldItem.None : hands.CurrentItem;

        private void Awake()
        {
            hands = GetComponent<PlayerHands>();
            interactionOrigin ??= transform;
        }

        private void Update()
        {
            currentTarget = FindClosestTarget();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                TryInteract();
        }

        public bool TryInteract()
        {
            return currentTarget != null && currentTarget.Interact(hands);
        }

        private InteractionPoint FindClosestTarget()
        {
            Vector3 origin = interactionOrigin == null ? transform.position : interactionOrigin.position;
            float maximumDistanceSquared = interactionRadius * interactionRadius;
            float closestDistanceSquared = maximumDistanceSquared;
            InteractionPoint closest = null;

            IReadOnlyList<InteractionPoint> points = InteractionPoint.Instances;
            for (int index = 0; index < points.Count; index++)
            {
                InteractionPoint point = points[index];
                if (point == null || !point.isActiveAndEnabled)
                    continue;

                float distanceSquared = (point.transform.position - origin).sqrMagnitude;
                if (distanceSquared > closestDistanceSquared)
                    continue;

                closestDistanceSquared = distanceSquared;
                closest = point;
            }

            return closest;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = interactionOrigin == null ? transform : interactionOrigin;
            Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.75f);
            Gizmos.DrawWireSphere(origin.position, interactionRadius);
        }
    }
}

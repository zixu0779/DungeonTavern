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
            if (DungeonTavern.Gameplay.Interaction.GamePauseMenu.IsPaused || DungeonTavern.UI.TavernUI.WindowOpen) return;
            currentTarget = FindClosestTarget();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                TryInteract();
        }

        public bool TryInteract()
        {
            if (GamePauseMenu.IsPaused || DungeonTavern.UI.TavernUI.WindowOpen) return false;
            if (GetComponentInChildren<DungeonTavern.Tavern25D.CharacterModelMotion>() is { IsFullBodyAction: true }) return false;
            currentTarget = FindClosestTarget();
            return currentTarget != null && currentTarget.Interact(hands);
        }

        private InteractionPoint FindClosestTarget()
        {
            Vector3 origin = interactionOrigin == null ? transform.position : interactionOrigin.position;
            float bestScore=float.PositiveInfinity;
            InteractionPoint closest=null;
            foreach(var point in InteractionPoint.Instances)
            {
                if(!point||!point.isActiveAndEnabled||string.IsNullOrEmpty(point.GetPrompt(hands)))continue;
                var customer=point as CustomerServicePoint;
                Vector3 destination=customer?customer.ServicePosition:point.transform.position+Vector3.up;
                var offset=destination-(origin+Vector3.up);
                if(Mathf.Abs(offset.y)>2f)continue;
                offset.y=0;
                float distance=offset.magnitude;
                float radius=customer?2.4f:interactionRadius;
                if(distance>radius)continue;
                float facing=distance>.01f?Vector3.Dot(transform.forward,offset/distance):1;
                if(customer&&distance>.85f&&facing<-.25f)continue;
                if(IsObstructed(origin+Vector3.up*1.2f,destination,point))continue;
                float score=distance*(1+.7f*(1-facing));
                if(point==currentTarget)score*=.9f;
                if(score>=bestScore)continue;
                bestScore=score;closest=point;
            }

            return closest;
        }

        private bool IsObstructed(Vector3 from,Vector3 to,InteractionPoint target)
        {
            var delta=to-from;
            foreach(var hit in Physics.RaycastAll(from,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
            {
                var collider=hit.collider;
                if(collider.transform.IsChildOf(transform)||collider.transform.IsChildOf(target.transform)||target.transform.IsChildOf(collider.transform))continue;
                if(collider.GetComponentInParent<CustomerServicePoint>()||collider.GetComponentInParent<DungeonTavern.Tavern25D.NpcNavigator>())continue;
                if(target is CustomerServicePoint guest&&guest.AssignedSeat?.Table is {} table&&collider.transform.IsChildOf(table.transform))continue;
                return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = interactionOrigin == null ? transform : interactionOrigin;
            Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.75f);
            Gizmos.DrawWireSphere(origin.position, interactionRadius);
        }
    }
}

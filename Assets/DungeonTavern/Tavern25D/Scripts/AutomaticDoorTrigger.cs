using System.Collections;
using System.Collections.Generic;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEngine;

namespace DungeonTavern.Tavern25D
{
    [RequireComponent(typeof(Collider))]
    public sealed class AutomaticDoorTrigger : MonoBehaviour
    {
        [SerializeField] private DoorStateController door;
        [SerializeField, Min(0f)] private float closeDelay = 0.45f;

        private readonly HashSet<PrototypePlayerMover> occupants = new();
        private Coroutine delayedClose;

        public void Configure(DoorStateController targetDoor, float delay)
        {
            door = targetDoor;
            closeDelay = Mathf.Max(0f, delay);
        }

        private void OnTriggerEnter(Collider other)
        {
            PrototypePlayerMover player = other.GetComponentInParent<PrototypePlayerMover>();
            if (player == null || !occupants.Add(player))
                return;

            if (delayedClose != null)
            {
                StopCoroutine(delayedClose);
                delayedClose = null;
            }
            door?.Open();
        }

        private void OnTriggerExit(Collider other)
        {
            PrototypePlayerMover player = other.GetComponentInParent<PrototypePlayerMover>();
            if (player == null || !occupants.Remove(player) || occupants.Count > 0)
                return;

            if (delayedClose != null)
                StopCoroutine(delayedClose);
            delayedClose = StartCoroutine(CloseAfterDelay());
        }

        private IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSeconds(closeDelay);
            if (occupants.Count == 0)
                door?.Close();
            delayedClose = null;
        }
    }
}

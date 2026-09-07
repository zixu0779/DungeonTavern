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

        public DoorStateController TargetDoor => door;

        private readonly HashSet<GameObject> occupants = new();
        private Coroutine delayedClose;

        public void Configure(DoorStateController targetDoor, float delay)
        {
            door = targetDoor;
            closeDelay = Mathf.Max(0f, delay);
        }

        private Collider trigger;
        private void Awake() => trigger = GetComponent<Collider>();

        private void FixedUpdate()
        {
            // Disabling or teleporting a CharacterController may omit OnTriggerExit.
            if (occupants.Count > 0 && occupants.RemoveWhere(HasLeftTrigger) > 0
                && occupants.Count == 0 && delayedClose == null)
                delayedClose = StartCoroutine(CloseAfterDelay());
        }

        private bool HasLeftTrigger(GameObject agent)
        {
            if (agent == null || !agent.activeInHierarchy) return true;
            var body = agent.GetComponent<Collider>() ?? agent.GetComponentInChildren<Collider>();
            return body == null || !body.enabled || !trigger.bounds.Intersects(body.bounds);
        }

        private void OnDisable()
        {
            if (delayedClose != null) StopCoroutine(delayedClose);
            delayedClose = null;
            occupants.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            GameObject agent = ResolveAgent(other);
            if (agent == null || !occupants.Add(agent))
                return;

            if (delayedClose != null)
            {
                StopCoroutine(delayedClose);
                delayedClose = null;
            }
            door?.Open();
        }

        private void OnTriggerStay(Collider other) => OnTriggerEnter(other);

        private void OnTriggerExit(Collider other)
        {
            GameObject agent = ResolveAgent(other);
            if (agent == null || !occupants.Remove(agent) || occupants.Count > 0)
                return;

            if (delayedClose != null)
                StopCoroutine(delayedClose);
            delayedClose = StartCoroutine(CloseAfterDelay());
        }

        private static GameObject ResolveAgent(Collider other)
        {
            PrototypePlayerMover player = other.GetComponentInParent<PrototypePlayerMover>();
            if (player != null)
                return player.gameObject;

            DoorPassageAgent npc = other.GetComponentInParent<DoorPassageAgent>();
            return npc == null ? null : npc.gameObject;
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

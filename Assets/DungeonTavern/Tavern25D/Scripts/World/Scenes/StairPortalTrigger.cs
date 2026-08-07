using DungeonTavern.Prototypes.Rotation25D;
using UnityEngine;

namespace DungeonTavern.Tavern25D
{
    [RequireComponent(typeof(Collider))]
    public sealed class StairPortalTrigger : MonoBehaviour
    {
        [SerializeField] private Transform destination;
        [SerializeField, Min(0f)] private float reentryLockSeconds = 0.4f;

        private static float nextAllowedTime;

        public void Configure(Transform target, float lockSeconds = 0.4f)
        {
            destination = target;
            reentryLockSeconds = Mathf.Max(0f, lockSeconds);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (destination == null || Time.time < nextAllowedTime)
                return;

            PrototypePlayerMover player = other.GetComponentInParent<PrototypePlayerMover>();
            if (player == null)
                return;

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            player.transform.SetPositionAndRotation(destination.position, destination.rotation);
            if (controller != null)
                controller.enabled = true;

            nextAllowedTime = Time.time + reentryLockSeconds;
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(CharacterController), typeof(PrototypePlayerMover))]
    public sealed class CounterVaultController : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float detectionDistance = 1.25f;
        [SerializeField, Min(1f)] private float vaultDistance = 2.35f;
        [SerializeField, Min(0.1f)] private float vaultHeight = 0.9f;
        [SerializeField, Min(0.1f)] private float duration = 0.42f;
        [SerializeField, Min(0.05f)] private float inputBuffer = 0.18f;
        [SerializeField, Range(0.1f, 0.6f)] private float detectionRadius = 0.34f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        private CharacterController controller;
        private PrototypePlayerMover mover;
        private bool vaulting;
        private Vector3 recentMoveDirection;
        private float recentMoveUntil;
        private float vaultInputUntil;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            mover = GetComponent<PrototypePlayerMover>();
        }

        private void Update()
        {
            if (mover.MovementDirection.sqrMagnitude > 0.0001f)
            {
                recentMoveDirection = mover.MovementDirection;
                recentMoveUntil = Time.time + inputBuffer;
            }
            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
                vaultInputUntil = Time.time + inputBuffer;
            if (!vaulting && Time.time <= vaultInputUntil && Time.time <= recentMoveUntil
                && TryBeginVault(recentMoveDirection))
                vaultInputUntil = 0f;
        }

        public bool TryBeginVault(Vector3 requestedDirection)
        {
            Vector3 forward = Vector3.ProjectOnPlane(requestedDirection, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.5f)
                return false;
            Vector3 origin = transform.position + Vector3.up * 0.65f;
            if (!Physics.SphereCast(origin, detectionRadius, forward, out RaycastHit hit, detectionDistance,
                    obstacleMask, QueryTriggerInteraction.Ignore)
                || hit.collider.GetComponentInParent<CounterVaultObstacle>() == null)
                return false;

            float travelDistance = Mathf.Max(vaultDistance, hit.distance + 1.55f);
            Vector3 landing = transform.position + forward * travelDistance;
            if (!Physics.Raycast(landing + Vector3.up * 1.5f, Vector3.down, out RaycastHit floorHit, 3f, obstacleMask, QueryTriggerInteraction.Ignore))
                return false;
            landing.y = floorHit.point.y;
            StartCoroutine(VaultRoutine(landing));
            return true;
        }

        private IEnumerator VaultRoutine(Vector3 landing)
        {
            vaulting = true;
            mover.MovementInputEnabled = false;
            controller.enabled = false;
            Vector3 start = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, landing, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * vaultHeight);
                yield return null;
            }
            transform.position = landing;
            controller.enabled = true;
            mover.MovementInputEnabled = true;
            vaulting = false;
        }
    }
}

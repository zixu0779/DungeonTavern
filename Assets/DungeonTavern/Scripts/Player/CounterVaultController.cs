using System.Collections;
using UnityEngine;
using DungeonTavern.Tavern25D;
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
        [SerializeField, Min(0.1f)] private float duration = 0.9f;
        [SerializeField, Min(0.05f)] private float inputBuffer = 0.18f;
        [SerializeField, Range(0.1f, 0.6f)] private float detectionRadius = 0.34f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        private CharacterController controller;
        private PrototypePlayerMover mover;
        private bool vaulting;
        private CharacterModelMotion modelMotion;
        public bool IsVaulting => vaulting;
        private Vector3 recentMoveDirection;
        private float recentMoveUntil;
        private float vaultInputUntil;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            mover = GetComponent<PrototypePlayerMover>();
            modelMotion = GetComponentInChildren<CharacterModelMotion>();
        }

        private void Update()
        {
            if (DungeonTavern.Gameplay.Interaction.GamePauseMenu.IsPaused || DungeonTavern.UI.TavernUI.WindowOpen) return;
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
            if (vaulting || !mover.MovementInputEnabled || (modelMotion && modelMotion.IsFullBodyAction)) return false;
            Vector3 forward = Vector3.ProjectOnPlane(requestedDirection, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.5f)
                return false;
            Vector3 origin = transform.position + Vector3.up * 0.65f;
            if (!Physics.SphereCast(origin, detectionRadius, forward, out RaycastHit hit, detectionDistance,
                    obstacleMask, QueryTriggerInteraction.Ignore)
                || hit.collider.GetComponentInParent<CounterVaultObstacle>() == null)
                return false;

            var bounds = hit.collider.bounds;
            // An L-shaped mesh bounds the whole bar, not the local countertop thickness.
            float travelDistance = Mathf.Max(vaultDistance, hit.distance + 1.55f);
            float limit = travelDistance + 1.75f;
            for (; travelDistance <= limit; travelDistance += .2f)
            {
                bool inCounter = false;
                foreach (var c in Physics.OverlapSphere(origin + forward * travelDistance, controller.radius + .06f, obstacleMask, QueryTriggerInteraction.Ignore))
                    if (c.GetComponentInParent<CounterVaultObstacle>()) { inCounter = true; break; }
                if (!inCounter) break;
            }
            if (travelDistance > limit) return false;
            Vector3 landing = transform.position + forward * travelDistance;
            if (!Physics.Raycast(landing + Vector3.up * 1.5f, Vector3.down, out RaycastHit floorHit, 3f, obstacleMask, QueryTriggerInteraction.Ignore))
                return false;
            landing.y = floorHit.point.y;
            Vector3 bottom = landing + Vector3.up * (controller.radius + .08f);
            Vector3 top = landing + Vector3.up * Mathf.Max(controller.radius + .08f, controller.height - controller.radius);
            foreach (var c in Physics.OverlapCapsule(bottom, top, controller.radius * .9f, obstacleMask, QueryTriggerInteraction.Ignore))
                if (!c.transform.IsChildOf(transform)) return false;
            Vector3 handPoint = hit.point; handPoint.y = bounds.max.y + .02f;
            StartCoroutine(VaultRoutine(landing, handPoint));
            return true;
        }

        private void OnDisable()
        {
            if (!vaulting) return;
            StopAllCoroutines();
            vaulting = false;
            controller.enabled = true;
            mover.MovementInputEnabled = true;
            modelMotion?.EndFullBodyAction();
        }

        private IEnumerator VaultRoutine(Vector3 landing, Vector3 handPoint)
        {
            vaulting = true;
            mover.MovementInputEnabled = false;
            controller.enabled = false;
            Vector3 start = transform.position;
            transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(landing-start, Vector3.up));
            modelMotion?.BeginVaultPose(handPoint, duration);
            float arcHeight = Mathf.Max(vaultHeight, handPoint.y - start.y + .12f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, landing, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);
                yield return null;
            }
            transform.position = landing;
            controller.enabled = true;
            mover.MovementInputEnabled = true;
            vaulting = false;
            modelMotion?.EndFullBodyAction();
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [DefaultExecutionOrder(-50)]
    public sealed class PrototypeCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField, Min(0.05f)] private float rotationDuration = 0.22f;
        [SerializeField, Range(0f, 90f)] private float cardinalYawOffset = 45f;

        private float startYaw;
        private float targetYaw;
        private float rotationElapsed;
        private bool rotating;
        private bool dialogueFraming;
        private Transform dialogueLeft;
        private Transform dialogueRight;
        private float explorationSize;
        private Camera gameCamera;
        private Vector3 preDialoguePosition;
        private Quaternion preDialogueRotation;
        private float preDialogueSize;
        private float preDialogueTargetYaw;
        private float dialogueTargetYaw;
        private DialogueOcclusionFader occlusionFader;
        private float nextOcclusionCheck;

        public Transform FollowTarget
        {
            get => followTarget;
            set => followTarget = value;
        }

        public float CurrentCardinalYaw => Mathf.Repeat(targetYaw, 360f);

        private void Awake()
        {
            targetYaw = SnapCardinalYaw(transform.eulerAngles.y);
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            gameCamera = GetComponentInChildren<Camera>();
            occlusionFader = GetComponent<DialogueOcclusionFader>();
            if (occlusionFader == null)
                occlusionFader = gameObject.AddComponent<DialogueOcclusionFader>();
            if (gameCamera != null)
                explorationSize = gameCamera.orthographicSize;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (dialogueFraming)
            {
                UpdateDialogueFraming();
                return;
            }
            if (keyboard != null)
            {
                if (keyboard.qKey.wasPressedThisFrame)
                    RotateLeft();
                else if (keyboard.eKey.wasPressedThisFrame)
                    RotateRight();
            }

            UpdateRotation();
        }

        private void LateUpdate()
        {
            if (dialogueFraming)
                return;
            if (followTarget == null)
            {
                PrototypePlayerMover player = FindAnyObjectByType<PrototypePlayerMover>();
                if (player != null)
                    followTarget = player.transform;
            }
            if (followTarget == null)
                return;

            Vector3 desired = followTarget.position;
            desired.y = 0f;
            transform.position = desired;
        }

        public void RotateLeft()
        {
            BeginRotation(targetYaw - 90f);
        }

        public void RotateRight()
        {
            BeginRotation(targetYaw + 90f);
        }

        public void BeginDialogueFraming(Transform leftCharacter, Transform rightCharacter)
        {
            if (leftCharacter == null || rightCharacter == null)
                return;
            if (!dialogueFraming)
            {
                preDialoguePosition = transform.position;
                preDialogueRotation = transform.rotation;
                preDialogueTargetYaw = targetYaw;
                preDialogueSize = gameCamera != null ? gameCamera.orthographicSize : explorationSize;
            }
            dialogueLeft = leftCharacter;
            dialogueRight = rightCharacter;
            dialogueTargetYaw = ChooseDialogueYaw(leftCharacter, rightCharacter);
            nextOcclusionCheck = 0f;
            dialogueFraming = true;
            rotating = false;
        }

        public void EndDialogueFraming()
        {
            if (!dialogueFraming)
                return;

            dialogueFraming = false;
            dialogueLeft = null;
            dialogueRight = null;
            if (gameCamera != null)
                gameCamera.orthographicSize = preDialogueSize;
            transform.SetPositionAndRotation(preDialoguePosition, preDialogueRotation);
            targetYaw = preDialogueTargetYaw;
            rotating = false;
            occlusionFader?.RestoreAll();
        }

        private void UpdateDialogueFraming()
        {
            if (dialogueLeft == null || dialogueRight == null)
            {
                EndDialogueFraming();
                return;
            }

            Vector3 midpoint = (dialogueLeft.position + dialogueRight.position) * 0.5f;
            midpoint.y = 0f;
            transform.position = Vector3.Lerp(transform.position, midpoint, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.Euler(0f, dialogueTargetYaw, 0f),
                1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));

            if (gameCamera != null)
                gameCamera.orthographicSize = Mathf.Lerp(gameCamera.orthographicSize, 3.25f, 1f - Mathf.Exp(-7f * Time.unscaledDeltaTime));

            if (Time.unscaledTime >= nextOcclusionCheck)
            {
                nextOcclusionCheck = Time.unscaledTime + 0.12f;
                UpdateDialogueOccluders();
            }
        }

        private float ChooseDialogueYaw(Transform leftCharacter, Transform rightCharacter)
        {
            Vector3 leftToRight = rightCharacter.position - leftCharacter.position;
            leftToRight.y = 0f;
            float preferred = leftToRight.sqrMagnitude > 0.01f
                ? Mathf.Atan2(leftToRight.x, leftToRight.z) * Mathf.Rad2Deg - 90f
                : transform.eulerAngles.y;
            float[] offsets = { 0f, -45f, 45f, -90f, 90f, 180f };
            float bestYaw = preferred;
            float bestScore = float.PositiveInfinity;
            for (int index = 0; index < offsets.Length; index++)
            {
                float candidate = preferred + offsets[index];
                int blockers = CountCandidateBlockers(candidate, leftCharacter, rightCharacter);
                float score = blockers * 1000f + Mathf.Abs(offsets[index]);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestYaw = candidate;
                }
            }
            return bestYaw;
        }

        private int CountCandidateBlockers(float yaw, Transform leftCharacter, Transform rightCharacter)
        {
            if (gameCamera == null)
                return 0;
            Vector3 midpoint = (leftCharacter.position + rightCharacter.position) * 0.5f;
            midpoint.y = 0f;
            Vector3 cameraLocal = transform.InverseTransformPoint(gameCamera.transform.position);
            Vector3 cameraPosition = Matrix4x4.TRS(midpoint, Quaternion.Euler(0f, yaw, 0f), transform.lossyScale)
                .MultiplyPoint3x4(cameraLocal);
            return CountBlockers(cameraPosition, GetLookPoint(leftCharacter), leftCharacter, rightCharacter)
                + CountBlockers(cameraPosition, GetLookPoint(rightCharacter), leftCharacter, rightCharacter);
        }

        private static int CountBlockers(Vector3 origin, Vector3 target, Transform leftCharacter, Transform rightCharacter)
        {
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
                return 0;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction / distance, distance, ~0, QueryTriggerInteraction.Ignore);
            int blockers = 0;
            for (int index = 0; index < hits.Length; index++)
            {
                Transform hit = hits[index].collider.transform;
                if (!hit.IsChildOf(leftCharacter) && !hit.IsChildOf(rightCharacter))
                    blockers++;
            }
            return blockers;
        }

        private void UpdateDialogueOccluders()
        {
            if (gameCamera == null || dialogueLeft == null || dialogueRight == null)
                return;
            var renderers = new HashSet<Renderer>();
            CollectOccluders(gameCamera.transform.position, GetLookPoint(dialogueLeft), renderers);
            CollectOccluders(gameCamera.transform.position, GetLookPoint(dialogueRight), renderers);
            occlusionFader?.SetOccluders(renderers);
        }

        private void CollectOccluders(Vector3 origin, Vector3 target, HashSet<Renderer> results)
        {
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
                return;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction / distance, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int index = 0; index < hits.Length; index++)
            {
                Transform hit = hits[index].collider.transform;
                if (hit.IsChildOf(dialogueLeft) || hit.IsChildOf(dialogueRight) || hit.IsChildOf(transform))
                    continue;
                Renderer renderer = hit.GetComponentInParent<Renderer>() ?? hit.GetComponentInChildren<Renderer>();
                if (renderer != null)
                    results.Add(renderer);
            }
        }

        private static Vector3 GetLookPoint(Transform character)
        {
            Renderer renderer = character.GetComponentInChildren<Renderer>();
            if (renderer != null)
                return new Vector3(renderer.bounds.center.x, renderer.bounds.max.y * 0.75f + renderer.bounds.center.y * 0.25f, renderer.bounds.center.z);
            return character.position + Vector3.up * 1.2f;
        }

        private void BeginRotation(float newTargetYaw)
        {
            startYaw = transform.eulerAngles.y;
            targetYaw = SnapCardinalYaw(newTargetYaw);
            rotationElapsed = 0f;
            rotating = true;
        }

        private float SnapCardinalYaw(float yaw)
        {
            return Mathf.Round((yaw - cardinalYawOffset) / 90f) * 90f +
                   cardinalYawOffset;
        }

        private void UpdateRotation()
        {
            if (!rotating)
                return;

            rotationElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(rotationElapsed / rotationDuration);
            float eased = t * t * (3f - 2f * t);
            float yaw = Mathf.LerpAngle(startYaw, targetYaw, eased);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (t < 1f)
                return;

            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            rotating = false;
        }
    }
}

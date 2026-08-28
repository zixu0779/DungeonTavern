using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [DefaultExecutionOrder(-50)]
    public sealed class PrototypeCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField, Min(1f)] private float keyboardRotationSpeed = 90f;
        [SerializeField, Min(0.01f)] private float mouseRotationSensitivity = 0.18f;

        private float targetYaw;
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
            targetYaw = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            ApplyIsometricProjection();
            gameCamera = GetComponentInChildren<Camera>();
            occlusionFader = GetComponent<DialogueOcclusionFader>();
            if (occlusionFader == null)
                occlusionFader = gameObject.AddComponent<DialogueOcclusionFader>();
            if (gameCamera != null)
                explorationSize = gameCamera.orthographicSize;
        }

        public void ApplyIsometricProjection()
        {
            var camera = GetComponentInChildren<Camera>(true);
            if (camera == null || camera.transform.parent != transform)
                throw new System.InvalidOperationException("Isometric camera must be a direct child of the orbit rig.");
            var position = camera.transform.localPosition;
            var forward = camera.transform.localRotation * Vector3.forward;
            var target = Mathf.Abs(forward.y) > .0001f
                ? position + forward * (-position.y / forward.y) : Vector3.zero;
            float distance = Vector3.Distance(position, target);
            float pitch = Mathf.Atan(1f / Mathf.Sqrt(2f)) * Mathf.Rad2Deg;
            camera.orthographic = true;
            camera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            camera.transform.localPosition = target - camera.transform.localRotation * Vector3.forward * distance;
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (dialogueFraming)
            {
                UpdateDialogueFraming();
                return;
            }
            if (Time.timeScale <= 0f) return;
            bool left = keyboard != null && keyboard.qKey.isPressed;
            bool right = keyboard != null && keyboard.eKey.isPressed;
            if (left || right)
                RotateBy(((right ? 1f : 0f) - (left ? 1f : 0f)) * keyboardRotationSpeed * Time.deltaTime);
            else if (Mouse.current != null && Mouse.current.rightButton.isPressed &&
                (UnityEngine.EventSystems.EventSystem.current == null ||
                 !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
                RotateBy(Mouse.current.delta.ReadValue().x * mouseRotationSensitivity);
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
            RotateBy(-keyboardRotationSpeed * Time.deltaTime);
        }

        public void RotateRight()
        {
            RotateBy(keyboardRotationSpeed * Time.deltaTime);
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
            float[] headings = { 45f, 135f, 225f, 315f };
            float bestYaw = preferred;
            float bestScore = float.PositiveInfinity;
            for (int index = 0; index < headings.Length; index++)
            {
                float candidate = headings[index];
                int blockers = CountCandidateBlockers(candidate, leftCharacter, rightCharacter);
                float score = blockers * 1000f + Mathf.Abs(Mathf.DeltaAngle(preferred, candidate));
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

        public void RotateBy(float deltaDegrees)
        {
            if (dialogueFraming) return;
            targetYaw = Mathf.Repeat(targetYaw + deltaDegrees, 360f);
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        }
    }
}

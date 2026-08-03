using UnityEngine;
using UnityEngine.InputSystem;

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
            dialogueFraming = true;
            rotating = false;
        }

        public void EndDialogueFraming()
        {
            dialogueFraming = false;
            dialogueLeft = null;
            dialogueRight = null;
            if (gameCamera != null)
                gameCamera.orthographicSize = preDialogueSize;
            transform.SetPositionAndRotation(preDialoguePosition, preDialogueRotation);
            targetYaw = preDialogueTargetYaw;
            rotating = false;
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

            Vector3 leftToRight = dialogueRight.position - dialogueLeft.position;
            leftToRight.y = 0f;
            if (leftToRight.sqrMagnitude > 0.01f)
            {
                float rightHeading = Mathf.Atan2(leftToRight.x, leftToRight.z) * Mathf.Rad2Deg;
                float desiredYaw = rightHeading - 90f;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.Euler(0f, desiredYaw, 0f),
                    1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            }

            if (gameCamera != null)
                gameCamera.orthographicSize = Mathf.Lerp(gameCamera.orthographicSize, 3.25f, 1f - Mathf.Exp(-7f * Time.unscaledDeltaTime));
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

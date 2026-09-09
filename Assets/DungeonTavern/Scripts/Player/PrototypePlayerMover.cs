using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypePlayerMover : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Min(0f)] private float moveSpeed = 3.25f;
        [SerializeField, Min(0f)] private float turnSharpness = 16f;
        [SerializeField, Min(0f)] private float gravity = 24f;
        [SerializeField, Min(0f)] private float groundedVelocity = 2f;
        [SerializeField, Min(0f)] private float maximumFallSpeed = 35f;

        private CharacterController controller;
        private float verticalVelocity;

        public bool MovementInputEnabled { get; set; } = true;
        public float MoveSpeed => moveSpeed;
        public Vector3 MovementDirection { get; private set; }

        public Transform CameraTransform
        {
            get => cameraTransform;
            set => cameraTransform = value;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle == null) obstacle = gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule;
            obstacle.center = controller.center;
            obstacle.radius = controller.radius + .05f;
            obstacle.height = controller.height;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            obstacle.carvingMoveThreshold = .1f;
            obstacle.carvingTimeToStationary = .25f;
            if (GetComponent<CounterVaultController>() == null)
                gameObject.AddComponent<CounterVaultController>();
        }

        private void Update()
        {
            if (DungeonTavern.Gameplay.Interaction.GamePauseMenu.IsPaused) return;
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            Keyboard keyboard = Keyboard.current;
            Vector2 input = Vector2.zero;
            if (MovementInputEnabled && keyboard != null)
            {
                if (keyboard.wKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed) input.y -= 1f;
                if (keyboard.dKey.isPressed) input.x += 1f;
                if (keyboard.aKey.isPressed) input.x -= 1f;
            }
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 movement = Vector3.zero;
            if (cameraTransform != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
                movement = forward * input.y + right * input.x;
            }
            MovementDirection = movement.sqrMagnitude > 0.0001f ? movement.normalized : Vector3.zero;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -groundedVelocity;
            else
                verticalVelocity = Mathf.Max(verticalVelocity - gravity * Time.deltaTime, -maximumFallSpeed);

            Vector3 velocity = movement * moveSpeed + Vector3.up * verticalVelocity;
            CollisionFlags collision = controller.Move(velocity * Time.deltaTime);
            if ((collision & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
                verticalVelocity = -groundedVelocity;

            if (movement.sqrMagnitude <= 0.0001f)
                return;

            Quaternion target = Quaternion.LookRotation(movement, Vector3.up);
            float t = 1f - Mathf.Exp(-turnSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
        }
    }
}

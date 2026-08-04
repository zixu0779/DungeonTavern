using System;
using DungeonTavern.Gameplay.Interaction;
using UnityEngine;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class Day1EveActor : InteractionPoint
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 2f;
        [SerializeField, Min(0.5f)] private float conversationRange = 1.35f;
        [SerializeField, Min(0.5f)] private float minimumSpacing = 0.65f;
        [SerializeField, Min(2f)] private float maximumPursuitDistance = 8f;

        private WorldSpeechBubble bubble;
        private NpcApproachSpeech approachSpeech;
        private Transform player;
        [SerializeField] private Transform sceneEntranceWaitPoint;
        private Vector3 entranceWaitPosition;
        private CharacterController movementController;
        private bool arriving;
        private bool conversationStarted;
        private bool playerInTavernArea = true;

        public event Action ConversationRequested;

        public void Configure(Transform destination)
        {
            sceneEntranceWaitPoint = destination;
            if (destination != null)
                entranceWaitPosition = destination.position;
        }

        private void Awake()
        {
            bubble = GetComponent<WorldSpeechBubble>();
            approachSpeech = GetComponent<NpcApproachSpeech>();
            if (approachSpeech == null)
                approachSpeech = gameObject.AddComponent<NpcApproachSpeech>();
            if (sceneEntranceWaitPoint == null)
                sceneEntranceWaitPoint = GameObject.Find("EveDay1Conversation")?.transform;
            entranceWaitPosition = sceneEntranceWaitPoint != null
                ? sceneEntranceWaitPoint.position
                : transform.position;
            movementController = GetComponent<CharacterController>();
            if (movementController == null)
                movementController = gameObject.AddComponent<CharacterController>();
            movementController.radius = 0.28f;
            movementController.height = 1.5f;
            movementController.center = new Vector3(0f, 0.75f, 0f);
            movementController.stepOffset = 0.25f;
            if (GetComponent<DoorPassageAgent>() == null)
                gameObject.AddComponent<DoorPassageAgent>();

            foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(true))
            {
                sprite.sortingOrder = Mathf.Max(sprite.sortingOrder, 100);
                sprite.rendererPriority = Mathf.Max(sprite.rendererPriority, 100);
                Vector3 local = sprite.transform.localPosition;
                sprite.transform.localPosition = new Vector3(local.x, Mathf.Max(local.y, 0.01f), local.z);
            }
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            PlayerAreaTransition.Started += OnPlayerAreaTransitionStarted;
            PlayerAreaTransition.Completed += OnPlayerAreaTransitionCompleted;
        }

        private void OnDisable()
        {
            PlayerAreaTransition.Started -= OnPlayerAreaTransitionStarted;
            PlayerAreaTransition.Completed -= OnPlayerAreaTransitionCompleted;
        }

        public void BeginArrival()
        {
            gameObject.SetActive(true);
            arriving = true;
            conversationStarted = false;
            player = FindAnyObjectByType<DungeonTavern.Prototypes.Rotation25D.PrototypePlayerMover>()?.transform;
            approachSpeech.BeginApproach("你终于回来了。");
        }

        private void Update()
        {
            if (!arriving)
                return;

            player ??= FindAnyObjectByType<DungeonTavern.Prototypes.Rotation25D.PrototypePlayerMover>()?.transform;
            if (player == null)
                return;

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (!playerInTavernArea)
            {
                MoveTowards(sceneEntranceWaitPoint != null
                    ? sceneEntranceWaitPoint.position
                    : entranceWaitPosition);
                return;
            }

            // Conversation range takes priority over the navigation waypoint. Both
            // characters have solid controllers, so the player may physically prevent
            // Eve from reaching the exact marker even though she is already close enough.
            if (distance <= conversationRange)
            {
                arriving = false;
                StartConversation();
                return;
            }

            Vector3 destination = player.position - toPlayer.normalized * minimumSpacing;
            destination.y = transform.position.y;
            MoveTowards(destination);
            if (toPlayer.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        }

        private void OnPlayerAreaTransitionCompleted(string loadedScene, string unloadedScene)
        {
            if (!arriving)
                return;

            if (!string.IsNullOrWhiteSpace(loadedScene)
                && !string.Equals(loadedScene, "Tavern_Main", StringComparison.Ordinal))
            {
                playerInTavernArea = false;
            }
            else if (!string.IsNullOrWhiteSpace(unloadedScene)
                     || string.Equals(loadedScene, "Tavern_Main", StringComparison.Ordinal))
            {
                playerInTavernArea = true;
            }
        }

        private void OnPlayerAreaTransitionStarted(string loadedScene, string unloadedScene)
        {
            if (!arriving)
                return;
            if (!string.IsNullOrWhiteSpace(loadedScene)
                && !string.Equals(loadedScene, "Tavern_Main", StringComparison.Ordinal))
            {
                playerInTavernArea = false;
            }
        }

        private void MoveTowards(Vector3 destination)
        {
            destination.y = transform.position.y;
            Vector3 next = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            Vector3 delta = next - transform.position;
            if (movementController != null && movementController.enabled)
                movementController.Move(delta);
            else
                transform.position = next;
        }

        public void ShowBubble(string text)
        {
            bubble?.Show(text);
        }

        public override string GetPrompt(PlayerHands hands)
        {
            return string.Empty;
        }

        public override bool Interact(PlayerHands hands)
        {
            return false;
        }

        private void StartConversation()
        {
            if (conversationStarted)
                return;
            conversationStarted = true;
            approachSpeech.BeginDialogue();
            ConversationRequested?.Invoke();
        }
    }
}

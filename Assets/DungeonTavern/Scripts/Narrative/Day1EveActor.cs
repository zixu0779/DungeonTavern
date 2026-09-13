using System;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEngine;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class Day1EveActor : InteractionPoint
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.25f;
        [SerializeField, Min(0.5f)] private float conversationRange = 1.35f;
        [SerializeField, Min(0.5f)] private float minimumSpacing = 0.65f;
        [SerializeField, Min(0.05f)] private float conversationClearanceRadius = 0.2f;
        [SerializeField, Min(0.1f)] private float sightLowerHeight = 0.35f;
        [SerializeField, Min(0.2f)] private float sightUpperHeight = 1.15f;
        [SerializeField] private LayerMask conversationBlockers = ~0;

        private WorldSpeechBubble bubble;
        private NpcApproachSpeech approachSpeech;
        private Transform player;
        [SerializeField] private Transform sceneEntranceWaitPoint;
        private Vector3 entranceWaitPosition;
        private CharacterController movementController;
        private NpcNavigator navigator;
        private bool arriving;
        private bool conversationStarted;
        private bool playerInTavernArea = true;
        private bool guidingToOpeningSwitch;
        private bool openingGuidanceReady;
        private Transform openingGuidePoint;
        private string openingGuidanceLine;
        private bool turningToOpeningSwitch;

        public bool IsOpeningGuidanceReady => openingGuidanceReady;

        public event Action ConversationRequested;

        public void Configure(Transform destination)
        {
            sceneEntranceWaitPoint = destination;
            if (destination != null)
                entranceWaitPosition = destination.position;
        }

        private void Awake()
        {
            PrototypePlayerMover playerMover = FindAnyObjectByType<PrototypePlayerMover>();
            float playerSpeed = playerMover == null ? 3.25f : playerMover.MoveSpeed;
            moveSpeed = playerSpeed * 1.08f;
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
            navigator = GetComponent<NpcNavigator>();
            if (navigator == null)
                navigator = gameObject.AddComponent<NpcNavigator>();
            navigator.Configure(moveSpeed, minimumSpacing);
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

        protected override void OnEnable()
        {
            base.OnEnable();
            PlayerAreaTransition.Started += OnPlayerAreaTransitionStarted;
            PlayerAreaTransition.Completed += OnPlayerAreaTransitionCompleted;
        }

        protected override void OnDisable()
        {
            PlayerAreaTransition.Started -= OnPlayerAreaTransitionStarted;
            PlayerAreaTransition.Completed -= OnPlayerAreaTransitionCompleted;
            base.OnDisable();
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
            if (guidingToOpeningSwitch)
            {
                UpdateOpeningSwitchGuidance();
                return;
            }
            if (!arriving)
                return;

            player ??= FindAnyObjectByType<DungeonTavern.Prototypes.Rotation25D.PrototypePlayerMover>()?.transform;
            if (player == null)
                return;

            if (!playerInTavernArea)
            {
                navigator.MoveTo(sceneEntranceWaitPoint != null
                    ? sceneEntranceWaitPoint.position
                    : entranceWaitPosition, minimumSpacing);
                return;
            }

            navigator.MoveTo(player.position, minimumSpacing);
            if (CanStartConversation())
            {
                arriving = false;
                navigator.Stop(true);
                StartConversation();
            }
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

        private bool CanStartConversation()
        {
            return playerInTavernArea
                && navigator.HasCompletePath
                && navigator.RemainingDistance <= conversationRange
                && HasClearConversationLineOfSight();
        }

        private bool HasClearConversationLineOfSight()
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
                return true;

            Vector3 lower = transform.position + Vector3.up * sightLowerHeight;
            Vector3 upper = transform.position + Vector3.up * sightUpperHeight;
            RaycastHit[] hits = Physics.CapsuleCastAll(
                lower,
                upper,
                conversationClearanceRadius,
                direction / distance,
                distance,
                conversationBlockers,
                QueryTriggerInteraction.Ignore);
            for (int index = 0; index < hits.Length; index++)
            {
                Transform hit = hits[index].collider.transform;
                if (hit == transform || hit.IsChildOf(transform)
                    || hit == player || hit.IsChildOf(player))
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public void ShowBubble(string text)
        {
            bubble?.Show(text);
        }

        public void BeginOpeningSwitchGuidance(Transform guidePoint, string text)
        {
            turningToOpeningSwitch = false;
            openingGuidePoint = guidePoint;
            openingGuidanceLine = text;
            openingGuidanceReady = false;
            guidingToOpeningSwitch = guidePoint != null;
            bubble?.Hide();
            if (guidePoint == null)
            {
                openingGuidanceReady = true;
                bubble?.Show(text);
            }
        }

        public void CompleteOpeningSwitchGuidance()
        {
            guidingToOpeningSwitch = false;
            openingGuidanceReady = false;
            openingGuidePoint = null;
            openingGuidanceLine = string.Empty;
            bubble?.Hide();
            navigator.Stop(true);
        }

        private void UpdateOpeningSwitchGuidance()
        {
            if (openingGuidePoint == null)
                return;
            if (!turningToOpeningSwitch)
            {
                navigator.MoveTo(openingGuidePoint.position, 0.15f);
                if (!navigator.HasArrived(0.2f)) return;
                navigator.Stop(true);
                turningToOpeningSwitch = true;
            }
            var lever = FindAnyObjectByType<FloorLeverPoint>();
            var direction = lever != null ? lever.transform.position - transform.position : openingGuidePoint.forward;
            direction.y = 0;
            if (direction.sqrMagnitude > .001f)
            {
                var target = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 180f * Time.deltaTime);
                if (Quaternion.Angle(transform.rotation, target) > 1f) return;
            }
            guidingToOpeningSwitch = false;
            openingGuidanceReady = true;
            navigator.Stop(true);
            bubble?.Show(openingGuidanceLine);
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

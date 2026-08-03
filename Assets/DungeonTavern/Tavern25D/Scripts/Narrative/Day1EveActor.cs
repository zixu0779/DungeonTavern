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

        private WorldSpeechBubble bubble;
        private Transform player;
        private bool arriving;
        private bool conversationStarted;

        public event Action ConversationRequested;

        public void Configure(Transform destination)
        {
            // Kept for scene-setup compatibility. Eve now follows the player.
        }

        private void Awake()
        {
            bubble = GetComponent<WorldSpeechBubble>();
            gameObject.SetActive(false);
        }

        public void BeginArrival()
        {
            gameObject.SetActive(true);
            arriving = true;
            conversationStarted = false;
            player = FindAnyObjectByType<DungeonTavern.Prototypes.Rotation25D.PrototypePlayerMover>()?.transform;
            bubble?.Show("我听见储藏室有动静……老板？");
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
            if (distance > conversationRange)
            {
                Vector3 destination = player.position - toPlayer.normalized * minimumSpacing;
                destination.y = player.position.y;
                transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
                if (toPlayer.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                return;
            }

            arriving = false;
            transform.position = new Vector3(transform.position.x, player.position.y, transform.position.z);
            StartConversation();
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
            bubble?.Hide();
            ConversationRequested?.Invoke();
        }
    }
}

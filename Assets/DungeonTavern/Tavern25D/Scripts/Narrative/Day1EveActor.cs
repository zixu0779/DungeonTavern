using System;
using DungeonTavern.Gameplay.Interaction;
using UnityEngine;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class Day1EveActor : InteractionPoint
    {
        [SerializeField] private Transform storageConversationPoint;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2f;

        private WorldSpeechBubble bubble;
        private bool arriving;
        private bool ready;

        public event Action ConversationRequested;

        public void Configure(Transform destination)
        {
            storageConversationPoint = destination;
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
            ready = false;
        }

        private void Update()
        {
            if (!arriving || storageConversationPoint == null)
                return;

            transform.position = Vector3.MoveTowards(transform.position, storageConversationPoint.position, moveSpeed * Time.deltaTime);
            if ((transform.position - storageConversationPoint.position).sqrMagnitude > 0.02f)
                return;

            arriving = false;
            ready = true;
            bubble?.Show("我听见储藏室有动静……老板？");
        }

        public void ShowBubble(string text)
        {
            bubble?.Show(text);
        }

        public override string GetPrompt(PlayerHands hands)
        {
            return ready ? "F: 和伊芙说话" : string.Empty;
        }

        public override bool Interact(PlayerHands hands)
        {
            if (!ready)
                return false;
            ready = false;
            bubble?.Hide();
            ConversationRequested?.Invoke();
            return true;
        }
    }
}

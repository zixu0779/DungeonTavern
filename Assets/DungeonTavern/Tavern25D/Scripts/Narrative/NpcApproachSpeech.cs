using UnityEngine;

namespace DungeonTavern.Tavern25D.Narrative
{
    [RequireComponent(typeof(WorldSpeechBubble))]
    public sealed class NpcApproachSpeech : MonoBehaviour
    {
        [SerializeField, TextArea] private string firstDialogueLine;

        private WorldSpeechBubble bubble;

        private void Awake()
        {
            bubble = GetComponent<WorldSpeechBubble>();
        }

        public void BeginApproach(string upcomingLine = null)
        {
            if (!string.IsNullOrWhiteSpace(upcomingLine))
                firstDialogueLine = upcomingLine;
            if (!string.IsNullOrWhiteSpace(firstDialogueLine))
                bubble.Show(firstDialogueLine);
        }

        public void BeginDialogue()
        {
            bubble.Hide();
        }
    }
}

using UnityEngine;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class WorldSpeechBubble : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new(0f, 2.2f, 0f);
        [SerializeField, Min(1f)] private float duration = 5f;

        private string line;
        private float hideAt;
        private GUIStyle style;

        public bool IsVisible => !string.IsNullOrEmpty(line) && Time.unscaledTime < hideAt;

        public void Show(string text, float visibleSeconds = -1f)
        {
            line = text;
            hideAt = Time.unscaledTime + (visibleSeconds > 0f ? visibleSeconds : duration);
        }

        public void Hide()
        {
            line = string.Empty;
        }

        private void OnGUI()
        {
            if (!IsVisible || Camera.main == null)
                return;

            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + worldOffset);
            if (screen.z <= 0f)
                return;

            style ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                wordWrap = true,
                padding = new RectOffset(14, 14, 9, 9),
                normal = { textColor = new Color(0.19f, 0.13f, 0.1f) }
            };

            float width = Mathf.Min(360f, Mathf.Max(150f, style.CalcHeight(new GUIContent(line), 320f) * 4.2f));
            float height = style.CalcHeight(new GUIContent(line), width);
            Rect rect = new(screen.x - width * 0.5f, Screen.height - screen.y - height - 18f, width, height);
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.93f, 0.86f, 0.7f, 0.97f);
            GUI.Box(rect, line, style);
            GUI.backgroundColor = old;
        }
    }
}

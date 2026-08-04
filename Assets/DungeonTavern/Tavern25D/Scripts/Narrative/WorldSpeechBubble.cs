using UnityEngine;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class WorldSpeechBubble : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new(0f, 2.2f, 0f);
        [SerializeField, Min(0f)] private float headClearance = 0.18f;
        private string line;
        private GUIStyle style;
        private Renderer[] characterRenderers;

        private void Awake()
        {
            characterRenderers = GetComponentsInChildren<Renderer>(true);
        }

        public bool IsVisible => !string.IsNullOrEmpty(line);

        public void Show(string text, float visibleSeconds = -1f)
        {
            line = text;
        }

        public void Hide()
        {
            line = string.Empty;
        }

        private void OnGUI()
        {
            if (!IsVisible || Camera.main == null)
                return;

            // PrototypePixelOutput draws the low-resolution game texture from OnGUI.
            // Draw bubbles above that full-screen output, but below fades/cinematics.
            GUI.depth = -4000;

            Vector3 anchor = GetHeadAnchor();
            Vector3 viewportPoint = Camera.main.WorldToViewportPoint(anchor);
            if (viewportPoint.z <= 0f)
                return;

            Rect outputRect = GetCameraOutputRect(Camera.main);
            Vector2 screen = new(
                outputRect.x + viewportPoint.x * outputRect.width,
                outputRect.y + (1f - viewportPoint.y) * outputRect.height);

            style ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(28, Mathf.RoundToInt(Screen.height / 28f)),
                wordWrap = true,
                padding = new RectOffset(14, 14, 9, 9),
                normal = { textColor = new Color(0.19f, 0.13f, 0.1f) }
            };

            float width = Mathf.Min(360f, Mathf.Max(150f, style.CalcHeight(new GUIContent(line), 320f) * 4.2f));
            float height = style.CalcHeight(new GUIContent(line), width);
            Rect rect = new(screen.x - width * 0.5f, screen.y - height - 12f, width, height);
            rect.x = Mathf.Clamp(rect.x, outputRect.x, outputRect.xMax - rect.width);
            rect.y = Mathf.Clamp(rect.y, outputRect.y, outputRect.yMax - rect.height);

            Color old = GUI.color;
            GUI.color = new Color(0.93f, 0.86f, 0.7f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, line, style);
            GUI.color = old;
        }

        private Vector3 GetHeadAnchor()
        {
            float highestPoint = float.NegativeInfinity;
            for (int index = 0; index < characterRenderers.Length; index++)
            {
                Renderer renderer = characterRenderers[index];
                if (renderer != null && renderer.enabled)
                    highestPoint = Mathf.Max(highestPoint, renderer.bounds.max.y);
            }

            Vector3 anchor = transform.position;
            anchor.x += worldOffset.x;
            anchor.z += worldOffset.z;
            anchor.y = float.IsNegativeInfinity(highestPoint)
                ? transform.position.y + worldOffset.y
                : highestPoint + headClearance;
            return anchor;
        }

        private static Rect GetCameraOutputRect(Camera camera)
        {
            float targetAspect = camera.targetTexture != null
                ? camera.targetTexture.width / (float)Mathf.Max(1, camera.targetTexture.height)
                : camera.aspect;
            float screenAspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            if (screenAspect > targetAspect)
            {
                float width = Screen.height * targetAspect;
                return new Rect((Screen.width - width) * 0.5f, 0f, width, Screen.height);
            }

            float height = Screen.width / targetAspect;
            return new Rect(0f, (Screen.height - height) * 0.5f, Screen.width, height);
        }
    }
}

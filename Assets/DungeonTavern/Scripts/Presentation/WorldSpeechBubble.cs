using UnityEngine;
using System.Linq;
using DungeonTavern.Gameplay.Interaction;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class WorldSpeechBubble : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new(0f, 2.2f, 0f);
        [SerializeField, Min(0f)] private float headClearance = 0.18f;
        private string line;
        private CustomerOrder order;
        private bool eating;
        private float hideAt = -1;
        private GUIStyle style;
        private Renderer[] characterRenderers;

        private void Awake()
        {
            characterRenderers = GetComponentsInChildren<Renderer>(true);
        }

        public bool IsVisible => order != null || !string.IsNullOrEmpty(line);
        public string CurrentText => line ?? string.Empty;

        public void Show(string text, float visibleSeconds = -1f)
        {
            order = null;
            line = text;
            hideAt = visibleSeconds > 0 ? Time.time + visibleSeconds : -1;
        }

        private void Update() { if (hideAt > 0 && Time.time >= hideAt) Hide(); }

        public void Hide()
        {
            order = null;
            line = string.Empty;
            hideAt = -1;
        }

        public void ShowOrder(CustomerOrder customerOrder, bool isEating)
        {
            order = customerOrder;
            eating = isEating;
            line = string.Empty;
            hideAt = -1;
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
            if (viewportPoint.z <= 0f
                || viewportPoint.x < 0f || viewportPoint.x > 1f
                || viewportPoint.y < 0f || viewportPoint.y > 1f)
                return;

            Rect outputRect = GetCameraOutputRect(Camera.main);
            Vector2 screen = new(
                outputRect.x + viewportPoint.x * outputRect.width,
                outputRect.y + (1f - viewportPoint.y) * outputRect.height);

            style ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(18, Mathf.RoundToInt(Screen.height / 42f)),
                wordWrap = true,
                padding = new RectOffset(12, 12, 7, 7),
                normal = { textColor = new Color(0.19f, 0.13f, 0.1f) }
            };
            ApplyNonInteractiveTextColor(style, style.normal.textColor);

            if (order != null)
            {
                DrawOrder(screen);
                return;
            }

            float width = Mathf.Clamp(style.CalcSize(new GUIContent(line)).x + 24f, 120f, 300f);
            float height = style.CalcHeight(new GUIContent(line), width);
            Rect rect = new(screen.x - width * 0.5f, screen.y - height - 12f, width, height);

            Color old = GUI.color;
            GUI.color = new Color(0.93f, 0.86f, 0.7f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, line, style);
            GUI.color = old;
        }

        private void DrawOrder(Vector2 screen)
        {
            var groups = order.Portions.Where(p => !p.Consumed).GroupBy(p => p.Item).ToArray();
            if (groups.Length == 0) return;
            float scale = Mathf.Clamp(Screen.height / 720f, .8f, 1.5f);
            float width = 106 * scale, row = 48 * scale;
            Rect panel = new(screen.x - width / 2, screen.y - row * groups.Length - 12, width, row * groups.Length);
            Color old = GUI.color;
            GUI.color = new Color(.93f, .86f, .7f); GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = Color.white;
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                bool active = eating && group.Any(p => p.Delivered && !p.Consumed);
                float remaining = active ? group.Where(p => p.Delivered).Average(p => p.RemainingFraction) : 1f;
                Rect icon = new(panel.x + 12 * scale, panel.y + i * row + 7 * scale, 28 * scale, 28 * scale);
                var matrix = GUI.matrix;
                if (active) GUIUtility.RotateAroundPivot(-8f + Mathf.Sin(Time.time * 3f) * 8f, icon.center);
                DrawFoodIcon(icon, group.Key, remaining);
                GUI.matrix = matrix;
                GUI.Label(new Rect(panel.x + 45 * scale, panel.y + i * row, 60 * scale, row), "×" + group.Count(), style);
                if (active)
                {
                    Fill(new Rect(panel.x + 10 * scale, panel.y + (i + 1) * row - 7 * scale, width - 20 * scale, 3 * scale), new Color(.28f,.23f,.17f));
                    Fill(new Rect(panel.x + 10 * scale, panel.y + (i + 1) * row - 7 * scale, (width - 20 * scale) * remaining, 3 * scale), new Color(.65f,.4f,.12f));
                }
            }
            GUI.color = old;
        }

        // Pixel-shaped icons avoid platform-dependent emoji fonts; quantities remain ordinary text.
        private static void DrawFoodIcon(Rect r, HeldItem item, float remaining)
        {
            Color outline = new(.22f,.13f,.06f), liquid = new(.78f,.43f,.09f), foam = new(1f,.96f,.78f);
            if (item == HeldItem.TestDrink)
            {
                Fill(new Rect(r.x+r.width*.65f,r.y+r.height*.25f,r.width*.3f,r.height*.48f),outline);
                Fill(new Rect(r.x+r.width*.72f,r.y+r.height*.34f,r.width*.14f,r.height*.27f),new Color(.93f,.86f,.7f));
                Fill(new Rect(r.x,r.y+r.height*.12f,r.width*.7f,r.height*.83f),outline);
                Fill(new Rect(r.x+r.width*.1f,r.y+r.height*.22f,r.width*.49f,r.height*.61f),new Color(.45f,.32f,.19f));
                Fill(new Rect(r.x+r.width*.1f,r.y+r.height*(.83f-.61f*remaining),r.width*.49f,r.height*.61f*remaining),liquid);
                Fill(new Rect(r.x,r.y+r.height*.08f,r.width*.7f,r.height*.15f),foam);
            }
            else
            {
                Fill(new Rect(r.x,r.y+r.height*.65f,r.width,r.height*.2f),outline);
                Fill(new Rect(r.x+r.width*.12f,r.y+r.height*.25f,r.width*.76f,r.height*.4f),item==HeldItem.MainDish?new Color(.59f,.27f,.12f):new Color(.32f,.48f,.15f));
            }
        }
        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white;
        }

        private static void ApplyNonInteractiveTextColor(GUIStyle target, Color color)
        {
            target.hover.textColor = color;
            target.hover.background = target.normal.background;
            target.active.textColor = color;
            target.active.background = target.normal.background;
            target.focused.textColor = color;
            target.focused.background = target.normal.background;
            target.onNormal.textColor = color;
            target.onNormal.background = target.normal.background;
            target.onHover.textColor = color;
            target.onHover.background = target.normal.background;
            target.onActive.textColor = color;
            target.onActive.background = target.normal.background;
            target.onFocused.textColor = color;
            target.onFocused.background = target.normal.background;
        }

        private Vector3 GetHeadAnchor()
        {
            float highestPoint = float.NegativeInfinity;
            characterRenderers ??= GetComponentsInChildren<Renderer>(true);
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

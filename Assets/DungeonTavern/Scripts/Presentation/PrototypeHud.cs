using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private PrototypeCameraOrbit orbit;

        private GUIStyle style;

        private void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = Mathf.Max(28, Mathf.RoundToInt(Screen.height / 28f)),
                wordWrap = true,
                padding = new RectOffset(22, 22, 16, 16),
                normal = { textColor = new Color(0.92f, 0.88f, 0.76f) }
            };

            string yaw = orbit == null ? "?" : $"{orbit.CurrentCardinalYaw:0}°";
            GUI.Box(
                new Rect(14f, 14f, Mathf.Min(680f, Screen.width - 28f), 150f),
                $"DUNGEON TAVERN 2.5D\nWASD: move   Hold Q / E: rotate   SPACE: vault counter   Facing: {yaw}\nMain 2.5D scene — debug controls enabled.",
                style);
        }
    }
}

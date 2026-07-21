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
                fontSize = 15,
                normal = { textColor = new Color(0.92f, 0.88f, 0.76f) }
            };

            string yaw = orbit == null ? "?" : $"{orbit.CurrentCardinalYaw:0}°";
            GUI.Box(
                new Rect(14f, 14f, 325f, 72f),
                $"DUNGEON TAVERN 2.5D\nWASD: move   Q / E: rotate 90°   Facing: {yaw}\nMain 2.5D scene — debug controls enabled.",
                style);
        }
    }
}

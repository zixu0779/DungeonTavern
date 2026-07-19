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
                $"2.5D PIXEL ROTATION PROTOTYPE\nWASD: move   Q / E: rotate 90°   Facing: {yaw}\nFormal tavern scene and assets are unchanged.",
                style);
        }
    }
}

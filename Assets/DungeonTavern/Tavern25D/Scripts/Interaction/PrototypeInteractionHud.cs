using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class PrototypeInteractionHud : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionController interactionController;
        [SerializeField] private BusinessDayController businessDay;

        private GUIStyle promptStyle;
        private GUIStyle heldItemStyle;

        private void OnGUI()
        {
            if (interactionController == null)
                return;

            promptStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                normal = { textColor = new Color(1f, 0.91f, 0.62f) }
            };

            heldItemStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 15,
                normal = { textColor = new Color(0.85f, 0.93f, 1f) }
            };

            string prompt = interactionController.CurrentPrompt;
            if (!string.IsNullOrEmpty(prompt))
            {
                float width = 280f;
                GUI.Box(
                    new Rect((Screen.width - width) * 0.5f, Screen.height - 82f, width, 42f),
                    prompt,
                    promptStyle);
            }

            if (interactionController.CurrentItem != HeldItem.None)
            {
                GUI.Box(
                    new Rect(Screen.width - 230f, Screen.height - 62f, 216f, 42f),
                    "Holding: Test Drink",
                    heldItemStyle);
            }

            if (businessDay != null)
            {
                GUI.Box(
                    new Rect(Screen.width - 230f, 14f, 216f, 42f),
                    businessDay.State == BusinessDayState.Completed
                        ? $"Day complete: {businessDay.CompletedCustomers} / {businessDay.TotalCustomers}"
                        : $"Customers: {businessDay.CompletedCustomers} / {businessDay.TotalCustomers}  Active: {businessDay.ActiveCustomers}",
                    heldItemStyle);
            }
        }
    }
}

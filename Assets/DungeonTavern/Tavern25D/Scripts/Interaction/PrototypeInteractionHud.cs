using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class PrototypeInteractionHud : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionController interactionController;
        [SerializeField] private BusinessDayController businessDay;

        private GUIStyle promptStyle;
        private GUIStyle heldItemStyle;
        private PlayerOrderBook orderBook;

        private void OnGUI()
        {
            interactionController ??= FindAnyObjectByType<PlayerInteractionController>();
            if (interactionController == null)
                return;
            orderBook ??= FindAnyObjectByType<PlayerOrderBook>();

            promptStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(28, Mathf.RoundToInt(Screen.height / 28f)),
                padding = new RectOffset(24, 24, 14, 14),
                normal = { textColor = new Color(1f, 0.91f, 0.62f) }
            };

            heldItemStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = Mathf.Max(28, Mathf.RoundToInt(Screen.height / 28f)),
                padding = new RectOffset(20, 20, 12, 12),
                normal = { textColor = new Color(0.85f, 0.93f, 1f) }
            };

            string prompt = interactionController.CurrentPrompt;
            if (!string.IsNullOrEmpty(prompt))
            {
                float width = Mathf.Min(620f, Screen.width - 40f);
                GUI.Box(
                    new Rect((Screen.width - width) * 0.5f, Screen.height - 116f, width, 76f),
                    prompt,
                    promptStyle);
            }

            if (interactionController.CurrentItem != HeldItem.None)
            {
                GUI.Box(
                    new Rect(Screen.width - 430f, Screen.height - 96f, 416f, 76f),
                    "手持：麦芽饮料",
                    heldItemStyle);
            }

            if (orderBook != null && orderBook.HasOrder)
            {
                string customerName = orderBook.Customer == null
                    ? "客人"
                    : orderBook.Customer.CustomerName;
                string status = orderBook.State == PlayerOrderState.Prepared
                    ? "已制作，可以送达"
                    : "待制作";
                GUI.Box(
                    new Rect(14f, 14f, 360f, 104f),
                    $"当前订单\n{customerName} · 麦芽饮料\n{status}",
                    heldItemStyle);
            }

            if (businessDay != null)
            {
                GUI.Box(
                    new Rect(Screen.width - 500f, 14f, 486f, 76f),
                    businessDay.State == BusinessDayState.Completed
                        ? $"Day complete: {businessDay.CompletedCustomers} / {businessDay.TotalCustomers}"
                        : $"Customers: {businessDay.CompletedCustomers} / {businessDay.TotalCustomers}  Active: {businessDay.ActiveCustomers}",
                    heldItemStyle);
            }
        }
    }
}

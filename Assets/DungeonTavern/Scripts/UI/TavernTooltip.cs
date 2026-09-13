using UnityEngine;
using UnityEngine.EventSystems;
namespace DungeonTavern.UI
{
    public sealed class TavernTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject View;
        public void OnPointerEnter(PointerEventData e) { if(View)TavernFadeIn.Show(View,true); }
        public void OnPointerExit(PointerEventData e) { if(View)TavernFadeIn.Show(View,false); }
        void OnDisable() { if(View)View.SetActive(false); }
    }
}

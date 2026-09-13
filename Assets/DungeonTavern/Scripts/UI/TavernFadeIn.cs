using UnityEngine;
namespace DungeonTavern.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TavernFadeIn : MonoBehaviour
    {
        CanvasGroup group;float progress;bool visible=true;bool blockRaycasts;
        void Awake(){group=GetComponent<CanvasGroup>();blockRaycasts=group.blocksRaycasts;}
        void OnEnable(){group=GetComponent<CanvasGroup>();progress=0;group.alpha=0;visible=true;}
        public void SetVisible(bool value)
        {
            if(value&&!gameObject.activeSelf)gameObject.SetActive(true);
            visible=value;
            if(group){group.interactable=value;group.blocksRaycasts=value&&blockRaycasts;}
        }
        void Update()
        {
            progress=Mathf.MoveTowards(progress,visible?1:0,Time.unscaledDeltaTime/.22f);
            group.alpha=progress*progress*(3-2*progress);
            if(!visible&&progress<=0)gameObject.SetActive(false);
        }
        public static void Add(GameObject view){if(!view.GetComponent<TavernFadeIn>())view.AddComponent<TavernFadeIn>();}
        public static void Show(GameObject view,bool visible)=>view.GetComponent<TavernFadeIn>().SetVisible(visible);
    }
}

using DungeonTavern.Tavern25D;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    [RequireComponent(typeof(TwoStateProp))]
    public sealed class ChestInteractionPoint : InteractionPoint
    {
        [SerializeField, Min(1.7f)] private float autoCloseDistance = 2.4f;
        [SerializeField, Min(0)] private float autoCloseDelay = .6f;
        private TwoStateProp chest;
        private Transform interactor;
        private float awayTime;
        private bool displayedOpen;
        private void Awake(){chest=GetComponent<TwoStateProp>();displayedOpen=chest.IsOpen;}
        public override string GetPrompt(PlayerHands hands)
        {
            if(chest==null)return string.Empty;
            if(!chest.IsTransitioning)displayedOpen=chest.IsOpen;
            return displayedOpen?"F：关闭箱子":"F：打开箱子";
        }
        public override bool Interact(PlayerHands hands)
        {
            if (chest == null || chest.IsTransitioning) return false;
            interactor = hands != null ? hands.transform : interactor;
            awayTime = 0;
            chest.SetOpen(!chest.IsOpen);
            return true;
        }
        private void Update()
        {
            if (!chest.IsOpen) { awayTime = 0; return; }
            if (interactor == null) interactor = FindAnyObjectByType<PlayerHands>()?.transform;
            if (interactor == null) return;
            Vector3 delta = interactor.position - transform.position;
            delta.y = 0;
            awayTime = delta.sqrMagnitude > autoCloseDistance * autoCloseDistance ? awayTime + Time.deltaTime : 0;
            if (awayTime >= autoCloseDelay && !chest.IsTransitioning) chest.Close();
        }
    }
}

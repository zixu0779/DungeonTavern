using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    [RequireComponent(typeof(PlayerHands))]
    public sealed class HeldCupVisual : MonoBehaviour
    {
        [SerializeField] private Transform handAnchor;
        [SerializeField] private GameObject cupPrefab;
        [SerializeField] private bool keepCupUpright;
        [SerializeField] private Vector3 gripOffset = new Vector3(0, 0, .06f);
        [SerializeField, Min(.05f)] private float cupHeight = .23f;
        private PlayerHands hands;
        private GameObject cup;
        private Vector3 cupCenter;
        public GameObject Cup => cup;
        public float DrinkTilt { get; set; }
        private void OnEnable()
        {
            hands = GetComponent<PlayerHands>();
            hands.ItemChanged += ShowItem;
            ShowItem(hands.CurrentItem);
        }
        private void ShowItem(HeldItem item)
        {
            bool holdingCup = item == HeldItem.EmptyCup || item == HeldItem.TestDrink;
            if (!holdingCup) { if (cup) cup.SetActive(false); return; }
            if (!handAnchor || !cupPrefab) return;
            if (!cup)
            {
                cup = Instantiate(cupPrefab, handAnchor, false);
                cup.name = "HeldWoodenCup";
                if (keepCupUpright) cup.transform.rotation = transform.rotation * Quaternion.Euler(-55f * Mathf.Clamp01(DrinkTilt), 0, 0) * cupPrefab.transform.localRotation;
                foreach (var collider in cup.GetComponentsInChildren<Collider>()) collider.enabled = false;
                var renderers = cup.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    float ratio = cupHeight / Mathf.Max(.001f, bounds.size.y);
                    cup.transform.localScale *= ratio;
                    bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    cupCenter = cup.transform.InverseTransformPoint(bounds.center);
                }
            }
            cup.SetActive(true);
        }
        private void LateUpdate()
        {
            if (!cup || !cup.activeSelf) return;
            if (keepCupUpright) cup.transform.rotation = transform.rotation * Quaternion.Euler(-55f * Mathf.Clamp01(DrinkTilt), 0, 0) * cupPrefab.transform.localRotation;
            cup.transform.position += handAnchor.position + (keepCupUpright ? transform.TransformDirection(gripOffset) : Vector3.zero) - cup.transform.TransformPoint(cupCenter);
        }
        private void OnDisable()
        {
            if (hands) hands.ItemChanged -= ShowItem;
            if (cup) Destroy(cup);
        }
    }
}

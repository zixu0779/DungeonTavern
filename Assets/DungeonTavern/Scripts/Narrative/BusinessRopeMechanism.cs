using System.Collections;
using UnityEngine;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class BusinessRopeMechanism : MonoBehaviour
    {
        [SerializeField] private Transform ropeVisual;
        [SerializeField] private TwoStateProp statusSign;
        [SerializeField, Min(0.05f)] private float pullDistance = 0.22f;
        [SerializeField, Min(0.05f)] private float pullDuration = 0.18f;

        private Coroutine animationRoutine;

        public void Configure(Transform rope, TwoStateProp sign)
        {
            ropeVisual = rope;
            statusSign = sign;
            ApplySign(false);
        }

        public void PullAndSetOpen(bool open)
        {
            ApplySign(open);
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);
            if (ropeVisual != null)
                animationRoutine = StartCoroutine(AnimatePull());
        }

        private void ApplySign(bool open)
        {
            if (statusSign != null)
                statusSign.SetOpen(open);
        }

        private IEnumerator AnimatePull()
        {
            Vector3 rest = ropeVisual.localPosition;
            Vector3 pulled = rest + Vector3.down * pullDistance;
            float elapsed = 0f;
            while (elapsed < pullDuration)
            {
                elapsed += Time.deltaTime;
                ropeVisual.localPosition = Vector3.Lerp(rest, pulled, elapsed / pullDuration);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < pullDuration)
            {
                elapsed += Time.deltaTime;
                ropeVisual.localPosition = Vector3.Lerp(pulled, rest, elapsed / pullDuration);
                yield return null;
            }
            ropeVisual.localPosition = rest;
            animationRoutine = null;
        }
    }
}

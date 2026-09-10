using System.Collections;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonTavern.Tavern25D
{
    [DefaultExecutionOrder(-10000)]
    public sealed class InitialAdditiveSceneLoader : MonoBehaviour
    {
        [SerializeField] private string sceneName = "SealRoom_B1";
        [SerializeField] private GameObject[] hostContentRoots;
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

        private float startupOverlayAlpha = 1f;

        public void Configure(string targetScene, float duration = 0.25f)
        {
            sceneName = targetScene;
            fadeDuration = Mathf.Max(0f, duration);
        }

        private IEnumerator Start()
        {
            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                while (load != null && !load.isDone)
                    yield return null;
            }

            PrototypePlayerMover player = FindAnyObjectByType<PrototypePlayerMover>();
            if (player == null)
            {
                Debug.LogError($"Initial scene loader could not find a player in {sceneName}.", this);
                yield break;
            }

            // The content scene owns the player's starting position and rotation.
            if (!SetHostContentVisible(false))
                yield break;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                startupOverlayAlpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            startupOverlayAlpha = 0f;
        }

        internal string HostSceneName => gameObject.scene.name;

        internal bool SetHostContentVisible(bool visible)
        {
            if (hostContentRoots == null || hostContentRoots.Length == 0)
            {
                Debug.LogError("Initial scene loader requires host content roots.", this);
                return false;
            }

            for (int index = 0; index < hostContentRoots.Length; index++)
            {
                if (hostContentRoots[index] == null)
                {
                    Debug.LogError($"Initial scene loader host content root {index} is missing.", this);
                    return false;
                }
            }

            for (int index = 0; index < hostContentRoots.Length; index++)
                hostContentRoots[index].SetActive(visible);
            return true;
        }


        private void LateUpdate() => DungeonTavern.UI.TavernUI.Instance?.SetFade(this, startupOverlayAlpha);
        private void OnDisable() => DungeonTavern.UI.TavernUI.Instance?.ClearFade(this);
    }
}

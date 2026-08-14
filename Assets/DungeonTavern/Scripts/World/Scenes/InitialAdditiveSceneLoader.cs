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
        [SerializeField] private PrototypePlayerMover player;
        [SerializeField] private Vector3 destinationPosition = new(81f, 0f, 20f);
        [SerializeField] private Vector3 destinationEulerAngles = new(0f, -90f, 0f);
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

        private float startupOverlayAlpha = 1f;

        public void Configure(string targetScene, Vector3 position, Vector3 eulerAngles, float duration = 0.25f)
        {
            sceneName = targetScene;
            destinationPosition = position;
            destinationEulerAngles = eulerAngles;
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

            player ??= FindAnyObjectByType<PrototypePlayerMover>();
            if (player == null)
            {
                Debug.LogError($"Initial scene loader could not find a player in {sceneName}.", this);
                yield break;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(destinationPosition, Quaternion.Euler(destinationEulerAngles));
            Physics.SyncTransforms();
            if (controller != null) controller.enabled = true;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                startupOverlayAlpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            startupOverlayAlpha = 0f;
        }

        private void OnGUI()
        {
            if (startupOverlayAlpha <= 0f)
                return;

            Color previous = GUI.color;
            GUI.depth = -10000;
            GUI.color = new Color(0f, 0f, 0f, startupOverlayAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}

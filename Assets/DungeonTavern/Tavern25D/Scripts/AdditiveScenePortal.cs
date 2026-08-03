using System.Collections;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonTavern.Tavern25D
{
    public static class PlayerAreaTransition
    {
        public static event System.Action<string, string> Started;
        public static event System.Action<string, string> Completed;

        internal static void RaiseStarted(string loadedScene, string unloadedScene)
        {
            Started?.Invoke(loadedScene, unloadedScene);
        }

        internal static void RaiseCompleted(string loadedScene, string unloadedScene)
        {
            Completed?.Invoke(loadedScene, unloadedScene);
        }
    }

    [RequireComponent(typeof(Collider))]
    public sealed class AdditiveScenePortal : MonoBehaviour
    {
        [SerializeField] private string sceneToLoad;
        [SerializeField] private string sceneToUnload;
        [SerializeField] private Vector3 destinationPosition;
        [SerializeField] private Vector3 destinationEulerAngles;
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

        public void Configure(
            string loadScene,
            string unloadScene,
            Vector3 position,
            Vector3 eulerAngles,
            float duration = 0.25f)
        {
            sceneToLoad = loadScene;
            sceneToUnload = unloadScene;
            destinationPosition = position;
            destinationEulerAngles = eulerAngles;
            fadeDuration = Mathf.Max(0f, duration);
        }

        private void OnTriggerEnter(Collider other)
        {
            PrototypePlayerMover player = other.GetComponentInParent<PrototypePlayerMover>();
            if (player == null)
                return;

            SceneTransitionRunner.Transition(
                player,
                sceneToLoad,
                sceneToUnload,
                destinationPosition,
                Quaternion.Euler(destinationEulerAngles),
                fadeDuration);
        }
    }

    internal sealed class SceneTransitionRunner : MonoBehaviour
    {
        private static SceneTransitionRunner instance;
        private static bool transitioning;
        private static float nextAllowedTime;

        private float overlayAlpha;

        public static void Transition(
            PrototypePlayerMover player,
            string sceneToLoad,
            string sceneToUnload,
            Vector3 destination,
            Quaternion rotation,
            float fadeDuration)
        {
            if (player == null || transitioning || Time.unscaledTime < nextAllowedTime)
                return;

            EnsureInstance().StartCoroutine(instance.RunTransition(
                player,
                sceneToLoad,
                sceneToUnload,
                destination,
                rotation,
                fadeDuration));
        }

        private static SceneTransitionRunner EnsureInstance()
        {
            if (instance != null)
                return instance;

            GameObject runner = new("SceneTransitionRunner");
            DontDestroyOnLoad(runner);
            instance = runner.AddComponent<SceneTransitionRunner>();
            return instance;
        }

        private IEnumerator RunTransition(
            PrototypePlayerMover player,
            string sceneToLoad,
            string sceneToUnload,
            Vector3 destination,
            Quaternion rotation,
            float fadeDuration)
        {
            transitioning = true;
            PlayerAreaTransition.RaiseStarted(sceneToLoad, sceneToUnload);
            yield return Fade(0f, 1f, fadeDuration);

            if (!string.IsNullOrWhiteSpace(sceneToLoad))
            {
                Scene target = SceneManager.GetSceneByName(sceneToLoad);
                if (!target.isLoaded)
                {
                    AsyncOperation load = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
                    while (load != null && !load.isDone)
                        yield return null;
                }
            }

            // The B1 scene contains its authored startup Player. When the persistent
            // player returns later, additive loading creates that scene copy again.
            // Keep the player that initiated this transition and remove only newcomers.
            PrototypePlayerMover[] players = FindObjectsByType<PrototypePlayerMover>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < players.Length; index++)
            {
                if (players[index] != null && players[index] != player)
                    Destroy(players[index].gameObject);
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            player.transform.SetPositionAndRotation(destination, rotation);
            Physics.SyncTransforms();
            if (controller != null)
                controller.enabled = true;

            if (!string.IsNullOrWhiteSpace(sceneToUnload))
            {
                Scene oldScene = SceneManager.GetSceneByName(sceneToUnload);
                if (oldScene.isLoaded)
                {
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(oldScene);
                    while (unload != null && !unload.isDone)
                        yield return null;
                }
            }

            yield return Fade(1f, 0f, fadeDuration);
            PlayerAreaTransition.RaiseCompleted(sceneToLoad, sceneToUnload);
            nextAllowedTime = Time.unscaledTime + 0.15f;
            transitioning = false;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                overlayAlpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                overlayAlpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            overlayAlpha = to;
        }

        private void OnGUI()
        {
            if (overlayAlpha <= 0f)
                return;

            Color previous = GUI.color;
            GUI.depth = -10000;
            GUI.color = new Color(0f, 0f, 0f, overlayAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}

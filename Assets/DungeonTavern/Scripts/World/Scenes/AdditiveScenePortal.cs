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
        [SerializeField, Tooltip("Hierarchy path of the arrival Transform in the target scene, including its root object.")]
        private string destinationPath;
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

        public void Configure(
            string loadScene,
            string unloadScene,
            string arrivalPath,
            float duration = 0.25f)
        {
            sceneToLoad = loadScene;
            sceneToUnload = unloadScene;
            destinationPath = arrivalPath;
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
                destinationPath,
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
            string destinationPath,
            float fadeDuration)
        {
            if (player == null || transitioning || Time.unscaledTime < nextAllowedTime)
                return;

            Scene target = SceneManager.GetSceneByName(sceneToLoad);
            if (string.IsNullOrWhiteSpace(destinationPath) || string.IsNullOrWhiteSpace(sceneToLoad)
                || sceneToLoad == sceneToUnload
                || (!target.isLoaded && !Application.CanStreamedLevelBeLoaded(sceneToLoad)))
            {
                Debug.LogError($"Scene transition cancelled: invalid target '{sceneToLoad}/{destinationPath}'.");
                return;
            }

            EnsureInstance().StartCoroutine(instance.RunTransition(
                player,
                sceneToLoad,
                sceneToUnload,
                destinationPath,
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
            string destinationPath,
            float fadeDuration)
        {
            transitioning = true;
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
                FindObjectsInactive.Include);
            for (int index = 0; index < players.Length; index++)
            {
                if (players[index] != null && players[index] != player)
                    Destroy(players[index].gameObject);
            }

            Transform destination = FindDestination(SceneManager.GetSceneByName(sceneToLoad), destinationPath);
            if (destination == null)
            {
                Debug.LogError($"Scene transition cancelled: arrival '{destinationPath}' was not found in '{sceneToLoad}'.", this);
                yield return Fade(1f, 0f, fadeDuration);
                transitioning = false;
                yield break;
            }

            InitialAdditiveSceneLoader loader = FindAnyObjectByType<InitialAdditiveSceneLoader>();
            if (loader == null || !loader.SetHostContentVisible(sceneToLoad == loader.HostSceneName))
            {
                Debug.LogError("Scene transition cancelled: host content visibility could not be updated.", this);
                yield return Fade(1f, 0f, fadeDuration);
                transitioning = false;
                yield break;
            }

            PlayerAreaTransition.RaiseStarted(sceneToLoad, sceneToUnload);
            CharacterController controller = player.GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controller != null)
                controller.enabled = false;
            player.transform.SetPositionAndRotation(destination.position, destination.rotation);
            Physics.SyncTransforms();
            if (controller != null)
                controller.enabled = controllerWasEnabled;

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

            yield return null;
            var cutout=FindAnyObjectByType<DungeonTavern.Prototypes.Rotation25D.DialogueOcclusionFader>();
            if(cutout)yield return cutout.PrepareForReveal();
            yield return Fade(1f, 0f, fadeDuration);
            PlayerAreaTransition.RaiseCompleted(sceneToLoad, sceneToUnload);
            nextAllowedTime = Time.unscaledTime + 0.15f;
            transitioning = false;
        }

        internal static Transform FindDestination(Scene scene, string path)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(path))
                return null;

            int separator = path.IndexOf('/');
            string rootName = separator < 0 ? path : path.Substring(0, separator);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                    return separator < 0 ? root.transform : root.transform.Find(path.Substring(separator + 1));
            }
            return null;
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


        private void LateUpdate() => DungeonTavern.UI.TavernUI.Instance?.SetFade(this, overlayAlpha);
        private void OnDisable() => DungeonTavern.UI.TavernUI.Instance?.ClearFade(this);
    }
}

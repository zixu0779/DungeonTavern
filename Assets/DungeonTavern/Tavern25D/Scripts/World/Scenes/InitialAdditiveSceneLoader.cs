using System.Collections;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonTavern.Tavern25D
{
    public sealed class InitialAdditiveSceneLoader : MonoBehaviour
    {
        [SerializeField] private string sceneName = "SealRoom_B1";
        [SerializeField] private PrototypePlayerMover player;
        [SerializeField] private Vector3 destinationPosition = new(81f, 0f, 20f);
        [SerializeField] private Vector3 destinationEulerAngles = new(0f, -90f, 0f);
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

        public void Configure(string targetScene, Vector3 position, Vector3 eulerAngles, float duration = 0.25f)
        {
            sceneName = targetScene;
            destinationPosition = position;
            destinationEulerAngles = eulerAngles;
            fadeDuration = Mathf.Max(0f, duration);
        }

        private IEnumerator Start()
        {
            yield return null;
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
        }
    }
}

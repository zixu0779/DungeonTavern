#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonTavern.Tavern25D
{
    /// <summary>
    /// Lets developers press Play while SealRoom_B1 is open. This type is stripped
    /// from player builds and does not alter the formal Tavern_Main entry flow.
    /// </summary>
    internal static class EditorContentScenePlayBootstrap
    {
        private const string PersistentHostScene = "Tavern_Main";
        private const string SealRoomScene = "SealRoom_B1";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePersistentHostForContentScenePreview()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != SealRoomScene ||
                SceneManager.GetSceneByName(PersistentHostScene).isLoaded)
            {
                return;
            }

            SceneManager.LoadScene(PersistentHostScene, LoadSceneMode.Additive);
            Debug.Log(
                $"Editor preview loaded {PersistentHostScene} additively for {SealRoomScene}. " +
                "This bootstrap is excluded from player builds.");
        }
    }
}
#endif

using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTavern.Gameplay.Interaction
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GamePauseMenu : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }
        private static GamePauseMenu instance;
        private float previousTimeScale = 1;
        private bool previousAudioPause;
        private bool controls, confirmQuit;
        public bool ControlsVisible => controls;
        public bool ConfirmingQuit => confirmQuit;
        public void ShowControls() { controls=true;confirmQuit=false; }
        public void ShowQuit() { confirmQuit=true;controls=false; }
        public void Back() { controls=confirmQuit=false; }
        public void ConfirmExit()
        {
            SetPaused(false);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() { instance = null; IsPaused = false; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (instance != null) return;
            instance = new GameObject("GamePauseMenu").AddComponent<GamePauseMenu>();
            DontDestroyOnLoad(instance.gameObject);
        }
        public void SetPaused(bool value)
        {
            if (value == IsPaused) return;
            if (value) { previousTimeScale = Time.timeScale; previousAudioPause = AudioListener.pause; }
            IsPaused = value;
            Time.timeScale = value ? 0 : previousTimeScale;
            AudioListener.pause = value || previousAudioPause;
            controls = confirmQuit = false;
        }
        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
            if (!IsPaused && DungeonTavern.UI.TavernUI.WindowOpen)
            { FindAnyObjectByType<TavernMenuSystem>()?.Close(); return; }
            if (IsPaused && (controls || confirmQuit)) { controls = confirmQuit = false; }
            else SetPaused(!IsPaused);
        }
        private void OnDestroy()
        {
            if (instance != this) return;
            if (IsPaused) SetPaused(false);
            instance = null;
        }

    }
}

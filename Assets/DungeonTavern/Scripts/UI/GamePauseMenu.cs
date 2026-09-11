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
        private bool controls, confirmQuit, history, historyReturnsToGame;
        public bool HistoryVisible => history;
        public void ShowHistory(bool returnToDialogue=false) { history=true;historyReturnsToGame=returnToDialogue;controls=confirmQuit=false; }
        public bool ControlsVisible => controls;
        public bool ConfirmingQuit => confirmQuit;
        public void ShowControls() { controls=true;confirmQuit=history=false; }
        public void ShowQuit() { confirmQuit=true;controls=history=false; }
        public void Back() { if(history&&historyReturnsToGame){SetPaused(false);return;}controls=confirmQuit=history=false; }
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
            controls = confirmQuit = history = false;
        }
        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
            if (!IsPaused && DungeonTavern.UI.TavernUI.WindowOpen)
            { FindAnyObjectByType<TavernMenuSystem>()?.Close(); return; }
            if (IsPaused && (controls || confirmQuit || history)) Back();
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

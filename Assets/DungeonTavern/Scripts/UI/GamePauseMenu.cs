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
        private GUIStyle button, title, text;

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
            if (IsPaused && (controls || confirmQuit)) { controls = confirmQuit = false; }
            else SetPaused(!IsPaused);
        }
        private void OnDestroy()
        {
            if (instance != this) return;
            if (IsPaused) SetPaused(false);
            instance = null;
        }
        private void OnGUI()
        {
            GUI.depth = -15000;
            float scale = Mathf.Clamp(Screen.height / 900f, .6f, 1.4f);
            button ??= new GUIStyle(GUI.skin.button);
            title ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            text ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true };
            button.fontSize = Mathf.RoundToInt(24 * scale); title.fontSize = Mathf.RoundToInt(32 * scale); text.fontSize = Mathf.RoundToInt(23 * scale);
            if (GUI.Button(new Rect(20 * scale, 20 * scale, 112 * scale, 48 * scale), IsPaused ? "继续游戏" : "菜单", button)) SetPaused(!IsPaused);
            if (!IsPaused) return;
            var old = GUI.color;
            GUI.color = new Color(.08f, .09f, .11f, .78f); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = old;
            float width = Mathf.Min((controls ? 680 : 440) * scale, Screen.width - 32), height = (controls ? 600 : confirmQuit ? 310 : 470) * scale;
            Rect panel = new((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
            GUI.color = new Color(.12f, .105f, .09f, 1); GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = old;
            GUI.Label(new Rect(panel.x + 20 * scale, panel.y + 20 * scale, width - 40 * scale, 52 * scale), controls ? "操作说明" : confirmQuit ? "退出游戏？" : "游戏已暂停", title);
            float x = panel.x + 40 * scale, y = panel.y + 95 * scale, inner = width - 80 * scale, row = 58 * scale;
            if (confirmQuit)
            {
                GUI.Label(new Rect(x, y, inner, 90 * scale), "当前 Demo 暂不支持存档。退出后，本次进度不会保留。", text);
                if (GUI.Button(new Rect(x, panel.yMax - 92 * scale, inner * .47f, row), "取消", button)) confirmQuit = false;
                if (GUI.Button(new Rect(x + inner * .53f, panel.yMax - 92 * scale, inner * .47f, row), "确认退出", button))
                {
                    SetPaused(false);
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            }
            else if (controls)
            {
                string[] instructions = { "W / A / S / D    移动", "Q / E    切换四个观察方向", "F    与附近物品或人物交互", "Space    朝吧台移动时翻越", "M    打开 / 关闭酒馆菜单", "Enter / Space    继续对话", "数字键或点击    选择对话选项", "Esc    暂停 / 返回" };
                foreach (string instruction in instructions)
                {
                    GUI.Label(new Rect(x, y, inner, 40 * scale), instruction, text);
                    y += 46 * scale;
                }
                if (GUI.Button(new Rect(x, panel.yMax - 88 * scale, inner, row), "返回", button)) controls = false;
            }
            else
            {
                GUI.enabled = false;
                GUI.Button(new Rect(x, y, inner, row), "保存进度（暂不可用）", button); y += row + 14 * scale;
                GUI.Button(new Rect(x, y, inner, row), "读取进度（暂不可用）", button); y += row + 14 * scale;
                GUI.enabled = true;
                if (GUI.Button(new Rect(x, y, inner, row), "操作说明", button)) controls = true; y += row + 14 * scale;
                if (GUI.Button(new Rect(x, y, inner, row), "退出游戏", button)) confirmQuit = true;
                GUI.Label(new Rect(x, panel.yMax - 46 * scale, inner, 30 * scale), "按 Esc 继续游戏", new GUIStyle(text) { alignment = TextAnchor.MiddleCenter });
            }
            // Consume background clicks so paused gameplay UI cannot receive them.
            if (Event.current.type is EventType.MouseDown or EventType.MouseUp) Event.current.Use();
        }
    }
}

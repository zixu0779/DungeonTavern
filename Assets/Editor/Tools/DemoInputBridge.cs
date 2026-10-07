using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DungeonTavern.Prototypes.Rotation25D;

// Editor-only input for demo capture. Never moves transforms directly.
[InitializeOnLoad]
public static class DemoInputBridge
{
    [Serializable] public class Command { public string keys; public float seconds; public bool click; public float mouseX, mouseY, scroll; }
    [Serializable] public class Sequence { public Command[] steps; }
    const string CommandPath = "Tools/DemoCapture/FullFlow.json";
    const string ResultPath = "Library/DemoInputResult.json";
    [Serializable] class Result
    {
        public string keys;
        public Vector3 before, afterRelease, afterSettled;
        public bool pressedObserved, releasedObserved, keyboardRestored;
        public Vector3[] stepPositions;
    }
    static Keyboard injected, previous;
    static Mouse injectedMouse, previousMouse;
    static Vector2 mousePosition;
    static Key[] keys;
    static double releaseAt, finishAt;
    static PrototypePlayerMover player;
    static Result result;
    static bool released;
    static Command[] steps;
    static Key[][] stepKeys;
    static int stepIndex;
    static double stepEnd, elapsed;

    static DemoInputBridge()
    {
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        EditorApplication.quitting += Stop;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode && injected != null)
            {
                result.afterSettled = player ? player.transform.position : Vector3.zero;
                Stop();
                result.keyboardRestored = previous == null || Keyboard.current == previous;
                result.releasedObserved = injected == null;
                File.WriteAllText(ResultPath, JsonUtility.ToJson(result, true));
            }
            else Stop();
        };
    }

    [MenuItem("Tools/Dungeon Tavern/Demo Input/Run Command")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            throw new InvalidOperationException("Requires unpaused Play Mode.");
        string json = File.ReadAllText(CommandPath);
        var commands = JsonUtility.FromJson<Sequence>(json)?.steps
            ?? new[] { JsonUtility.FromJson<Command>(json) };
        if (commands.Length == 0 || commands.Length > 200)
            throw new ArgumentException("Specify 1 to 200 steps.");
        var allKeys = new Key[commands.Length][];
        double total = 0;
        for (int step = 0; step < commands.Length; step++)
        {
            var command = commands[step];
            if (command == null || float.IsNaN(command.seconds) || command.seconds <= 0 || command.seconds > 30)
                throw new ArgumentException("Specify 0 < seconds <= 30 for each step.");
            total += command.seconds;
            string[] names = string.IsNullOrWhiteSpace(command.keys) ? Array.Empty<string>() : command.keys.Split('+');
            var parsed = new Key[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                if (!Enum.TryParse(names[i], true, out parsed[i]) ||
                    !(parsed[i] == Key.W || parsed[i] == Key.A || parsed[i] == Key.S ||
                      parsed[i] == Key.D || parsed[i] == Key.F || parsed[i] == Key.Q || parsed[i] == Key.E || parsed[i] == Key.Enter || parsed[i] == Key.Space || parsed[i] == Key.M || parsed[i] == Key.Escape))
                    throw new ArgumentException("Allowed keys: W A S D F Q E Enter Space M Escape, joined by +.");
            }
            allKeys[step] = parsed;
        }
        if (total > 300) throw new ArgumentException("Sequence must be at most 300 seconds.");
        Stop();
        player = UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMover>();
        if (!player) throw new InvalidOperationException("Player not found.");
        steps = commands;
        stepKeys = allKeys;
        stepIndex = 0;
        keys = stepKeys[0];
        previous = Keyboard.current;
        injected = InputSystem.AddDevice<Keyboard>("DemoCaptureKeyboard");
        if (Array.Exists(commands, c => c.click || c.scroll != 0))
        {
            previousMouse = Mouse.current;
            injectedMouse = InputSystem.AddDevice<Mouse>("DemoCaptureMouse");
        }
        result = new Result { keys = json, before = player.transform.position, stepPositions = new Vector3[steps.Length] };
        released = false;
        elapsed = 0;
        stepEnd = steps[0].seconds;
        releaseAt = total;
        finishAt = releaseAt + .5;
        InputSystem.onAfterUpdate += Inject;
        InputSystem.onAfterUpdate += Observe;
        EditorApplication.update += Tick;
    }

    static void Inject()
    {
        if (injected == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        elapsed += Time.timeScale > 0 ? Time.deltaTime : Time.unscaledDeltaTime;
        while (!released && elapsed >= stepEnd)
        {
            result.stepPositions[stepIndex] = player.transform.position;
            if (++stepIndex == steps.Length)
            {
                released = true;
                result.afterRelease = player.transform.position;
                break;
            }
            keys = stepKeys[stepIndex];
            stepEnd += steps[stepIndex].seconds;
        }
        injected.MakeCurrent();
        InputState.Change(injected, released ? new KeyboardState() : new KeyboardState(keys));
        if (injectedMouse != null)
        {
            bool click = !released && steps[stepIndex].click;
            if (click || !released && steps[stepIndex].scroll != 0) mousePosition = new Vector2(steps[stepIndex].mouseX * Screen.width, steps[stepIndex].mouseY * Screen.height);
            injectedMouse.MakeCurrent();
            InputState.Change(injectedMouse, new MouseState { position = mousePosition, scroll = new Vector2(0, released ? 0 : steps[stepIndex].scroll * Time.unscaledDeltaTime), buttons = (ushort)(click ? 1 : 0) });
        }
    }
    static void Observe()
    {
        if (injected == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        bool pressed = false;
        foreach (var key in keys) pressed |= injected[key].isPressed;
        if (!released) result.pressedObserved |= pressed;
        else result.releasedObserved |= !pressed;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || !player) { Stop(); return; }
        if (elapsed < finishAt) return;
        result.afterSettled = player.transform.position;
        Stop();
        result.keyboardRestored = previous == null || Keyboard.current == previous;
        File.WriteAllText(ResultPath, JsonUtility.ToJson(result, true));
    }

    [MenuItem("Tools/Dungeon Tavern/Demo Input/Release All")]
    public static void Stop()
    {
        EditorApplication.update -= Tick;
        InputSystem.onAfterUpdate -= Inject;
        InputSystem.onAfterUpdate -= Observe;
        if (injected != null && injected.added) InputSystem.RemoveDevice(injected);
        injected = null;
        if (injectedMouse != null && injectedMouse.added) InputSystem.RemoveDevice(injectedMouse);
        injectedMouse = null;
        if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
        if (previous != null && previous.added) previous.MakeCurrent();
    }
}

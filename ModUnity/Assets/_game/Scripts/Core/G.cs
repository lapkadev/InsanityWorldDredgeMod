using System;
using InsanityWorldMod.Core.Dialogue;
using UnityEngine;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        // Vanilla DREDGE shortcuts
        public static GameManager     GameVanilla => GameManager.Instance;
        public static SaveData        SaveVanilla => GameManager.Instance?.SaveData;
        public static Player          Player      => GameManager.Instance?.Player;
        public static UIController    UI          => GameManager.Instance?.UI;
        public static LanguageManager Lang        => GameManager.Instance?.LanguageManager;

        // Our state
        public static GameState            Game         { get; set; }
        public static RunState             Run          { get; set; }
        public static SaveState            Save         { get; set; }
        public static InsanityWorldConfig  Config       { get; set; }
        public static InsanityDialogueView DialogueView { get; set; }

        /// <summary>
        /// Logger delegates. Default implementation uses UnityEngine.Debug.
        /// Api layer reassigns these at bootstrap to route into Winch's logger
        /// (which writes to dedicated mod log file).
        /// </summary>
        public static class Log
        {
            public static Action<string> Info  { get; set; } = msg => UnityEngine.Debug.Log(msg);
            public static Action<string> Warn  { get; set; } = msg => UnityEngine.Debug.LogWarning(msg);
            public static Action<string> Error { get; set; } = msg => UnityEngine.Debug.LogError(msg);
            public static Action<string> Debug { get; set; } = msg => UnityEngine.Debug.Log($"[DEBUG] {msg}");
        }
    }
}

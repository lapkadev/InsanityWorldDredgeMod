using InsanityWorldMod.Core;
using UnityEngine;
using UnityEngine.Localization.Settings;
using static InsanityWorldMod.Api.Constants;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Api
{
    public static partial class Constants
    {
        public const string YARN_TEST_HELLO_NODE = "lapkadev_test_hello";
        public const KeyCode TEST_DIALOGUE_KEY   = KeyCode.F10;
    }

    /// <summary>
    /// MonoBehaviour host for periodic Core ticks (auto-save loop, Run.Tick).
    /// Spawned and parented to a DontDestroyOnLoad GameObject by <c>EntrySystem.OnLoad()</c>.
    /// </summary>
    public class MainBehaviour : MonoBehaviour
    {
        private float _nextAutoSaveAt;

        public void Awake()
        {
            _nextAutoSaveAt = Time.time + AUTO_SAVE_INTERVAL_SEC;
            G.Log.Debug($"{nameof(MainBehaviour)} has loaded!");

            // Diagnostic logging - uncomment to dump locale codes when DREDGE updates
            // its language list (e.g. to find exact Identifier.Code for new locales).
            // DumpLocales();
            // ApplicationEvents.Instance.OnLocaleChanged += locale =>
            //     G.Log.Info($"Locale changed to: '{locale?.Identifier.Code}' ({locale?.LocaleName})");
        }

        /// <summary>
        /// Dumps current and all available locale codes to the Winch log.
        /// </summary>
        private static void DumpLocales()
        {
            var current = LocalizationSettings.SelectedLocale;
            G.Log.Info($"Current locale: '{current?.Identifier.Code}' ({current?.LocaleName})");

            var availableLocales = LocalizationSettings.AvailableLocales?.Locales;
            if (availableLocales == null) { G.Log.Warn("AvailableLocales is null at Awake (init not complete?)"); return; }

            foreach (var loc in availableLocales)
                G.Log.Info($"  - Available locale: '{loc?.Identifier.Code}' ({loc?.LocaleName})");
        }

        public void Update()
        {
            if (Time.time >= _nextAutoSaveAt)
            {
                _nextAutoSaveAt = Time.time + AUTO_SAVE_INTERVAL_SEC;
                Save();
            }

            G.Run?.Tick(Time.deltaTime);

            if (Input.GetKeyDown(TEST_DIALOGUE_KEY))
            {
                G.Log.Info($"{TEST_DIALOGUE_KEY} pressed - starting test dialogue '{YARN_TEST_HELLO_NODE}'");
                GameManager.Instance?.DialogueRunner?.StartDialogue(YARN_TEST_HELLO_NODE);
            }
        }
    }
}

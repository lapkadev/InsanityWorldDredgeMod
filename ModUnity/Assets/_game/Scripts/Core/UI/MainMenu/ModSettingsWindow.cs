using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public class ModSettingsWindow : MonoBehaviour
    {
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI[] skinTexts;

        private Action _onClosed;

        private void Awake()
        {
            saveButton.onClick.AddListener(OnSave);
            cancelButton.onClick.AddListener(OnCancel);
            ApplyVanillaFont();
        }

        public void Open(Action onClosed)
        {
            _onClosed = onClosed;
            playerNameInput.text = G.Config.PlayerName;
        }

        private void OnSave()
        {
            G.Config.PlayerName = playerNameInput.text;
            SaveConfig();
            G.Log.Info($"ModSettingsWindow: PlayerName saved = '{G.Config.PlayerName}'");
            Close();
        }

        private void OnCancel() => Close();

        private void Close()
        {
            _onClosed?.Invoke();
            Destroy(gameObject);
        }

        private void ApplyVanillaFont()
        {
            foreach (var text in skinTexts)
            {
                var bypass = text.GetComponent<LocalizeFontBypass>();
                if (bypass == null)
                    bypass = text.gameObject.AddComponent<LocalizeFontBypass>();
                bypass.textField = text;
                bypass.tableString = MENU_FONT_TABLE;
                bypass.tableEntryString = MENU_FONT_ENTRY;
            }
        }
    }
}

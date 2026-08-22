using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string SETTINGS_TAB_PROFILE  = "Profile";
        public const string SETTINGS_WINDOW_NAME  = "InsanityWorldSettings";
    }

    [AddToMainMenuScene]
    public class InsanityWorldMenu : MonoBehaviour
    {
        private readonly List<GameObject> _submenuButtons = new List<GameObject>();
        private readonly List<GameObject> _hiddenButtons = new List<GameObject>();

        private GameObject _settingsPanel;
        private TMP_InputField _playerNameInput;
        private GameObject _settings;
        private int _closeAction;
        private int _submenuBack;
        private TMP_InputField _profileNameInput;

        public void Start()
        {
            if (G.Config == null || !G.Config.IsTransitionPhaseCompleted)
                return;

            AddMainMenuButton("Insanity World", OpenSubmenu, 0);
        }

        private void OpenSubmenu()
        {
            HideExistingButtons();

            if (G.LastSession != null)
                AddSubmenuButton("Continue", OnContinue);

            AddSubmenuButton("Load/New World (Offline)", OnLoadNewOffline);
            AddSubmenuButton("Load/New World (Online)", OnLoadNewOnline);
            AddSubmenuButton("Mod Settings", OnModSettings);
            AddSubmenuButton("Player Name", OnPlayerNameOld);
            AddSubmenuButton("Back", CloseSubmenu);
            RegisterSubmenuBack();
        }

        private void CloseSubmenu()
        {
            RemoveSubmenuBack();

            foreach (var button in _submenuButtons)
                Destroy(button);

            _submenuButtons.Clear();

            foreach (var button in _hiddenButtons)
                if (button != null)
                    button.SetActive(true);

            _hiddenButtons.Clear();
        }

        private void HideExistingButtons()
        {
            foreach (Transform child in G.MenuButtonContainer)
            {
                if (!child.gameObject.activeSelf)
                    continue;

                child.gameObject.SetActive(false);
                _hiddenButtons.Add(child.gameObject);
            }
        }

        private void AddSubmenuButton(string text, Action onClick)
        {
            int index = G.MenuButtonContainer.childCount;
            _submenuButtons.Add(AddMainMenuButton(text, onClick, index));
        }

        private void OnModSettings()
        {
            RemoveSubmenuBack();

            if (_settings != null)
                Destroy(_settings);

            _settings = CreateSettingsClone(SETTINGS_WINDOW_NAME);
            if (_settings == null)
                return;

            SetupSettingsTab();

            ShowSettings(_settings);
            SetSettingsCloseHandler(_settings, CloseModSettings);
            RegisterCloseAction();
            Log.Info("Menu: Mod Settings opened (Profile tab)");
        }

        private void SetupSettingsTab()
        {
            var tabs = SetSettingsTabs(_settings, new string[] { SETTINGS_TAB_PROFILE });
            if (tabs.Length == 0 || tabs[0] == null)
                return;

            var controlList = tabs[0];
            AddLabel(controlList, "Player Name", 26f);
            _profileNameInput = AddInputField(controlList, G.Config.PlayerName);

            var inputBg = _profileNameInput.GetComponent<Image>();
            if (inputBg != null)
                inputBg.color = Color.black;

            if (_profileNameInput.textComponent != null)
                _profileNameInput.textComponent.color = Color.white;

            _profileNameInput.caretColor = Color.white;
        }

        private void RegisterCloseAction()
        {
            _closeAction = AddInputBackAction(CloseModSettings, false);
        }

        private void CloseModSettings()
        {
            if (_closeAction != 0)
            {
                RemoveInputBackAction(_closeAction);
                _closeAction = 0;
            }

            HideUnpausePrompt();

            if (_profileNameInput != null)
            {
                G.Config.PlayerName = _profileNameInput.text;
                SaveConfig();
                _profileNameInput = null;
            }

            if (_settings != null)
            {
                HideSettings(_settings);
                Destroy(_settings);
            }
            _settings = null;
            RegisterSubmenuBack();
        }

        private void RegisterSubmenuBack()
        {
            _submenuBack = AddInputBackAction(CloseSubmenu, true);
        }

        private void RemoveSubmenuBack()
        {
            if (_submenuBack == 0)
                return;

            RemoveInputBackAction(_submenuBack);
            _submenuBack = 0;
        }

        private void OnPlayerNameOld()
        {
            RemoveSubmenuBack();
            SetSubmenuActive(false);
            OpenSettings();
        }

        private void OpenSettings()
        {
            _settingsPanel = AddPanel("InsanityModSettings", new Vector2(560f, 420f), Vector2.zero);
            AddLabel(_settingsPanel.transform, "Mod Settings", 40f);
            AddLabel(_settingsPanel.transform, "Player Name", 26f);

            _playerNameInput = AddInputField(_settingsPanel.transform, G.Config.PlayerName);
            var inputLayout = _playerNameInput.gameObject.AddComponent<LayoutElement>();
            inputLayout.preferredHeight = 50f;
            inputLayout.preferredWidth = 460f;

            AddButton(_settingsPanel.transform, "Save", SaveSettings);
            AddButton(_settingsPanel.transform, "Cancel", CancelSettings);
        }

        private void SaveSettings()
        {
            if (_playerNameInput != null)
            {
                G.Config.PlayerName = _playerNameInput.text;
                SaveConfig();
                Log.Info($"Menu: PlayerName saved = '{G.Config.PlayerName}'");
            }
            CloseSettings();
        }

        private void CancelSettings() => CloseSettings();

        private void CloseSettings()
        {
            Destroy(_settingsPanel);
            _settingsPanel = null;
            _playerNameInput = null;
            SetSubmenuActive(true);
            RegisterSubmenuBack();
        }

        private void SetSubmenuActive(bool active)
        {
            foreach (var button in _submenuButtons)
                if (button != null)
                    button.SetActive(active);
        }

        private void OnContinue() => Log.Info($"Menu: Continue (WorldId={G.LastSession.WorldId}, Mode={G.LastSession.Mode}) [stub]");
        private void OnLoadNewOffline() => Log.Info("Menu: Load/New (Offline) [stub]");
        private void OnLoadNewOnline() => Log.Info("Menu: Load/New (Online) [stub]");
    }
}

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    [AddToMainMenuScene]
    public class InsanityWorldMenu : MonoBehaviour
    {
        private readonly List<GameObject> _submenuButtons = new List<GameObject>();
        private readonly List<GameObject> _hiddenButtons = new List<GameObject>();

        private GameObject _settingsPanel;
        private TMP_InputField _playerNameInput;
        private GameObject _settings;
        private DredgePlayerActionPress _closeAction;
        private DredgePlayerActionPress _submenuBack;
        private TMP_InputField _profileNameInput;

        public void Start()
        {
            if (G.Config == null || !G.Config.IsTransitionPhaseCompleted)
                return;

            MenuUIHelper.AddMainMenuButton("Insanity World", OpenSubmenu, 0);
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
            foreach (Transform child in MenuUIHelper.ButtonContainer.transform)
            {
                if (!child.gameObject.activeSelf)
                    continue;
                child.gameObject.SetActive(false);
                _hiddenButtons.Add(child.gameObject);
            }
        }

        private void AddSubmenuButton(string text, Action onClick)
        {
            int index = MenuUIHelper.ButtonContainer.transform.childCount;
            _submenuButtons.Add(MenuUIHelper.AddMainMenuButton(text, onClick, index));
        }

        private void OnModSettings()
        {
            RemoveSubmenuBack();

            if (_settings != null)
                Destroy(_settings);

            var vanilla = FindObjectOfType<SettingsDialog>(true);
            if (vanilla == null)
            {
                G.Log.Warn("Menu: vanilla SettingsDialog not found in scene");
                return;
            }

            _settings = Instantiate(vanilla.gameObject);
            _settings.name = "InsanityWorldSettings";

            SetupSettingsTab();

            _settings.GetComponent<SettingsDialog>().Show();
            WireCloseButtons();
            RegisterCloseAction();
            G.Log.Info("Menu: Mod Settings opened (Profile tab)");
        }

        private void SetupSettingsTab()
        {
            var container = _settings.GetComponentInChildren<TabbedPanelContainer>(true);
            if (container == null)
            {
                G.Log.Warn("Menu: clone TabbedPanelContainer not found");
                return;
            }

            var tabs = container.TabbedPanels;
            for (int i = tabs.Count - 1; i >= 1; i--)
            {
                Destroy(tabs[i].tab.gameObject);
                Destroy(tabs[i].panel.gameObject);
                tabs.RemoveAt(i);
            }
            container.RequestShowablePanels(new List<int> { 0 });

            var controlList = tabs[0].panel.transform.Find("Container/ControlScroller/ControlList");
            if (controlList != null)
            {
                for (int i = controlList.childCount - 1; i >= 0; i--)
                    Destroy(controlList.GetChild(i).gameObject);

                MenuUIHelper.AddLabel(controlList, "Player Name", 26f);
                _profileNameInput = MenuUIHelper.AddInputField(controlList, G.Config.PlayerName);

                var inputBg = _profileNameInput.GetComponent<Image>();
                if (inputBg != null)
                    inputBg.color = Color.black;
                if (_profileNameInput.textComponent != null)
                    _profileNameInput.textComponent.color = Color.white;
                _profileNameInput.caretColor = Color.white;
            }

            var tabText = tabs[0].tab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tabText != null)
            {
                var localize = tabText.GetComponent<LocalizeStringEvent>();
                if (localize != null)
                    Destroy(localize);
                tabText.text = "Profile";
            }
        }

        private void WireCloseButtons()
        {
            var buttonBar = _settings.transform.Find("TabbedPanelContainer/ButtonBar");
            if (buttonBar == null)
            {
                G.Log.Warn("Menu: clone ButtonBar not found (check hierarchy path)");
                return;
            }

            foreach (var wrapper in buttonBar.GetComponentsInChildren<BasicButtonWrapper>(true))
                wrapper.OnClick = CloseModSettings;
        }

        private void RegisterCloseAction()
        {
            _closeAction = new DredgePlayerActionPress("prompt.back", GameManager.Instance.Input.Controls.Back);
            _closeAction.evaluateWhenPaused = true;
            _closeAction.OnPressComplete = CloseModSettings;
            GameManager.Instance.Input.AddActionListener(new DredgePlayerActionPress[] { _closeAction }, ActionLayer.SYSTEM);
        }

        private void CloseModSettings()
        {
            if (_closeAction != null)
            {
                GameManager.Instance.Input.RemoveActionListener(new DredgePlayerActionPress[] { _closeAction }, ActionLayer.SYSTEM);
                _closeAction = null;
            }

            GameManager.Instance.PauseListener.CanShowUnpauseAction(false);

            if (_profileNameInput != null)
            {
                G.Config.PlayerName = _profileNameInput.text;
                SaveConfig();
                _profileNameInput = null;
            }

            if (_settings != null)
            {
                var dialog = _settings.GetComponent<SettingsDialog>();
                if (dialog != null)
                    dialog.Hide();
                Destroy(_settings);
            }
            _settings = null;
            RegisterSubmenuBack();
        }

        private void RegisterSubmenuBack()
        {
            _submenuBack = new DredgePlayerActionPress("prompt.back", GameManager.Instance.Input.Controls.Back);
            _submenuBack.showInControlArea = true;
            _submenuBack.evaluateWhenPaused = true;
            _submenuBack.OnPressComplete = CloseSubmenu;
            GameManager.Instance.Input.AddActionListener(new DredgePlayerActionPress[] { _submenuBack }, ActionLayer.SYSTEM);
        }

        private void RemoveSubmenuBack()
        {
            if (_submenuBack == null)
                return;

            GameManager.Instance.Input.RemoveActionListener(new DredgePlayerActionPress[] { _submenuBack }, ActionLayer.SYSTEM);
            _submenuBack = null;
        }

        private void OnPlayerNameOld()
        {
            RemoveSubmenuBack();
            SetSubmenuActive(false);
            OpenSettings();
        }

        private void OpenSettings()
        {
            _settingsPanel = MenuUIHelper.AddPanel("InsanityModSettings", new Vector2(560f, 420f), Vector2.zero);
            MenuUIHelper.AddLabel(_settingsPanel.transform, "Mod Settings", 40f);
            MenuUIHelper.AddLabel(_settingsPanel.transform, "Player Name", 26f);

            _playerNameInput = MenuUIHelper.AddInputField(_settingsPanel.transform, G.Config.PlayerName);
            var inputLayout = _playerNameInput.gameObject.AddComponent<LayoutElement>();
            inputLayout.preferredHeight = 50f;
            inputLayout.preferredWidth = 460f;

            MenuUIHelper.AddButton(_settingsPanel.transform, "Save", SaveSettings);
            MenuUIHelper.AddButton(_settingsPanel.transform, "Cancel", CancelSettings);
        }

        private void SaveSettings()
        {
            if (_playerNameInput != null)
            {
                G.Config.PlayerName = _playerNameInput.text;
                SaveConfig();
                G.Log.Info($"Menu: PlayerName saved = '{G.Config.PlayerName}'");
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

        private void OnContinue() => G.Log.Info($"Menu: Continue (WorldId={G.LastSession.WorldId}, Mode={G.LastSession.Mode}) [stub]");
        private void OnLoadNewOffline() => G.Log.Info("Menu: Load/New (Offline) [stub]");
        private void OnLoadNewOnline() => G.Log.Info("Menu: Load/New (Online) [stub]");
    }
}

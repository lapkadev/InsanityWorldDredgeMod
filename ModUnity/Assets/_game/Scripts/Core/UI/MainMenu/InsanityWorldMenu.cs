using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
        private GameObject _settingsClone;
        private DredgePlayerActionPress _closeAction;

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
            AddSubmenuButton("Load/New (Offline)", OnLoadNewOffline);
            AddSubmenuButton("Load/New (Online)", OnLoadNewOnline);
            AddSubmenuButton("Mod Settings", OnModSettings);
            AddSubmenuButton("Player Name", OnPlayerNameOld);
            AddSubmenuButton("Back", CloseSubmenu);
        }

        private void CloseSubmenu()
        {
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
            if (_settingsClone != null)
                Destroy(_settingsClone);

            var vanilla = FindObjectOfType<SettingsDialog>(true);
            if (vanilla == null)
            {
                G.Log.Warn("Menu: vanilla SettingsDialog not found in scene");
                return;
            }

            _settingsClone = Instantiate(vanilla.gameObject);
            _settingsClone.name = "InsanityWorldSettingsClone";
            _settingsClone.GetComponent<SettingsDialog>().Show();
            WireCloneCloseButtons();
            RegisterCloseAction();
            G.Log.Info("Menu: vanilla SettingsDialog cloned and shown");
        }

        private void WireCloneCloseButtons()
        {
            var buttonBar = _settingsClone.transform.Find("TabbedPanelContainer/ButtonBar");
            if (buttonBar == null)
            {
                G.Log.Warn("Menu: clone ButtonBar not found (check hierarchy path)");
                return;
            }

            foreach (var wrapper in buttonBar.GetComponentsInChildren<BasicButtonWrapper>(true))
                wrapper.OnClick = CloseSettingsClone;
        }

        private void RegisterCloseAction()
        {
            _closeAction = new DredgePlayerActionPress("prompt.back", GameManager.Instance.Input.Controls.Back);
            _closeAction.evaluateWhenPaused = true;
            _closeAction.OnPressComplete = CloseSettingsClone;
            GameManager.Instance.Input.AddActionListener(new DredgePlayerActionPress[] { _closeAction }, ActionLayer.SYSTEM);
        }

        private void CloseSettingsClone()
        {
            if (_closeAction != null)
            {
                GameManager.Instance.Input.RemoveActionListener(new DredgePlayerActionPress[] { _closeAction }, ActionLayer.SYSTEM);
                _closeAction = null;
            }

            GameManager.Instance.PauseListener.CanShowUnpauseAction(false);

            if (_settingsClone != null)
            {
                var dialog = _settingsClone.GetComponent<SettingsDialog>();
                if (dialog != null)
                    dialog.Hide();
                Destroy(_settingsClone);
            }
            _settingsClone = null;
        }

        private void OnPlayerNameOld()
        {
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

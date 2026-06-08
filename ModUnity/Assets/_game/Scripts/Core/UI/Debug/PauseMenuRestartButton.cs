using TMPro;
using UnityEngine;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public class PauseMenuRestartButton : MonoBehaviour
    {
        private GameObject _injectedButton;

        public void Awake()
        {
            if (ApplicationEvents.Instance != null)
                ApplicationEvents.Instance.OnToggleSettings += OnToggleSettings;
        }

        public void OnDestroy()
        {
            if (ApplicationEvents.Instance != null)
                ApplicationEvents.Instance.OnToggleSettings -= OnToggleSettings;

            if (_injectedButton != null) Destroy(_injectedButton);
        }

        private void OnToggleSettings(bool show)
        {
            if (!show) return;
            // ApplicationEvents.OnToggleSettings fires from BOTH the in-game pause menu
            // AND the title-screen settings dialog. We only want to inject in-game -
            // G.Player is non-null only after a save is loaded (gameplay scene active).
            if (G.Player == null) return;
            if (_injectedButton != null) return;
            TryInject();
        }

        private void TryInject()
        {
            var quitBtn = FindObjectOfType<QuitToMenuButton>(includeInactive: true);
            if (quitBtn == null) { G.Log.Warn("PauseMenuRestartButton: QuitToMenuButton not found"); return; }

            var quitGo = quitBtn.gameObject;
            var parent = quitGo.transform.parent;
            if (parent == null) { G.Log.Warn("PauseMenuRestartButton: quit button has no parent"); return; }

            _injectedButton = Instantiate(quitGo, parent);
            _injectedButton.name = "InsanityRestartRunButton";
            _injectedButton.SetActive(true);

            // Strip vanilla quit-to-menu logic.
            var vanillaQuit = _injectedButton.GetComponent<QuitToMenuButton>();
            if (vanillaQuit != null) Destroy(vanillaQuit);

            // Rewire BasicButtonWrapper click handler to our restart action.
            var bbw = _injectedButton.GetComponent<BasicButtonWrapper>();
            if (bbw != null)
            {
                bbw.OnClick = OnRestartClicked;

                var lse = bbw.LocalizedString;
                if (lse != null) lse.enabled = false;
            }

            // Override label text directly (localization disabled above).
            var label = _injectedButton.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            if (label != null) label.text = "RESTART RUN";

            // Move to the leftmost slot in the bottom row layout.
            _injectedButton.transform.SetSiblingIndex(0);

            G.Log.Info("PauseMenuRestartButton: injected into pause/settings dialog");
        }

        private void OnRestartClicked()
        {
            TeleportToLastDock();

            // Close the pause/settings dialog after triggering restart.
            if (G.GameVanilla?.PauseListener != null)
                G.GameVanilla.PauseListener.OnUnpausePressComplete();
        }
    }
}

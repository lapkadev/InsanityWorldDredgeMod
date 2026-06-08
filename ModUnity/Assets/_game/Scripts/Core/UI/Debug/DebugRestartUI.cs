using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public class DebugRestartUI : MonoBehaviour
    {
        private GameObject _buttonObject;

        public void Start()
        {
            var gameCanvas = GameObject.Find("GameCanvases/GameCanvas");
            if (gameCanvas == null) { G.Log.Warn("DebugRestartUI: GameCanvas not found"); return; }

            _buttonObject = new GameObject("InsanityDebugRestartButton");
            _buttonObject.transform.SetParent(gameCanvas.transform, false);

            // Position: screen top, center of left half (25% along X).
            var rt = _buttonObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.25f, 1f);
            rt.anchorMax = new Vector2(0.25f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(120, 40);

            var image = _buttonObject.AddComponent<Image>();
            image.color = new Color(0.5f, 0.15f, 0.15f, 0.9f);

            var button = _buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => TeleportToLastDock());

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(_buttonObject.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "RESTART";
            label.fontSize = 18;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            G.Log.Debug("DebugRestartUI: button created");
        }

        public void OnDestroy()
        {
            if (_buttonObject != null) Destroy(_buttonObject);
        }
    }
}

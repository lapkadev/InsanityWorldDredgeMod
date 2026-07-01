using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string MENU_BUTTON_CONTAINER_PATH = "Canvases/MenuCanvas/ButtonContainer";
        public const string MENU_CANVAS_PATH = "Canvases/MenuCanvas";
        public const string MENU_FONT_TABLE = "Fonts";
        public const string MENU_FONT_ENTRY = "DefaultFont";
    }

    public class MenuUIHelper : MonoBehaviour
    {
        private static GameObject _buttonContainer;
        private static GameObject _menuCanvas;
        private static GameObject _buttonPrefab;

        public static GameObject ButtonContainer => _buttonContainer;
        public static GameObject MenuCanvas => _menuCanvas;

        public void Awake()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
            OnSceneChanged(default, SceneManager.GetActiveScene());
        }

        public void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene previous, Scene current)
        {
            if (current.name != TITLE_SCENE_NAME)
                return;

            _buttonContainer = GameObject.Find(MENU_BUTTON_CONTAINER_PATH);
            _menuCanvas = GameObject.Find(MENU_CANVAS_PATH);
            CreateButtonPrefab();
        }

        private static void CreateButtonPrefab()
        {
            var source = GameObject.Find(MENU_BUTTON_CONTAINER_PATH + "/Settings");
            if (source == null)
                return;

            _buttonPrefab = Instantiate(source);
            _buttonPrefab.SetActive(false);
            _buttonPrefab.name = "InsanityMenuButtonPrefab";

            var settingsButton = _buttonPrefab.GetComponent<SettingsButton>();
            if (settingsButton != null)
                DestroyImmediate(settingsButton);

            var localize = _buttonPrefab.GetComponentInChildren<LocalizeStringEvent>();
            if (localize != null)
                DestroyImmediate(localize);
        }

        public static GameObject AddMainMenuButton(string text, Action onClick, int index)
        {
            var button = AddButton(_buttonContainer.transform, text, onClick);
            button.transform.SetSiblingIndex(index);
            return button;
        }

        public static GameObject AddButton(Transform parent, string text, Action onClick)
        {
            var button = Instantiate(_buttonPrefab, parent);
            button.name = $"{text}_Button";
            button.transform.localScale = Vector3.one;

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.text = text;
            label.enabled = true;
            ApplyFont(label);

            button.GetComponent<BasicButtonWrapper>().OnClick = onClick;
            button.SetActive(true);
            return button;
        }

        public static TextMeshProUGUI AddLabel(Transform parent, string text, float fontSize)
        {
            var obj = new GameObject($"{text}_Label", typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            var label = obj.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            ApplyFont(label);
            return label;
        }

        public static TMP_InputField AddInputField(Transform parent, string initialText)
        {
            var obj = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            obj.transform.SetParent(parent, false);
            obj.transform.localScale = Vector3.one;

            var input = obj.GetComponent<TMP_InputField>();
            input.pointSize = 28f;
            input.text = initialText;

            input.onFocusSelectAll = false;
            input.customCaretColor = true;
            input.caretColor = Color.black;
            input.caretWidth = 3;
            input.selectionColor = new Color(0.3f, 0.5f, 1f, 0.5f);
            input.caretPosition = initialText.Length;

            if (input.textComponent != null)
                input.textComponent.color = Color.black;

            obj.SetActive(false);
            obj.SetActive(true);

            return input;
        }

        public static GameObject AddPanel(string name, Vector2 size, Vector2 anchoredPosition)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(_menuCanvas.transform, false);

            var rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20f;
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.childForceExpandHeight = false;
            return panel;
        }

        private static void ApplyFont(TextMeshProUGUI label)
        {
            var bypass = label.GetComponent<LocalizeFontBypass>();
            if (bypass == null)
                bypass = label.gameObject.AddComponent<LocalizeFontBypass>();
            bypass.textField = label;
            bypass.tableString = MENU_FONT_TABLE;
            bypass.tableEntryString = MENU_FONT_ENTRY;
        }
    }
}

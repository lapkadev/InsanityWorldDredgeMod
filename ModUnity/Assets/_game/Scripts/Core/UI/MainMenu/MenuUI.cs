using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static InsanityWorldMod.Core.DredgeHooks;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        public static RectTransform MenuCanvas;
        public static RectTransform MenuButtonContainer;
        public static GameObject MenuButtonTemplate;
    }

    public static partial class Funcs
    {
        public static GameObject AddMainMenuButton(string text, Action onClick, int index)
        {
            var button = AddButton(G.MenuButtonContainer, text, onClick);
            button.transform.SetSiblingIndex(index);
            return button;
        }

        public static GameObject AddButton(Transform parent, string text, Action onClick)
        {
            var button = CloneUiNode(G.MenuButtonTemplate, $"{text}_Button", parent);
            button.transform.localScale = Vector3.one;

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.text = text;
            label.enabled = true;
            label.UseLocalizedFont();

            SetMenuButtonClick(button, onClick);
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
            label.UseLocalizedFont();
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
            panel.transform.SetParent(G.MenuCanvas, false);

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
    }
}

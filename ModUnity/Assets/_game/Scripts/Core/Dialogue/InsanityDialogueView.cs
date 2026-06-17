using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string TAG_VANILLA_UI  = "vanilla_ui";
        public const string TAG_LAPKADEV_UI = "lapkadev_ui";
    }
}

namespace InsanityWorldMod.Core.Dialogue
{
    public class InsanityDialogueView : DialogueViewBase
    {
        private GameObject _container;
        private TMP_Text _characterText;
        private TMP_Text _lineText;
        private Button _continueButton;
        private GameObject _optionsContainer;
        private Button[] _optionButtons;
        private TMP_Text[] _optionTexts;

        private Action _onLineFinished;
        private Action<int> _onOptionSelected;

        public static InsanityDialogueView Spawn()
        {
            var go = new GameObject("InsanityDialogueView");
            UnityEngine.Object.DontDestroyOnLoad(go);
            return go.AddComponent<InsanityDialogueView>();
        }

        private void Awake()
        {
            BuildUI();
            HideContainer();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            _container = new GameObject("Container");
            _container.transform.SetParent(canvasGo.transform, false);
            var containerRect = _container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, 0.05f);
            containerRect.anchorMax = new Vector2(0.9f, 0.35f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            var bg = _container.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.0f, 0.1f, 0.92f);

            var charNameGo = new GameObject("CharacterName");
            charNameGo.transform.SetParent(_container.transform, false);
            var charRect = charNameGo.AddComponent<RectTransform>();
            charRect.anchorMin = new Vector2(0, 1);
            charRect.anchorMax = new Vector2(1, 1);
            charRect.pivot = new Vector2(0.5f, 1);
            charRect.sizeDelta = new Vector2(0, 40);
            charRect.anchoredPosition = new Vector2(0, -10);
            _characterText = charNameGo.AddComponent<TextMeshProUGUI>();
            _characterText.fontSize = 28;
            _characterText.alignment = TextAlignmentOptions.Center;
            _characterText.color = new Color(0.8f, 0.3f, 0.6f);

            var lineGo = new GameObject("LineText");
            lineGo.transform.SetParent(_container.transform, false);
            var lineRect = lineGo.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0, 0);
            lineRect.anchorMax = new Vector2(1, 1);
            lineRect.offsetMin = new Vector2(30, 60);
            lineRect.offsetMax = new Vector2(-30, -60);
            _lineText = lineGo.AddComponent<TextMeshProUGUI>();
            _lineText.fontSize = 24;
            _lineText.alignment = TextAlignmentOptions.TopLeft;
            _lineText.color = new Color(0.9f, 0.85f, 0.95f);

            var contGo = new GameObject("ContinueButton");
            contGo.transform.SetParent(_container.transform, false);
            var contRect = contGo.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(1, 0);
            contRect.anchorMax = new Vector2(1, 0);
            contRect.pivot = new Vector2(1, 0);
            contRect.sizeDelta = new Vector2(150, 40);
            contRect.anchoredPosition = new Vector2(-20, 15);
            var contBg = contGo.AddComponent<Image>();
            contBg.color = new Color(0.2f, 0.1f, 0.25f);
            _continueButton = contGo.AddComponent<Button>();
            _continueButton.targetGraphic = contBg;
            _continueButton.onClick.AddListener(OnContinuePressed);
            var contTextGo = new GameObject("Text");
            contTextGo.transform.SetParent(contGo.transform, false);
            var contTextRect = contTextGo.AddComponent<RectTransform>();
            contTextRect.anchorMin = Vector2.zero;
            contTextRect.anchorMax = Vector2.one;
            contTextRect.offsetMin = Vector2.zero;
            contTextRect.offsetMax = Vector2.zero;
            var contText = contTextGo.AddComponent<TextMeshProUGUI>();
            contText.text = "Continue >";
            contText.fontSize = 18;
            contText.alignment = TextAlignmentOptions.Center;
            contText.color = new Color(0.9f, 0.85f, 0.95f);

            _optionsContainer = new GameObject("OptionsContainer");
            _optionsContainer.transform.SetParent(_container.transform, false);
            var optRect = _optionsContainer.AddComponent<RectTransform>();
            optRect.anchorMin = new Vector2(0, 0);
            optRect.anchorMax = new Vector2(1, 0);
            optRect.pivot = new Vector2(0.5f, 0);
            optRect.sizeDelta = new Vector2(0, 180);
            optRect.anchoredPosition = new Vector2(0, 15);
            var vlg = _optionsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.padding = new RectOffset(40, 40, 0, 0);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;

            _optionButtons = new Button[5];
            _optionTexts = new TMP_Text[5];
            for (int i = 0; i < 5; i++)
            {
                var btnGo = new GameObject($"Option{i}");
                btnGo.transform.SetParent(_optionsContainer.transform, false);
                var btnRect = btnGo.AddComponent<RectTransform>();
                btnRect.sizeDelta = new Vector2(0, 32);
                var btnBg = btnGo.AddComponent<Image>();
                btnBg.color = new Color(0.15f, 0.05f, 0.2f);
                _optionButtons[i] = btnGo.AddComponent<Button>();
                _optionButtons[i].targetGraphic = btnBg;
                int captured = i;
                _optionButtons[i].onClick.AddListener(() => OnOptionPressed(captured));
                var btnTextGo = new GameObject("Text");
                btnTextGo.transform.SetParent(btnGo.transform, false);
                var btnTextRect = btnTextGo.AddComponent<RectTransform>();
                btnTextRect.anchorMin = Vector2.zero;
                btnTextRect.anchorMax = Vector2.one;
                btnTextRect.offsetMin = new Vector2(15, 0);
                btnTextRect.offsetMax = new Vector2(-15, 0);
                _optionTexts[i] = btnTextGo.AddComponent<TextMeshProUGUI>();
                _optionTexts[i].fontSize = 20;
                _optionTexts[i].alignment = TextAlignmentOptions.Left;
                _optionTexts[i].color = new Color(0.85f, 0.8f, 0.95f);
            }
        }

        private bool ShouldRender(LocalizedLine line)
        {
            if (USE_VANILLA_DIALOGUE_ALWAYS) return false;
            var currentNode = GameManager.Instance?.DialogueRunner?.CurrentNodeName ?? "";
            bool isOurNode = currentNode.StartsWith(PREFIX);
            bool hasVanillaTag = HasTag(line, TAG_VANILLA_UI);
            bool hasLapkadevTag = HasTag(line, TAG_LAPKADEV_UI);
            return (isOurNode && !hasVanillaTag) || hasLapkadevTag;
        }

        private static bool HasTag(LocalizedLine line, string tag)
        {
            return line.Metadata?.Any(m => m == tag) == true;
        }

        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            if (!ShouldRender(dialogueLine))
            {
                onDialogueLineFinished();
                return;
            }
            ShowContainer();
            _characterText.text = dialogueLine.CharacterName ?? "";
            _lineText.text = dialogueLine.TextWithoutCharacterName.Text;
            _optionsContainer.SetActive(false);
            _continueButton.gameObject.SetActive(true);
            _onLineFinished = onDialogueLineFinished;
        }

        public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            if (USE_VANILLA_DIALOGUE_ALWAYS) return;
            var currentNode = GameManager.Instance?.DialogueRunner?.CurrentNodeName ?? "";
            if (!currentNode.StartsWith(PREFIX)) return;

            ShowContainer();
            _continueButton.gameObject.SetActive(false);
            _optionsContainer.SetActive(true);
            _onOptionSelected = onOptionSelected;

            for (int i = 0; i < _optionButtons.Length; i++)
            {
                if (i < dialogueOptions.Length)
                {
                    _optionButtons[i].gameObject.SetActive(true);
                    _optionButtons[i].interactable = dialogueOptions[i].IsAvailable;
                    _optionTexts[i].text = dialogueOptions[i].Line.TextWithoutCharacterName.Text;
                }
                else
                {
                    _optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        public override void DialogueStarted()
        {
            var currentNode = GameManager.Instance?.DialogueRunner?.CurrentNodeName ?? "";
            if (!currentNode.StartsWith(PREFIX)) return;
            ForceCursorVisible();
        }

        private void Update()
        {
            if (_container != null && _container.activeSelf) ForceCursorVisible();
        }

        private static void ForceCursorVisible()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public override void DismissLine(Action onDismissalComplete)
        {
            HideContainer();
            onDismissalComplete();
        }

        public override void DialogueComplete()
        {
            HideContainer();
        }

        private void OnContinuePressed()
        {
            var cb = _onLineFinished;
            _onLineFinished = null;
            cb?.Invoke();
        }

        private void OnOptionPressed(int index)
        {
            var cb = _onOptionSelected;
            _onOptionSelected = null;
            cb?.Invoke(index);
        }

        private void ShowContainer() => _container.SetActive(true);
        private void HideContainer() => _container.SetActive(false);
    }
}

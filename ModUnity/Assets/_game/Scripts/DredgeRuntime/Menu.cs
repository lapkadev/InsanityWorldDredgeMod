using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Components;
using InsanityWorldMod.Core;
using Core = InsanityWorldMod.Core;
using static InsanityWorldMod.Core.Funcs;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public const string MENU_BUTTON_CONTAINER_PATH = "Canvases/MenuCanvas/ButtonContainer";
        public const string MENU_CANVAS_PATH           = "Canvases/MenuCanvas";
        public const string MENU_SOURCE_BUTTON_NAME    = "Settings";
        public const string MENU_BUTTON_TEMPLATE_NAME  = "MenuButtonTemplate";
        public const string TITLE_SCENE_NAME           = "Title";
    }

    public static partial class Funcs
    {
        public static void AddHooksMenu()
        {
            DredgeHooks.SetMenuButtonClick = (button, onClick) =>
            {
                var wrapper = button.GetComponent<BasicButtonWrapper>();
                if (wrapper == null)
                {
                    Log.Warn("SetMenuButtonClick: button wrapper not found on cloned button");
                    return;
                }

                wrapper.OnClick = onClick;
            };
        }

        public static void AddListenersMenuScene()
        {
            SceneManager.activeSceneChanged += (previous, current) => RefreshMenuRefs(current);
            RefreshMenuRefs(SceneManager.GetActiveScene());
        }

        public static void RefreshMenuRefs(Scene scene)
        {
            if (scene.name != TITLE_SCENE_NAME)
            {
                Core.G.MenuCanvas = null;
                Core.G.MenuButtonContainer = null;
                Core.G.MenuButtonTemplate = null;
                return;
            }

            Core.G.MenuCanvas = FindUiNode(MENU_CANVAS_PATH, "menu canvas");
            Core.G.MenuButtonContainer = FindUiNode(MENU_BUTTON_CONTAINER_PATH, "button container");
            Core.G.MenuButtonTemplate = CreateMenuButtonTemplate();

            MenuSceneManager.SpawnMenuObjects();
        }

        public static GameObject CreateMenuButtonTemplate()
        {
            var source = GameObject.Find($"{MENU_BUTTON_CONTAINER_PATH}/{MENU_SOURCE_BUTTON_NAME}");
            if (source == null)
            {
                Log.Warn("CreateMenuButtonTemplate: source button not found in scene");
                return null;
            }

            var template = CloneUiNode(source, MENU_BUTTON_TEMPLATE_NAME);
            template.SetActive(false);
            StripMenuButtonBehaviours(template);

            return template;
        }

        public static void StripMenuButtonBehaviours(GameObject template)
        {
            var settingsButton = template.GetComponent<SettingsButton>();
            if (settingsButton != null)
                Object.DestroyImmediate(settingsButton);

            var localize = template.GetComponentInChildren<LocalizeStringEvent>();
            if (localize != null)
                Object.DestroyImmediate(localize);
        }
    }
}

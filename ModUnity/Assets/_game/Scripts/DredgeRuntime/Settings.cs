using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using InsanityWorldMod.Core;
using static InsanityWorldMod.Core.Funcs;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public const string SETTINGS_CONTROL_LIST_PATH = "Container/ControlScroller/ControlList";
        public const string SETTINGS_BUTTON_BAR_PATH   = "TabbedPanelContainer/ButtonBar";
    }

    public static partial class Funcs
    {
        public static void AddHooksSettings()
        {
            DredgeHooks.ShowSettings = clone => GetSettingsDialog(clone)?.Show();
            DredgeHooks.HideSettings = clone => GetSettingsDialog(clone)?.Hide();

            DredgeHooks.CreateSettingsClone = name =>
            {
                var source = FindDredgeSettingsDialog();
                if (source == null)
                    return null;

                return CloneUiNode(source.gameObject, name);
            };

            DredgeHooks.SetSettingsTabs = (clone, titles) =>
            {
                var container = GetSettingsTabContainer(clone);
                if (container == null)
                    return new RectTransform[0];

                TrimSettingsTabs(container.TabbedPanels, titles.Length);
                CloneSettingsTabs(container.TabbedPanels, titles.Length);

                return FillSettingsTabs(container, titles);
            };

            DredgeHooks.SetSettingsCloseHandler = (clone, onClose) =>
            {
                var buttonBar = FindSettingsButtonBar(clone);
                if (buttonBar == null)
                    return;

                foreach (var wrapper in buttonBar.GetComponentsInChildren<BasicButtonWrapper>(true))
                    wrapper.OnClick = onClose;
            };
        }

        public static SettingsDialog FindDredgeSettingsDialog()
        {
            var dialog = Object.FindObjectOfType<SettingsDialog>(true);
            if (dialog == null)
                Log.Warn("FindDredgeSettingsDialog: settings dialog not found in scene");

            return dialog;
        }

        public static SettingsDialog GetSettingsDialog(GameObject clone)
        {
            var dialog = clone.GetComponent<SettingsDialog>();
            if (dialog == null)
                Log.Warn("GetSettingsDialog: clone has no dialog component");

            return dialog;
        }

        public static TabbedPanelContainer GetSettingsTabContainer(GameObject clone)
        {
            var container = clone.GetComponentInChildren<TabbedPanelContainer>(true);
            if (container == null)
            {
                Log.Warn("GetSettingsTabContainer: tabbed panel container not found in clone");
                return null;
            }

            if (container.TabbedPanels.Count == 0)
            {
                Log.Warn("GetSettingsTabContainer: clone has no tabs to reuse");
                return null;
            }

            return container;
        }

        public static void TrimSettingsTabs(List<TabConfig> tabs, int count)
        {
            while (tabs.Count > count)
            {
                var extra = tabs[tabs.Count - 1];
                Object.Destroy(extra.tab.gameObject);
                Object.Destroy(extra.panel.gameObject);
                tabs.RemoveAt(tabs.Count - 1);
            }
        }

        public static void CloneSettingsTabs(List<TabConfig> tabs, int count)
        {
            while (tabs.Count < count)
            {
                var sample = tabs[tabs.Count - 1];
                var tab = Object.Instantiate(sample.tab, sample.tab.transform.parent);
                var panel = Object.Instantiate(sample.panel, sample.panel.transform.parent);
                tabs.Add(new TabConfig { tab = tab, panel = panel });
            }
        }

        public static RectTransform[] FillSettingsTabs(TabbedPanelContainer container, string[] titles)
        {
            var tabs = container.TabbedPanels;
            var showable = new List<int>();
            var contents = new RectTransform[titles.Length];

            for (int i = 0; i < titles.Length; i++)
            {
                SetSettingsTabTitle(tabs[i], titles[i]);
                contents[i] = ClearSettingsTabContent(tabs[i]);
                showable.Add(i);
            }

            container.RequestShowablePanels(showable);
            return contents;
        }

        public static void SetSettingsTabTitle(TabConfig config, string title)
        {
            var text = config.tab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null)
                return;

            var localize = text.GetComponent<LocalizeStringEvent>();
            if (localize != null)
                Object.Destroy(localize);

            text.text = title;
        }

        public static RectTransform ClearSettingsTabContent(TabConfig config)
        {
            var content = config.panel.transform.Find(SETTINGS_CONTROL_LIST_PATH) as RectTransform;
            if (content == null)
            {
                Log.Warn("ClearSettingsTabContent: control list not found in tab panel");
                return null;
            }

            for (int i = content.childCount - 1; i >= 0; i--)
                Object.Destroy(content.GetChild(i).gameObject);

            return content;
        }

        public static Transform FindSettingsButtonBar(GameObject clone)
        {
            var buttonBar = clone.transform.Find(SETTINGS_BUTTON_BAR_PATH);
            if (buttonBar == null)
                Log.Warn("FindSettingsButtonBar: button bar not found in clone");

            return buttonBar;
        }
    }
}

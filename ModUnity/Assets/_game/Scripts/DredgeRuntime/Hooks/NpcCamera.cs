using System.Collections;
using Cinemachine;
using HarmonyLib;
using InsW.Core;
using Winch.Util;
using static InsW.DredgeRuntime.Constants;

namespace InsW.DredgeRuntime
{
    public static partial class Constants
    {
        public const string DOCK_SPEAKER_CAMERAS_PROPERTY = "SpeakerVCams";
    }

    public static partial class Funcs
    {
        public static void AddHooksNpcCamera()
        {
            DredgeHooks.RegisterNpcCamera = RegisterNpcCamera;
        }

        public static void RegisterNpcCamera(string dockId, string npcId, CinemachineVirtualCamera camera)
        {
            var dock = DockUtil.GetDock(dockId);
            if (dock == null)
            {
                Log.Warn($"RegisterNpcCamera: dock '{dockId}' not found");
                return;
            }

            var vcams = AccessTools.Property(typeof(Dock), DOCK_SPEAKER_CAMERAS_PROPERTY)?.GetValue(dock) as IDictionary;
            if (vcams == null)
            {
                Log.Warn($"RegisterNpcCamera: dock '{dockId}' has no camera table");
                return;
            }

            vcams[npcId] = camera;
            Log.Info($"RegisterNpcCamera: '{npcId}' registered at '{dockId}'");
        }
    }
}

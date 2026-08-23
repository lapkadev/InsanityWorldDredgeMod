using System.Collections;
using Cinemachine;
using HarmonyLib;
using InsanityWorldMod.Core;
using Winch.Util;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
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

        public static void RegisterNpcCamera(string dockId, string speakerId, CinemachineVirtualCamera camera)
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

            vcams[speakerId] = camera;
            Log.Info($"RegisterNpcCamera: '{speakerId}' registered at '{dockId}'");
        }
    }
}

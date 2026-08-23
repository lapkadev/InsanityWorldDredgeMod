using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string NPC_CAMERA_KEY_SEPARATOR = "/";
    }

    public static partial class G
    {
        public static Dictionary<string, CinemachineVirtualCamera> NpcCameras    = new Dictionary<string, CinemachineVirtualCamera>();
        public static HashSet<string>                              NpcCameraSets = new HashSet<string>();
    }

    public static partial class Funcs
    {
        public static string NpcCameraKey(string dockId, string speakerId)
        {
            return dockId + NPC_CAMERA_KEY_SEPARATOR + speakerId;
        }

        public static void CreateNpcCameras(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName) || G.NpcCameraSets.Contains(prefabName))
                return;

            if (!G.Prefabs.TryGetValue(prefabName, out var prefab) || prefab == null)
            {
                Log.Warn($"CreateNpcCameras: prefab '{prefabName}' not found among loaded bundles");
                return;
            }

            var obj = Object.Instantiate(prefab);
            obj.name = prefabName;
            G.NpcCameraSets.Add(prefabName);

            int registered = 0;
            foreach (var item in obj.GetComponentsInChildren<NpcCamera>(true))
            {
                if (item.Camera == null)
                {
                    Log.Warn($"CreateNpcCameras: '{item.name}' in '{prefabName}' has no camera reference");
                    continue;
                }

                var key = NpcCameraKey(item.DockId, item.SpeakerId);
                item.Camera.enabled = false;
                G.NpcCameras[key] = item.Camera;
                RegisterNpcCamera(item.DockId, item.SpeakerId, item.Camera);
                registered++;
            }

            Log.Info($"CreateNpcCameras: registered {registered} camera(s) from '{prefabName}'");
        }

        public static void SetNpcCameraEnabled(string dockId, string speakerId, bool enabled)
        {
            var camera = FindNpcCamera(dockId, speakerId);
            if (camera == null)
                return;

            camera.enabled = enabled;
            Log.Info($"SetNpcCameraEnabled: '{NpcCameraKey(dockId, speakerId)}' -> {enabled}");
        }

        public static void SetNpcCameraTransform(string dockId, string speakerId, Vector3 position, Quaternion rotation)
        {
            var camera = FindNpcCamera(dockId, speakerId);
            if (camera == null)
                return;

            camera.transform.position = position;
            camera.transform.rotation = rotation;
            Log.Info($"SetNpcCameraTransform: '{NpcCameraKey(dockId, speakerId)}' -> {position} / {rotation.eulerAngles}");
        }

        public static CinemachineVirtualCamera FindNpcCamera(string dockId, string speakerId)
        {
            var key = NpcCameraKey(dockId, speakerId);
            if (G.NpcCameras.TryGetValue(key, out var camera) && camera != null)
                return camera;

            Log.Warn($"FindNpcCamera: no camera for '{key}'");
            return null;
        }
    }

    public class NpcCamera : MonoBehaviour
    {
        public CinemachineVirtualCamera Camera;
        public string DockId;
        public string SpeakerId;
    }
}

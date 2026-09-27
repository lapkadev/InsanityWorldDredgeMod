using Cinemachine;
using UnityEngine;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static void CreateNpcCameras(GameObject prefab, string npcId)
        {
            if (prefab == null)
                return;

            var obj = Object.Instantiate(prefab);
            obj.name = prefab.name;

            int registered = 0;
            foreach (var item in obj.GetComponentsInChildren<NpcCamera>(true))
            {
                if (item.Camera == null)
                {
                    Log.Warn($"CreateNpcCameras: '{item.name}' in '{prefab.name}' has no camera reference");
                    continue;
                }

                item.Camera.enabled = false;
                RegisterNpcCamera(item.DockId, npcId, item.Camera);
                registered++;
            }

            Log.Info($"CreateNpcCameras: registered {registered} camera(s) from '{prefab.name}' for '{npcId}'");
        }
    }

    public class NpcCamera : MonoBehaviour
    {
        public CinemachineVirtualCamera Camera;
        public string DockId;
    }
}

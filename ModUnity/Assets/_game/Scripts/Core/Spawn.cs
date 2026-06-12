using UnityEngine;

namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static GameObject Spawn(string prefabName, Vector3 position, Quaternion rotation, float scale)
        {
            if (!G.Prefabs.TryGetValue(prefabName, out var prefab) || prefab == null)
            {
                G.Log.Warn($"Spawn: prefab '{prefabName}' not loaded");
                return null;
            }

            var obj = Object.Instantiate(prefab, position, rotation);
            obj.transform.localScale = Vector3.one * scale;
            return obj;
        }
    }
}

using UnityEngine;

namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static RectTransform FindUiNode(string path, string label)
        {
            var obj = GameObject.Find(path);
            if (obj == null)
            {
                Log.Warn($"FindUiNode: {label} not found in scene");
                return null;
            }

            var rect = obj.GetComponent<RectTransform>();
            if (rect == null)
                Log.Warn($"FindUiNode: {label} has no RectTransform");

            return rect;
        }

        public static GameObject CloneUiNode(GameObject source, string name, Transform parent = null)
        {
            var clone = parent == null
                ? Object.Instantiate(source)
                : Object.Instantiate(source, parent, false);

            clone.name = name;
            return clone;
        }
    }
}

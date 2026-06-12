using UnityEngine;

namespace InsanityWorldMod.Core
{
    public static class Fallbacks
    {
        private static Transform _transform;

        public static Transform Transform
        {
            get
            {
                if (_transform == null)
                {
                    var obj = new GameObject("Fallbacks.Transform") { hideFlags = HideFlags.HideAndDontSave };
                    Object.DontDestroyOnLoad(obj);
                    _transform = obj.transform;
                }
                return _transform;
            }
        }
    }
}

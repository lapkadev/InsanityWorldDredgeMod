using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string TITLE_SCENE_NAME = "Title";
    }

    public class MenuSceneManager : MonoBehaviour
    {
        public void Awake()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
            OnSceneChanged(default, SceneManager.GetActiveScene());
        }

        public void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene previous, Scene current)
        {
            if (current.name != TITLE_SCENE_NAME)
                return;

            var host = new GameObject("InsanityWorldMenuObjects");
            foreach (var type in GetMainMenuSceneTypes())
                host.AddComponent(type);
        }

        private static List<Type> GetMainMenuSceneTypes()
        {
            var result = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                    if (type.IsDefined(typeof(AddToMainMenuSceneAttribute), true))
                        result.Add(type);
            }
            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string MENU_OBJECTS_HOST_NAME = "InsanityWorldMenuObjects";
    }

    public static class MenuSceneManager
    {
        private static GameObject _host;

        public static void SpawnMenuObjects()
        {
            if (_host != null)
                UnityEngine.Object.Destroy(_host);

            _host = new GameObject(MENU_OBJECTS_HOST_NAME);
            foreach (var type in GetMainMenuSceneTypes())
                _host.AddComponent(type);

            Log.Info("MenuSceneManager: menu objects spawned");
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

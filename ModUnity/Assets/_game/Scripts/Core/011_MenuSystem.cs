using UnityEngine;

namespace InsanityWorldMod.Core
{
    public class MenuSystem : IInsanityWorldSystem
    {
        public int Order => 11;

        private static GameObject _host;

        public void OnLoad()
        {
            if (_host != null)
                Object.Destroy(_host);

            _host = new GameObject("InsanityWorldMenu");
            _host.AddComponent<MenuUIHelper>();
            _host.AddComponent<MenuSceneManager>();
            Object.DontDestroyOnLoad(_host);
            G.Log.Info("MenuSystem: menu scene manager spawned (Order 11)");
        }
    }
}

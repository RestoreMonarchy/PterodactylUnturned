using RestoreMonarchy.PterodactylUnturned.Services;
using SDG.Framework.Modules;
using UnityEngine;

namespace RestoreMonarchy.PterodactylUnturned
{
    public class PterodactylUnturnedPlugin : IModuleNexus
    {
        private GameObject gameObject;

        public void initialize()
        {
            gameObject = new GameObject();
            gameObject.AddComponent<ServerInfoService>();
        }

        public void shutdown()
        {
            GameObject.Destroy(gameObject);
        }
    }
}
using System.Collections.Generic;

namespace RestoreMonarchy.PterodactylUnturned.Models
{
    public class RocketInfo
    {
        public string Version { get; set; }
        public string PermissionsPath { get; set; }
        public List<LibraryInfo> Libraries { get; set; }
        public List<PluginInfo> Plugins { get; set; }
    }
}

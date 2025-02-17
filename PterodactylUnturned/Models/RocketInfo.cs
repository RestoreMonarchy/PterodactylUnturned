using System.Collections.Generic;

namespace RestoreMonarchy.PterodactylUnturned.Models
{
    public class RocketInfo
    {
        public string DirectoryPath { get; set; }
        public List<PluginInfo> Plugins { get; set; }
        public List<LibraryInfo> Libraries { get; set; }
    }
}

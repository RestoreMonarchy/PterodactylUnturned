using SDG.Framework.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestoreMonarchy.PterodactylUnturned.Helpers
{
    internal static class ModuleHelper
    {
        internal static bool IsModuleEnabled(string moduleName)
        {
            Module module = ModuleHook.getModuleByName(moduleName);
            if (module != null && module.isEnabled)
            {
                return true;
            }
            return false;
        }
    }
}

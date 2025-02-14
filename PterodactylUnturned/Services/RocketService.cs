using RestoreMonarchy.PterodactylUnturned.Models;
using Rocket.API;
using Rocket.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RestoreMonarchy.PterodactylUnturned.Services
{
    internal static class RocketService
    {
        internal static bool IsRocketReady()
        {
            return R.Plugins != null;
        }

        internal static RocketInfo GetRocketInfo()
        {
            string rocketDirectory = Rocket.Unturned.Environment.RocketDirectory;
            string pluginsDirectory = Path.Combine(Rocket.Unturned.Environment.RocketDirectory, Rocket.Core.Environment.PluginsDirectory);

            RocketInfo rocketInfo = new()
            {
                Version = typeof(R).GetType().Assembly.GetName().Version.ToString(),
                PermissionsPath = Path.Combine(rocketDirectory, Rocket.Core.Environment.PermissionFile),
                Libraries = new(),
                Plugins = new()
            };

            List<IRocketPlugin> plugins = R.Plugins.GetPlugins();
            foreach (IRocketPlugin plugin in plugins)
            {
                string pluginDirectory = Path.Combine(pluginsDirectory, plugin.Name);
                string configurationFileName = string.Format(Rocket.Core.Environment.PluginConfigurationFileTemplate, plugin.Name);
                string translationsFileName = string.Format(Rocket.Core.Environment.PluginTranslationFileTemplate, plugin.Name, R.Settings.Instance.LanguageCode);

                bool hasConfiguration = plugin.GetType().GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRocketPlugin<>));

                PluginInfo pluginInfo = new()
                {
                    Name = plugin.Name,
                    Version = plugin.GetType().Assembly.GetName().Version.ToString(),
                    TranslationsPath = plugin.DefaultTranslations.Any() ? Path.Combine(pluginDirectory, translationsFileName) : null,
                    ConfigurationPath = hasConfiguration ? Path.Combine(pluginDirectory, configurationFileName) : null,
                    State = plugin.State.ToString()
                };
                rocketInfo.Plugins.Add(pluginInfo);
            }



            return rocketInfo;
        }
    }
}

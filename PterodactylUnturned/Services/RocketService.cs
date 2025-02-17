using RestoreMonarchy.PterodactylUnturned.Models;
using Rocket.API;
using Rocket.Core;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
                DirectoryPath = rocketDirectory,
                Libraries = new(),
                Plugins = new()
            };

            List<IRocketPlugin> plugins = R.Plugins.GetPlugins();
            List<string> pluginFullNames = new();
            foreach (IRocketPlugin plugin in plugins)
            {
                Assembly assembly = plugin.GetType().Assembly;
                AssemblyName assemblyName = assembly.GetName();
                pluginFullNames.Add(assemblyName.FullName);

                string pluginDirectory = Path.Combine(pluginsDirectory, plugin.Name);
                string configurationFileName = string.Format(Rocket.Core.Environment.PluginConfigurationFileTemplate, plugin.Name);
                string translationsFileName = string.Format(Rocket.Core.Environment.PluginTranslationFileTemplate, plugin.Name, R.Settings.Instance.LanguageCode);

                bool hasConfiguration = plugin.GetType().GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRocketPlugin<>));
                bool hasTranslations = plugin.DefaultTranslations.Any();
    
                PluginInfo pluginInfo = new()
                {
                    Name = plugin.Name,
                    Version = assemblyName.Version.ToString(),
                    DirectoryPath = pluginDirectory,
                    TranslationsFileName = hasTranslations ? translationsFileName : null,
                    ConfigurationFileName = hasConfiguration ? configurationFileName : null,
                    State = plugin.State.ToString()
                };
                rocketInfo.Plugins.Add(pluginInfo);
            }
            
            Dictionary<AssemblyName, string> libraries;

            FieldInfo librariesField = typeof(RocketPluginManager).GetField("libraries", BindingFlags.NonPublic | BindingFlags.Instance);
            if (librariesField != null) {
                libraries = librariesField.GetValue(R.Plugins) as Dictionary<AssemblyName, string>;
            } else {
                libraries = new();
            }

            foreach (KeyValuePair<AssemblyName, string> library in libraries)
            {
                if (pluginFullNames.Contains(library.Key.FullName))
                {
                    continue;
                }

                FileInfo fileInfo = new(library.Value);
                LibraryInfo libraryInfo = new()
                {
                    Name = library.Key.Name,
                    Version = library.Key.Version.ToString(),
                    DirectoryPath = fileInfo.Directory.FullName,
                    FileName = fileInfo.Name
                };
                rocketInfo.Libraries.Add(libraryInfo);
            }

            return rocketInfo;
        }
    }
}

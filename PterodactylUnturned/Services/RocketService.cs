using RestoreMonarchy.PterodactylUnturned.Models;
using Rocket.API;
using Rocket.Core;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using System;
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
                DirectoryPath = rocketDirectory.TrimEnd('/'),
                Libraries = new(),
                Plugins = new()
            };

            List<IRocketPlugin> plugins = R.Plugins.GetPlugins();
            List<string> pluginFullNames = new();
            foreach (IRocketPlugin plugin in plugins)
            {
                string pluginName;
                string version;
                string pluginState;

                bool hasConfiguration;
                bool hasTranslations;
                
                try
                {
                    pluginName = plugin.Name;
                    Assembly assembly = plugin.GetType().Assembly;
                    AssemblyName assemblyName = assembly.GetName();
                    version = assemblyName.Version.ToString();
                    pluginState = plugin.State.ToString();

                    hasConfiguration = plugin.GetType().GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRocketPlugin<>));
                    hasTranslations = plugin.DefaultTranslations?.Any() ?? false;

                    pluginFullNames.Add(assemblyName.FullName);
                } catch (Exception)
                {
                    // Sometimes when dll is replaced AssemblyName throws random exceptions
                    continue;
                }
                

                string pluginDirectory = Path.Combine(pluginsDirectory, pluginName);
                string configurationFileName = string.Format(Rocket.Core.Environment.PluginConfigurationFileTemplate, pluginName);
                string translationsFileName = string.Format(Rocket.Core.Environment.PluginTranslationFileTemplate, pluginName, R.Settings.Instance.LanguageCode);
    
                PluginInfo pluginInfo = new()
                {
                    Name = pluginName,
                    Version = version,
                    DirectoryPath = pluginDirectory,
                    TranslationsFileName = hasTranslations ? translationsFileName : null,
                    ConfigurationFileName = hasConfiguration ? configurationFileName : null,
                    State = pluginState
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
                string fullName;
                string name;
                string version;

                try
                {
                    fullName = library.Key.FullName;
                    name = library.Key.Name;
                    version = library.Key.Version.ToString();
                } catch (Exception)
                {
                    // Sometimes when dll is replaced AssemblyName throws random exceptions
                    continue;
                }
                

                if (pluginFullNames.Contains(fullName))
                {
                    continue;
                }

                FileInfo fileInfo = new(library.Value);

                if (!fileInfo.Exists)
                {
                    continue;
                }

                string directoryPath = fileInfo.DirectoryName;
                if (directoryPath.StartsWith("/home/container/"))
                {
                    directoryPath = directoryPath.Substring("/home/container/".Length);
                }
                LibraryInfo libraryInfo = new()
                {
                    Name = name,
                    Version = version,
                    DirectoryPath = directoryPath,
                    FileName = fileInfo.Name
                };
                rocketInfo.Libraries.Add(libraryInfo);
            }

            return rocketInfo;
        }
    }
}

using Newtonsoft.Json;
using RestoreMonarchy.PterodactylUnturned.Services;
using SDG.Framework.Modules;
using SDG.Unturned;
using System;
using System.IO;
using UnityEngine;

namespace RestoreMonarchy.PterodactylUnturned
{
    public class PterodactylUnturnedModule : IModuleNexus
    {
        public static PterodactylUnturnedConfig Config { get; private set; }

        public static string Directory => Path.Combine(UnturnedPaths.RootDirectory.FullName, "PterodactylAPI");

        internal static void EnsureDirectoryExists()
        {
            if (!System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.CreateDirectory(Directory);
            }
        }

        private GameObject gameObject;

        public void initialize()
        {   
            try
            {
                FileInfo fileInfo = new(typeof(PterodactylUnturnedModule).Assembly.Location);
                string configPath = Path.Combine(fileInfo.Directory.FullName, "config.json");

                if (!File.Exists(configPath))
                {
                    Config = new();
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
                }

                string configJson = File.ReadAllText(configPath);
                Config = JsonConvert.DeserializeObject<PterodactylUnturnedConfig>(configJson);
            } catch (Exception exception)
            {
                Logs.printLine($"Failed to load Pterodactyl Unturned config: {exception.Message}");
                Config = new();
            }

            if (Config.AutomaticallyEnableFakeIP && !Provider.configData.Server.Use_FakeIP)
            {
                Provider.configData.Server.Use_FakeIP = true;
                Logs.printLine("Automatically enabled FakeIP");
            }

            ConfigDataService.GenerateConfigSchema();

            

            gameObject = new GameObject();
            gameObject.AddComponent<ServerInfoService>();
        }

        public void shutdown()
        {
            GameObject.Destroy(gameObject);
        }
    }
}
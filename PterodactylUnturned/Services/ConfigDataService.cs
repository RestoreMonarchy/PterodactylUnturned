using Newtonsoft.Json.Schema;
using RestoreMonarchy.PterodactylUnturned.Helpers;
using SDG.Unturned;
using System;
using System.IO;

namespace RestoreMonarchy.PterodactylUnturned.Services
{
    public static class ConfigDataService
    {
        private static string ConfigSchemaPath => Path.Combine(PterodactylUnturnedModule.Directory, "config-schema.json");

        public static void GenerateConfigSchema()
        {
            try
            {
                ConfigData configData = ConfigData.CreateDefault(false);

                string xmlPath = Path.Combine(UnturnedPaths.RootDirectory.FullName, "Unturned_Data", "Managed", "Assembly-CSharp.xml");
                CustomSchemaGenerator generator = new(xmlPath);

                // Generate the schema
                string schema = generator.GenerateSchema(configData);

                // Save the schema to file
                PterodactylUnturnedModule.EnsureDirectoryExists();
                File.WriteAllText(ConfigSchemaPath, schema);
            }
            catch (Exception exception)
            {
                Logs.printLine($"Failed to generate config schema: {exception.Message}");
                Logs.printLine(exception.StackTrace);
            }
        }
    }
}

using Newtonsoft.Json;
using RestoreMonarchy.PterodactylUnturned.Helpers;
using RestoreMonarchy.PterodactylUnturned.Models;
using SDG.Unturned;
using Steamworks;
using System;
using System.IO;
using UnityEngine;

namespace RestoreMonarchy.PterodactylUnturned.Services
{
    public class ServerInfoService : MonoBehaviour
    {
        private string Directory => Path.Combine(UnturnedPaths.RootDirectory.FullName, "PterodactylAPI");
        private string ServerInfoPath => Path.Combine(Directory, "server.json");

        private float UpdateInterval => PterodactylUnturnedModule.Config.UpdateInterval;

        void Awake() 
        {
            DontDestroyOnLoad(this.gameObject);
        }

        void Start()
        {
            InvokeRepeating(nameof(UpdateServerInfo), 0, UpdateInterval);
        }

        internal void UpdateServerInfo()
        {
            ulong steamId;
            try
            {
                steamId = SteamGameServer.GetSteamID().m_SteamID;
            } catch (InvalidOperationException)
            {
                steamId = 0;
            }
            
            bool isRocketEnabled = ModuleHelper.IsModuleEnabled("Rocket.Unturned");
            bool isRocketReady = false;
            if (isRocketEnabled)
            {
                isRocketReady = RocketService.IsRocketReady();
            }

            ServerInfo serverInfo = new()
            {
                SteamId = steamId.ToString(),
                Name = Provider.serverName,
                Players = Provider.clients.Count,
                PendingPlayers = Provider.pending.Count,
                MaxPlayers = Provider.maxPlayers,
                Map = Level.info?.name ?? Provider.map,
                ThumbnailUrl = Provider.configData.Browser.Thumbnail,
                LastUpdate = DateTime.UtcNow,
                NextUpdate = DateTime.UtcNow.AddSeconds(UpdateInterval),
                PlayerList = new(),
                IsRocketEnabled = isRocketEnabled,
                IsFakeIPEnabled = Provider.configData.Server.Use_FakeIP,
                RocketInfo = isRocketReady ? RocketService.GetRocketInfo() : null
            };

            foreach (SteamPlayer player in Provider.clients)
            {
                PlayerInfo playerInfo = new()
                {
                    SteamId = player.playerID.steamID.ToString(),
                    SteamName = player.playerID.playerName,
                    CharacterName = player.playerID.characterName,
                    GroupId = player.player.quests.groupID.ToString(),
                    Ping = (int)(player.ping * 1000),
                    Playtime = (int)(Time.realtimeSinceStartup - player.joined),
                    IsGold = player.isPro,
                    IsAdmin = player.isAdmin,
                    SkinColor = ColorUtility.ToHtmlStringRGB(player.skin),
                    Face = player.face,
                    Health = player.player.life.health
                };

                serverInfo.PlayerList.Add(playerInfo);
            }

            if (!System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.CreateDirectory(Directory);
            }

            string serverJson = JsonConvert.SerializeObject(serverInfo, Formatting.Indented);
            File.WriteAllText(ServerInfoPath, serverJson);
        }
    }
}

using Newtonsoft.Json;
using RestoreMonarchy.PterodactylUnturned.Models;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RestoreMonarchy.PterodactylUnturned.Services
{
    public class ServerInfoService : MonoBehaviour
    {
        private string Directory => Path.Combine(UnturnedPaths.RootDirectory.FullName, "PterodactylAPI");
        private string ServerInfoPath => Path.Combine(Directory, "server.json");
        private string PlayersInfoPath => Path.Combine(Directory, "players.json");

        private const float UpdateInterval = 2;

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
            
            ServerInfo serverInfo = new()
            {
                SteamId = steamId,
                Name = Provider.serverName,
                Players = Provider.clients.Count,
                PendingPlayers = Provider.pending.Count,
                MaxPlayers = Provider.maxPlayers,
                Map = Level.info?.name ?? Provider.map,
                ThumbnailUrl = Provider.configData.Browser.Thumbnail,
                LastUpdate = DateTime.UtcNow,
                NextUpdate = DateTime.UtcNow.AddSeconds(UpdateInterval)
            };

            List<PlayerInfo> players = new();
            foreach (SteamPlayer player in Provider.clients)
            {
                PlayerInfo playerInfo = new()
                {
                    SteamId = player.playerID.steamID.m_SteamID,
                    SteamName = player.playerID.playerName,
                    CharacterName = player.playerID.characterName,
                    GroupId = player.player.quests.groupID.m_SteamID,
                    Ping = (int)(player.ping * 1000),
                    Playtime = (int)(Time.realtimeSinceStartup - player.joined)
                };

                players.Add(playerInfo);
            }

            if (!System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.CreateDirectory(Directory);
            }

            string serverJson = JsonConvert.SerializeObject(serverInfo, Formatting.Indented);
            File.WriteAllText(ServerInfoPath, serverJson);

            string playersJson = JsonConvert.SerializeObject(players, Formatting.Indented);
            File.WriteAllText(PlayersInfoPath, playersJson);
        }
    }
}

﻿using System;
using System.Collections.Generic;

namespace RestoreMonarchy.PterodactylUnturned.Models
{
    public class ServerInfo
    {
        public string SteamId { get; set; }
        public string Name { get; set; }
        public int Players { get; set; }
        public int PendingPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string Map { get; set; }
        public string Mode { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool IsRocketEnabled { get; set; }
        public bool IsFakeIPEnabled { get; set; }
        public RocketInfo RocketInfo { get; set; }
        public List<ulong> WorkshopFileIds { get; set; }

        public DateTime LastUpdate { get; set; }
        public DateTime NextUpdate { get; set; }

        public List<PlayerInfo> PlayerList { get; set; }
    }
}
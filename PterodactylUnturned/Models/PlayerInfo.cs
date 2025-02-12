namespace RestoreMonarchy.PterodactylUnturned.Models
{
    public class PlayerInfo
    {
        public ulong SteamId { get; set; }
        public ulong GroupId { get; set; }
        public string CharacterName { get; set; }
        public string SteamName { get; set; }
        public int Playtime { get; set; }
        public int Ping { get; set; }
    }
}

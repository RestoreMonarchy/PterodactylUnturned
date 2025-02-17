namespace RestoreMonarchy.PterodactylUnturned.Models
{
    public class PlayerInfo
    {
        public string SteamId { get; set; }
        public string GroupId { get; set; }
        public string CharacterName { get; set; }
        public string SteamName { get; set; }
        public int Playtime { get; set; }
        public int Ping { get; set; }
        public bool IsGold { get; set; }
        public bool IsAdmin { get; set; }
        public string SkinColor { get; set; }
        public int Face { get; set; }
    }
}

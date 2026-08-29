namespace TornWarTracker.Models
{
    public class UserInfo
    {
        public int PlayerId { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public string Rank { get; set; } = "";  // Title like "Champion Nudist"
        public int FactionId { get; set; }
        public string FactionName { get; set; } = "";
    }
}

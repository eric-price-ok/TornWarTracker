namespace TornWarTracker.Models
{
    /// <summary>
    /// Torn's "status" object on a faction member. State is things like
    /// Okay / Hospital / Jail / Traveling / Abroad / Federal. Until is a
    /// Unix timestamp (0 when not applicable) - e.g. hospital release time.
    /// </summary>
    public class MemberStatus
    {
        public string Description { get; set; } = "";
        public string Details { get; set; } = "";
        public string State { get; set; } = "";
        public string Color { get; set; } = "";
        public long Until { get; set; }
    }
}

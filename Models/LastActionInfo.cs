namespace TornWarTracker.Models
{
    /// <summary>
    /// Torn's "last_action" object on a faction member: Online / Idle / Offline,
    /// plus when that was and a human-readable relative string ("54 minutes ago").
    /// </summary>
    public class LastActionInfo
    {
        public string Status { get; set; } = "";
        public long Timestamp { get; set; }
        public string Relative { get; set; } = "";
    }
}

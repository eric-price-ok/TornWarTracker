namespace TornWarTracker.Models
{
    /// <summary>
    /// Everything that gets saved to %AppData%\TornWarTracker\config.json.
    /// Plain-text local file per your call - not encrypted.
    /// </summary>
    public class AppConfig
    {
        public string TornApiKey { get; set; } = "";
        public string FfScouterApiKey { get; set; } = "";
        public int MyFactionId { get; set; } = 0;
        public int PollIntervalSeconds { get; set; } = 20;

        // Filters
        public bool ExcludeTraveling { get; set; } = false;
        public bool ExcludeAbroad { get; set; } = false;
        public bool ExcludeOnline { get; set; } = false;

        public int? MinLevel { get; set; }
        public int? MaxLevel { get; set; }
        public long? MinStatEstimate { get; set; }
        public long? MaxStatEstimate { get; set; }

        public bool ClaimerMaxFfEnabled { get; set; } = true;
        public double ClaimerMaxFf { get; set; } = 3.0;
    }
}

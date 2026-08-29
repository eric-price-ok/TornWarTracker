namespace TornWarTracker.Models
{
    /// <summary>
    /// The merged battle-stat estimate FFScouter returns for one player
    /// (from their get-stats endpoint).
    /// </summary>
    public class FfStatEstimate
    {
        public int PlayerId { get; set; }
        public double? FairFight { get; set; }
        public long? BsEstimate { get; set; }
        public string? BsEstimateHuman { get; set; }
        public long? BssPublic { get; set; }
        public long? LastUpdated { get; set; }
        public string? Source { get; set; }
    }
}

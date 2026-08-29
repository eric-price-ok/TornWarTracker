using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TornWarTracker.Models;

namespace TornWarTracker.Services
{
    /// <summary>
    /// Client for FFScouter's get-stats endpoint (confirmed against their
    /// published API docs at https://ffscouter.com/api-docs). Batches
    /// requests at 205 IDs per call, their documented maximum.
    /// </summary>
    public class FfScouterClient
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "https://ffscouter.com/api/v1";
        private const int MaxTargetsPerCall = 205;

        public FfScouterClient(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
        }

        public async Task<Dictionary<int, FfStatEstimate>> GetStatsAsync(string apiKey, IEnumerable<int> playerIds)
        {
            var result = new Dictionary<int, FfStatEstimate>();
            var ids = playerIds.Distinct().ToList();

            for (int i = 0; i < ids.Count; i += MaxTargetsPerCall)
            {
                var batch = ids.Skip(i).Take(MaxTargetsPerCall);
                var targets = string.Join(",", batch);
                var url = $"{BaseUrl}/get-stats?key={apiKey}&targets={targets}";

                using var response = await _http.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var entries = await response.Content.ReadFromJsonAsync<List<FfStatsResponseEntry>>();
                if (entries == null) continue;

                foreach (var e in entries)
                {
                    result[e.PlayerId] = new FfStatEstimate
                    {
                        PlayerId = e.PlayerId,
                        FairFight = e.FairFight,
                        BsEstimate = e.BsEstimate,
                        BsEstimateHuman = e.BsEstimateHuman,
                        BssPublic = e.BssPublic,
                        LastUpdated = e.LastUpdated,
                        Source = e.Source,
                    };
                }
            }

            return result;
        }

        private class FfStatsResponseEntry
        {
            [JsonPropertyName("player_id")]
            public int PlayerId { get; set; }

            [JsonPropertyName("fair_fight")]
            public double? FairFight { get; set; }

            [JsonPropertyName("bs_estimate")]
            public long? BsEstimate { get; set; }

            [JsonPropertyName("bs_estimate_human")]
            public string? BsEstimateHuman { get; set; }

            [JsonPropertyName("bss_public")]
            public long? BssPublic { get; set; }

            [JsonPropertyName("last_updated")]
            public long? LastUpdated { get; set; }

            [JsonPropertyName("source")]
            public string? Source { get; set; }
        }
    }
}

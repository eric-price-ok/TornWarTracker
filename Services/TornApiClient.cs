using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TornWarTracker.Models;

namespace TornWarTracker.Services
{
    /// <summary>
    /// Thin client for the Torn v2 API - only the two endpoints this app needs.
    ///
    /// IMPORTANT: The JSON field names used in ParseActiveWarOpponent() and
    /// ParseMembers() below are based on Torn's documented v1/v2 conventions
    /// (status.state, status.until, last_action.status, rankedwars[].factions[].id,
    /// etc). I was not able to make a live authenticated call while building this,
    /// so treat these as "should be right" rather than confirmed. If a real
    /// response comes back shaped differently, the fix is isolated to these two
    /// parse methods - hit the endpoints directly (e.g. via
    /// https://www.torn.com/swagger.php with your key) and adjust the property
    /// names to match.
    /// </summary>
    public class TornApiClient
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "https://api.torn.com/v2";

        public TornApiClient(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Fetches the current user's info (name, level, rank/title, faction).
        /// Uses v1 API with basic,profile selections.
        /// </summary>
        public async Task<UserInfo?> GetUserInfoAsync(string apiKey)
        {
            var url = $"https://api.torn.com/user/?selections=basic,profile&key={apiKey}";
            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Check for API error
            if (root.TryGetProperty("error", out _))
                return null;

            var info = new UserInfo
            {
                PlayerId = root.TryGetProperty("player_id", out var pid) ? pid.GetInt32() : 0,
                Name = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Level = root.TryGetProperty("level", out var lvl) ? lvl.GetInt32() : 0,
                Rank = root.TryGetProperty("rank", out var rank) ? rank.GetString() ?? "" : "",
            };

            if (root.TryGetProperty("faction", out var faction))
            {
                info.FactionId = faction.TryGetProperty("faction_id", out var fid) ? fid.GetInt32() : 0;
                info.FactionName = faction.TryGetProperty("faction_name", out var fname) ? fname.GetString() ?? "" : "";
            }

            return info;
        }

        /// <summary>
        /// Looks at your own faction's ranked wars and returns the opponent
        /// faction's ID if a war is currently active, or null if there isn't one.
        /// </summary>
        public async Task<int?> GetActiveWarOpponentFactionIdAsync(int myFactionId, string apiKey)
        {
            var url = $"{BaseUrl}/faction/{myFactionId}/wars?key={apiKey}";
            using var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            return ParseActiveWarOpponent(doc.RootElement, myFactionId);
        }

        internal static int? ParseActiveWarOpponent(JsonElement root, int myFactionId)
        {
            // API returns: { "wars": { "ranked": { "factions": [...], "end": null|timestamp } } }
            if (!root.TryGetProperty("wars", out var wars))
                return null;

            if (!wars.TryGetProperty("ranked", out var ranked) ||
                ranked.ValueKind != JsonValueKind.Object)
                return null;

            // Skip if war has ended (end is a non-null, non-zero timestamp)
            if (ranked.TryGetProperty("end", out var endEl) &&
                endEl.ValueKind == JsonValueKind.Number &&
                endEl.GetInt64() != 0)
            {
                return null;
            }

            if (!ranked.TryGetProperty("factions", out var factions) ||
                factions.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var faction in factions.EnumerateArray())
            {
                if (faction.TryGetProperty("id", out var idEl) &&
                    idEl.ValueKind == JsonValueKind.Number &&
                    idEl.GetInt32() != myFactionId)
                {
                    return idEl.GetInt32();
                }
            }

            return null;
        }

        public async Task<List<FactionMember>> GetFactionMembersAsync(int factionId, string apiKey)
        {
            var url = $"{BaseUrl}/faction/{factionId}/members?key={apiKey}";
            using var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            return ParseMembers(doc.RootElement);
        }

        internal static List<FactionMember> ParseMembers(JsonElement root)
        {
            var result = new List<FactionMember>();

            if (!root.TryGetProperty("members", out var membersEl))
                return result;

            // Handles the typical v2 "array of member objects" shape, and
            // defensively also a v1-style "object keyed by player id" shape,
            // in case the live response looks like the latter.
            if (membersEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var m in membersEl.EnumerateArray())
                    result.Add(ParseOneMember(m));
            }
            else if (membersEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in membersEl.EnumerateObject())
                {
                    var member = ParseOneMember(prop.Value);
                    if (member.Id == 0 && int.TryParse(prop.Name, out var idFromKey))
                        member.Id = idFromKey;
                    result.Add(member);
                }
            }

            return result;
        }

        private static FactionMember ParseOneMember(JsonElement m)
        {
            var member = new FactionMember
            {
                Id = m.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number ? idEl.GetInt32() : 0,
                Name = m.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "",
                Level = m.TryGetProperty("level", out var lvlEl) && lvlEl.ValueKind == JsonValueKind.Number ? lvlEl.GetInt32() : 0,
            };

            if (m.TryGetProperty("last_action", out var lastAction))
            {
                member.LastAction = new LastActionInfo
                {
                    Status = lastAction.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "",
                    Timestamp = lastAction.TryGetProperty("timestamp", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt64() : 0,
                    Relative = lastAction.TryGetProperty("relative", out var r) ? r.GetString() ?? "" : "",
                };
            }

            if (m.TryGetProperty("status", out var status))
            {
                member.Status = new MemberStatus
                {
                    Description = status.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    Details = status.TryGetProperty("details", out var det) ? det.GetString() ?? "" : "",
                    State = status.TryGetProperty("state", out var st) ? st.GetString() ?? "" : "",
                    Color = status.TryGetProperty("color", out var c) ? c.GetString() ?? "" : "",
                    Until = status.TryGetProperty("until", out var u) && u.ValueKind == JsonValueKind.Number ? u.GetInt64() : 0,
                };
            }

            return member;
        }
    }
}

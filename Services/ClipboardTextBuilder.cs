using System;
using TornWarTracker.Models;

namespace TornWarTracker.Services
{
    /// <summary>
    /// Builds the two clipboard strings: the "Caller" version (stats + link,
    /// for calling a target in faction chat) and the "Claim" version
    /// (no stats, no link, no FF - just who's being hit and when).
    /// </summary>
    public static class ClipboardTextBuilder
    {
        public static string BuildCallerText(FactionMember member, FfStatEstimate? estimate)
        {
            var statPart = estimate?.BsEstimateHuman is string h ? $"Est. {h}" : "Est. unknown";
            var statusPart = BuildStatusPhrase(member);
            var url = $"https://www.torn.com/profiles.php?XID={member.Id}";
            return $"{member.Name} L{member.Level} — {statPart} — {statusPart} — {url}";
        }

        public static string BuildClaimText(FactionMember member)
        {
            if (IsHospitalized(member, out var minutesRemaining))
                return $"Claiming {member.Name} L{member.Level} in {minutesRemaining}min";

            return $"Attacking {member.Name} L{member.Level} now";
        }

        /// <summary>
        /// True while the member's status is Hospital and the release time
        /// is still in the future. minutesRemaining is rounded up so "45
        /// seconds left" reads as "1min" rather than "0min".
        /// </summary>
        public static bool IsHospitalized(FactionMember member, out int minutesRemaining)
        {
            minutesRemaining = 0;

            if (!string.Equals(member.Status.State, "Hospital", StringComparison.OrdinalIgnoreCase))
                return false;

            var until = DateTimeOffset.FromUnixTimeSeconds(member.Status.Until);
            var remaining = until - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
                return false;

            minutesRemaining = (int)Math.Ceiling(remaining.TotalMinutes);
            return true;
        }

        private static string BuildStatusPhrase(FactionMember member)
        {
            if (IsHospitalized(member, out var minutesRemaining))
                return $"Hosp, out in {minutesRemaining}m";

            return string.IsNullOrEmpty(member.Status.State) ? "Unknown" : member.Status.State;
        }
    }
}

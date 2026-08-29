using System;
using TornWarTracker.Models;
using TornWarTracker.Services;
using TornWarTracker.Utilities;

namespace TornWarTracker.ViewModels
{
    /// <summary>
    /// One row in the grid. Wraps a FactionMember + its FFScouter estimate
    /// and exposes display-ready properties. RefreshCountdown() is called
    /// once a second by MainViewModel so the hospital timer ticks live
    /// without waiting for the next poll.
    /// </summary>
    public class MemberRowViewModel : ObservableObject
    {
        public FactionMember Member { get; private set; }
        public FfStatEstimate? Estimate { get; private set; }

        public MemberRowViewModel(FactionMember member, FfStatEstimate? estimate)
        {
            Member = member;
            Estimate = estimate;
        }

        /// <summary>Called each poll cycle - this is what re-syncs a live countdown
        /// if the member got healed/revived between polls.</summary>
        public void UpdateData(FactionMember member, FfStatEstimate? estimate)
        {
            Member = member;
            Estimate = estimate;
            RaiseAll();
        }

        public int Id => Member.Id;
        public string Name => Member.Name;
        public int Level => Member.Level;
        public double? FairFight => Estimate?.FairFight;
        public string StatEstimateDisplay => Estimate?.BsEstimateHuman ?? "—";
        public long? StatEstimateRaw => Estimate?.BsEstimate;

        public string StatusDisplay
        {
            get
            {
                if (ClipboardTextBuilder.IsHospitalized(Member, out var mins))
                    return $"Hospital - out in {mins}m";
                return string.IsNullOrEmpty(Member.Status.State) ? "Unknown" : Member.Status.State;
            }
        }

        public bool IsHospitalized => ClipboardTextBuilder.IsHospitalized(Member, out _);

        /// <summary>
        /// Minutes remaining in hospital, or int.MaxValue if not hospitalized.
        /// Used for proper numeric sorting of hospital times.
        /// </summary>
        public int HospitalMinutesRemaining
        {
            get
            {
                if (ClipboardTextBuilder.IsHospitalized(Member, out var mins))
                    return mins;
                return int.MaxValue; // Not hospitalized sorts to end
            }
        }
        public bool IsTraveling => string.Equals(Member.Status.State, "Traveling", StringComparison.OrdinalIgnoreCase);
        public bool IsAbroad => string.Equals(Member.Status.State, "Abroad", StringComparison.OrdinalIgnoreCase);
        public bool IsOnline => string.Equals(Member.LastAction.Status, "Online", StringComparison.OrdinalIgnoreCase);

        public string LastActionDisplay => Member.LastAction.Relative;

        public string CallerClipboardText => ClipboardTextBuilder.BuildCallerText(Member, Estimate);
        public string ClaimClipboardText => ClipboardTextBuilder.BuildClaimText(Member);

        /// <summary>Ticked every second by MainViewModel's countdown timer.</summary>
        public void RefreshCountdown()
        {
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(IsHospitalized));
            OnPropertyChanged(nameof(CallerClipboardText));
            OnPropertyChanged(nameof(ClaimClipboardText));
        }

        private void RaiseAll()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Level));
            OnPropertyChanged(nameof(FairFight));
            OnPropertyChanged(nameof(StatEstimateDisplay));
            OnPropertyChanged(nameof(StatEstimateRaw));
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(IsHospitalized));
            OnPropertyChanged(nameof(IsTraveling));
            OnPropertyChanged(nameof(IsAbroad));
            OnPropertyChanged(nameof(IsOnline));
            OnPropertyChanged(nameof(LastActionDisplay));
            OnPropertyChanged(nameof(CallerClipboardText));
            OnPropertyChanged(nameof(ClaimClipboardText));
        }
    }
}

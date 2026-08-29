using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using TornWarTracker.Models;
using TornWarTracker.Services;
using TornWarTracker.Utilities;

namespace TornWarTracker.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly ConfigService _configService;
        private readonly DatabaseService _databaseService;
        private readonly TornApiClient _tornApi;
        private readonly FfScouterClient _ffScouter;

        private readonly DispatcherTimer _pollTimer = new();
        private readonly DispatcherTimer _countdownTimer = new();

        private readonly Dictionary<int, MemberRowViewModel> _rowsById = new();

        public ObservableCollection<MemberRowViewModel> Members { get; } = new();
        public ICollectionView MembersView { get; }

        public AppConfig Config { get; }

        private string _statusMessage = "Not started.";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        // Player info display
        private string _playerName = "";
        public string PlayerName
        {
            get => _playerName;
            set => SetField(ref _playerName, value);
        }

        private int _playerLevel;
        public int PlayerLevel
        {
            get => _playerLevel;
            set => SetField(ref _playerLevel, value);
        }

        private string _playerTitle = "";
        public string PlayerTitle
        {
            get => _playerTitle;
            set => SetField(ref _playerTitle, value);
        }

        private string _factionName = "";
        public string FactionName
        {
            get => _factionName;
            set => SetField(ref _factionName, value);
        }

        private bool _playerInfoLoaded;
        public bool PlayerInfoLoaded
        {
            get => _playerInfoLoaded;
            set => SetField(ref _playerInfoLoaded, value);
        }

        private int? _enemyFactionId;
        public int? EnemyFactionId
        {
            get => _enemyFactionId;
            set => SetField(ref _enemyFactionId, value);
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set => SetField(ref _isRunning, value);
        }

        // --- Settings bindings (read/write straight through to Config) ---

        public string TornApiKey
        {
            get => Config.TornApiKey;
            set { Config.TornApiKey = value; OnPropertyChanged(); }
        }

        public string FfScouterApiKey
        {
            get => Config.FfScouterApiKey;
            set { Config.FfScouterApiKey = value; OnPropertyChanged(); }
        }

        public int MyFactionId
        {
            get => Config.MyFactionId;
            set { Config.MyFactionId = value; OnPropertyChanged(); }
        }

        public int PollIntervalSeconds
        {
            get => Config.PollIntervalSeconds;
            set
            {
                Config.PollIntervalSeconds = value;
                OnPropertyChanged();
                _pollTimer.Interval = TimeSpan.FromSeconds(Math.Max(5, value));
            }
        }

        // --- Filters ---

        public bool ExcludeTraveling
        {
            get => Config.ExcludeTraveling;
            set { Config.ExcludeTraveling = value; OnPropertyChanged(); MembersView.Refresh(); }
        }

        public bool ExcludeAbroad
        {
            get => Config.ExcludeAbroad;
            set { Config.ExcludeAbroad = value; OnPropertyChanged(); MembersView.Refresh(); }
        }

        public bool ExcludeOnline
        {
            get => Config.ExcludeOnline;
            set { Config.ExcludeOnline = value; OnPropertyChanged(); MembersView.Refresh(); }
        }

        public int? MinLevel
        {
            get => Config.MinLevel;
            set { Config.MinLevel = value; OnPropertyChanged(); MembersView.Refresh(); }
        }

        public int? MaxLevel
        {
            get => Config.MaxLevel;
            set { Config.MaxLevel = value; OnPropertyChanged(); MembersView.Refresh(); }
        }

        public long? MinStatEstimate
        {
            get => Config.MinStatEstimate;
            set { Config.MinStatEstimate = value; OnPropertyChanged(); MembersView.Refresh(); }
        }

        public long? MaxStatEstimate
        {
            get => Config.MaxStatEstimate;
            set { Config.MaxStatEstimate = value; OnPropertyChanged(); MembersView.Refresh(); }
        }

        public bool ClaimerMaxFfEnabled
        {
            get => Config.ClaimerMaxFfEnabled;
            set { Config.ClaimerMaxFfEnabled = value; OnPropertyChanged(); MembersView.Refresh(); }
        }

        public double ClaimerMaxFf
        {
            get => Config.ClaimerMaxFf;
            set { Config.ClaimerMaxFf = value; OnPropertyChanged(); MembersView.Refresh(); }
        }

        // --- Commands ---

        public RelayCommand SaveSettingsCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand ValidateApiKeyCommand { get; }
        public RelayCommand<MemberRowViewModel> CopyCallerCommand { get; }
        public RelayCommand<MemberRowViewModel> CopyClaimCommand { get; }

        public MainViewModel()
        {
            _configService = new ConfigService();
            _databaseService = new DatabaseService();

            var httpClient = new HttpClient();
            _tornApi = new TornApiClient(httpClient);
            _ffScouter = new FfScouterClient(httpClient);

            Config = _configService.Load();

            MembersView = CollectionViewSource.GetDefaultView(Members);
            MembersView.Filter = FilterPredicate;

            SaveSettingsCommand = new RelayCommand(_ => _configService.Save(Config));
            StartCommand = new RelayCommand(_ => Start(), _ => !IsRunning && HasRequiredSettings());
            StopCommand = new RelayCommand(_ => Stop(), _ => IsRunning);
            ValidateApiKeyCommand = new RelayCommand(_ => _ = ValidateApiKeyAsync(), _ => !string.IsNullOrWhiteSpace(Config.TornApiKey));
            CopyCallerCommand = new RelayCommand<MemberRowViewModel>(row => CopyToClipboard(row?.CallerClipboardText));
            CopyClaimCommand = new RelayCommand<MemberRowViewModel>(row => CopyToClipboard(row?.ClaimClipboardText));

            _pollTimer.Interval = TimeSpan.FromSeconds(Math.Max(5, Config.PollIntervalSeconds));
            _pollTimer.Tick += async (_, _) => await PollAsync();

            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += (_, _) => TickCountdowns();
        }

        private bool HasRequiredSettings() =>
            !string.IsNullOrWhiteSpace(Config.TornApiKey) &&
            !string.IsNullOrWhiteSpace(Config.FfScouterApiKey) &&
            Config.MyFactionId > 0;

        private async Task ValidateApiKeyAsync()
        {
            if (string.IsNullOrWhiteSpace(Config.TornApiKey))
            {
                StatusMessage = "Please enter your Torn API key.";
                return;
            }

            StatusMessage = "Validating API key...";
            try
            {
                var userInfo = await _tornApi.GetUserInfoAsync(Config.TornApiKey);
                if (userInfo == null)
                {
                    StatusMessage = "Invalid API key or API error.";
                    PlayerInfoLoaded = false;
                    return;
                }

                PlayerName = userInfo.Name;
                PlayerLevel = userInfo.Level;
                PlayerTitle = userInfo.Rank;
                FactionName = userInfo.FactionName;
                PlayerInfoLoaded = true;

                // Auto-fill faction ID
                if (userInfo.FactionId > 0)
                {
                    MyFactionId = userInfo.FactionId;
                }

                StatusMessage = $"Welcome, {userInfo.Name}! Faction ID auto-filled.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error validating API key: {ex.Message}";
                PlayerInfoLoaded = false;
            }
        }

        private void Start()
        {
            _configService.Save(Config);
            IsRunning = true;
            StatusMessage = "Looking for an active ranked war...";
            _pollTimer.Start();
            _countdownTimer.Start();
            _ = PollAsync(); // don't wait for the first timer tick
        }

        private void Stop()
        {
            IsRunning = false;
            _pollTimer.Stop();
            _countdownTimer.Stop();
            StatusMessage = "Stopped.";
        }

        private async Task PollAsync()
        {
            try
            {
                if (EnemyFactionId is null)
                {
                    var opponent = await _tornApi.GetActiveWarOpponentFactionIdAsync(Config.MyFactionId, Config.TornApiKey);
                    if (opponent is null)
                    {
                        StatusMessage = "No active ranked war found yet - will keep checking.";
                        return;
                    }
                    EnemyFactionId = opponent;
                }

                var members = await _tornApi.GetFactionMembersAsync(EnemyFactionId.Value, Config.TornApiKey);
                var estimates = await _ffScouter.GetStatsAsync(Config.FfScouterApiKey, members.Select(m => m.Id));

                MergeMembers(members, estimates);
                _databaseService.SaveSnapshot(EnemyFactionId.Value, members, estimates);

                StatusMessage = $"Last updated {DateTime.Now:T} - {members.Count} members.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void MergeMembers(List<FactionMember> members, Dictionary<int, FfStatEstimate> estimates)
        {
            var seenIds = new HashSet<int>();

            foreach (var member in members)
            {
                seenIds.Add(member.Id);
                estimates.TryGetValue(member.Id, out var est);

                if (_rowsById.TryGetValue(member.Id, out var row))
                {
                    row.UpdateData(member, est);
                }
                else
                {
                    var newRow = new MemberRowViewModel(member, est);
                    _rowsById[member.Id] = newRow;
                    Members.Add(newRow);
                }
            }

            // Anyone no longer in the roster (left the faction) drops off the grid.
            var stale = Members.Where(r => !seenIds.Contains(r.Id)).ToList();
            foreach (var row in stale)
            {
                Members.Remove(row);
                _rowsById.Remove(row.Id);
            }

            MembersView.Refresh();
        }

        private void TickCountdowns()
        {
            foreach (var row in Members)
                row.RefreshCountdown();

            // A hospital timer expiring can change whether a row still
            // passes the current filters, so keep the view honest every tick.
            MembersView.Refresh();
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not MemberRowViewModel row) return false;

            if (ExcludeTraveling && row.IsTraveling) return false;
            if (ExcludeAbroad && row.IsAbroad) return false;
            if (ExcludeOnline && row.IsOnline) return false;

            if (MinLevel.HasValue && row.Level < MinLevel.Value) return false;
            if (MaxLevel.HasValue && row.Level > MaxLevel.Value) return false;

            if (MinStatEstimate.HasValue && (row.StatEstimateRaw ?? 0) < MinStatEstimate.Value) return false;
            if (MaxStatEstimate.HasValue && row.StatEstimateRaw.HasValue && row.StatEstimateRaw.Value > MaxStatEstimate.Value) return false;

            if (ClaimerMaxFfEnabled && row.FairFight.HasValue && row.FairFight.Value > ClaimerMaxFf) return false;

            return true;
        }

        private static void CopyToClipboard(string? text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Clipboard.SetText(text);
        }
    }
}

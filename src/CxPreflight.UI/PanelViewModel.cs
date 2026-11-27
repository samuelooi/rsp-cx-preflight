using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CxPreflight.Core;

namespace CxPreflight.UI
{
    public sealed class SegmentVm : ISegment
    {
        public SegmentVm(string name, double weight, double fill)
        {
            Name = name;
            Weight = weight;
            Fill = fill;
        }

        public string Name { get; }
        public double Weight { get; }
        /// <summary>0..1, the share of the segment that is earned.</summary>
        public double Fill { get; }
        public string Tip => $"{Name} · {Weight:0} points";
    }

    public sealed class ItemVm
    {
        public ItemVm(string name, string score, string state)
        {
            Name = name;
            Score = score;
            State = state;
        }

        public string Name { get; }
        public string Score { get; }
        public string State { get; }
    }

    public sealed class GroupVm
    {
        public GroupVm(string name, string score, IReadOnlyList<ItemVm> items)
        {
            Name = name;
            Score = score;
            Items = items;
        }

        public string Name { get; }
        public string Score { get; }
        public IReadOnlyList<ItemVm> Items { get; }
    }

    public sealed class LevelVm
    {
        public LevelVm(LevelInfo l)
        {
            Name = l.Name;
            Elevation = (l.ElevationMm / 1000).ToString("+0.000;-0.000", CultureInfo.InvariantCulture);
            IsBuildingStory = l.IsBuildingStory;
        }

        public string Name { get; }
        public string Elevation { get; }
        public bool IsBuildingStory { get; }
        public string Mark => IsBuildingStory ? "Building Story" : "Not a storey";
    }

    /// <summary>
    /// State behind the sidebar. P0 measures levels and element count for real;
    /// the rest of the readiness items are listed with their weights and marked
    /// "not checked yet" until the P1 checks are ported from the pyRevit version.
    /// </summary>
    public sealed class PanelViewModel : ObservableObject
    {
        private readonly IPreflightHost _host;

        public PanelViewModel(IPreflightHost host)
        {
            _host = host;
            _host.ModelChanged += OnModelChanged;
            RunChecksCommand = new AsyncCommand(RunChecksAsync);
            DismissStaleCommand = new RelayCommand(() => IsStale = false);
            Segments = Empty.Groups.Select(g => new SegmentVm(g.Name, g.Weight, 0)).ToList();
            Groups = BuildGroups(null);
        }

        private static readonly FileReadiness Empty = ReadinessCalculator.Score(new ReadinessInputs());

        public AsyncCommand RunChecksCommand { get; }
        public RelayCommand DismissStaleCommand { get; }

        private string _documentTitle = "No model checked yet";
        public string DocumentTitle { get => _documentTitle; private set => Set(ref _documentTitle, value); }

        public string PercentText => "—";
        public string ScoringNote => "Scoring arrives in P1";

        public IReadOnlyList<SegmentVm> Segments { get; }

        private IReadOnlyList<GroupVm> _groups = Array.Empty<GroupVm>();
        public IReadOnlyList<GroupVm> Groups { get => _groups; private set => Set(ref _groups, value); }

        private IReadOnlyList<LevelVm> _levels = Array.Empty<LevelVm>();
        public IReadOnlyList<LevelVm> Levels { get => _levels; private set => Set(ref _levels, value); }

        private string _storeysSummary = "Run the checks to read the levels.";
        public string StoreysSummary { get => _storeysSummary; private set => Set(ref _storeysSummary, value); }

        private string _statusText = "Not checked yet";
        public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }

        private bool _isRunning;
        public bool IsRunning { get => _isRunning; private set => Set(ref _isRunning, value); }

        private bool _isStale;
        public bool IsStale { get => _isStale; set => Set(ref _isStale, value); }

        private string _staleText = "";
        public string StaleText { get => _staleText; private set => Set(ref _staleText, value); }

        private int _changedSinceRun;
        private bool _levelsChangedSinceRun;

        private async Task RunChecksAsync()
        {
            IsRunning = true;
            StatusText = "Checking…";
            try
            {
                ModelSummary? s = await _host.RunChecksAsync();
                if (s == null)
                {
                    StatusText = "Open a model to check it.";
                    return;
                }

                DocumentTitle = s.DocumentTitle;
                Levels = s.Levels.OrderBy(l => l.ElevationMm).Select(l => new LevelVm(l)).ToList();
                int storeys = s.Levels.Count(l => l.IsBuildingStory);
                StoreysSummary = storeys == 0
                    ? $"No level is set as Building Story ({s.Levels.Count} levels). The IFC would have no storeys."
                    : $"{storeys} of {s.Levels.Count} levels are set as Building Story.";
                Groups = BuildGroups(storeys > 0);
                StatusText = string.Format(CultureInfo.InvariantCulture,
                    "Checked {0:HH:mm} · {1:N0} elements · {2:N1} s", s.CheckedAt, s.ElementCount, s.DurationMs / 1000.0);

                _changedSinceRun = 0;
                _levelsChangedSinceRun = false;
                IsStale = false;
            }
            catch (Exception ex)
            {
                StatusText = "The check failed: " + ex.Message;
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void OnModelChanged(object? sender, ModelChangedEventArgs e)
        {
            _changedSinceRun += e.ChangedElements;
            _levelsChangedSinceRun |= e.LevelsChanged;
            StaleText = string.Format(CultureInfo.InvariantCulture,
                "{0:N0} elements edited since the last check{1}. Results may be out of date.",
                _changedSinceRun, _levelsChangedSinceRun ? ", including levels" : "");
            IsStale = true;
        }

        /// <summary>All items with their agreed weights; only Building storeys is measured in P0.</summary>
        private static IReadOnlyList<GroupVm> BuildGroups(bool? storeysOk) =>
            Empty.Groups.Select(g => new GroupVm(
                g.Name,
                $"{g.Weight:0} points",
                g.Items.Select(i =>
                {
                    if (i.Id == "A3" && storeysOk.HasValue)
                        return new ItemVm(i.Name, $"{(storeysOk.Value ? i.Weight : 0):0} / {i.Weight:0}", storeysOk.Value ? "Pass" : "Fail");
                    return new ItemVm(i.Name, $"– / {i.Weight:0}", "Not checked yet");
                }).ToList())).ToList();
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CxPreflight.Core
{
    /// <summary>A Revit level as the checks see it.</summary>
    public sealed class LevelInfo
    {
        public LevelInfo(string name, double elevationMm, bool isBuildingStory)
        {
            Name = name;
            ElevationMm = elevationMm;
            IsBuildingStory = isBuildingStory;
        }

        public string Name { get; }
        public double ElevationMm { get; }
        public bool IsBuildingStory { get; }
    }

    /// <summary>What one check run read from the model. P0 reads levels and counts only.</summary>
    public sealed class ModelSummary
    {
        public string DocumentTitle { get; set; } = "";
        public int ElementCount { get; set; }
        public IReadOnlyList<LevelInfo> Levels { get; set; } = Array.Empty<LevelInfo>();
        public DateTime CheckedAt { get; set; }
        public long DurationMs { get; set; }
    }

    /// <summary>Edits made in Revit since the last run, counted cheaply as they happen.</summary>
    public sealed class ModelChangedEventArgs : EventArgs
    {
        public ModelChangedEventArgs(int changedElements, bool levelsChanged)
        {
            ChangedElements = changedElements;
            LevelsChanged = levelsChanged;
        }

        public int ChangedElements { get; }
        public bool LevelsChanged { get; }
    }

    /// <summary>
    /// What the sidebar asks of its host. Inside Revit the host queues the work
    /// onto Revit's API thread; in the preview app it returns sample data.
    /// The sidebar never touches the Revit API itself.
    /// </summary>
    public interface IPreflightHost
    {
        /// <summary>Runs the checks on the active model; null when no model is open.</summary>
        Task<ModelSummary?> RunChecksAsync(CancellationToken cancellationToken = default);

        /// <summary>Raised on the UI thread after edits made outside the panel.</summary>
        event EventHandler<ModelChangedEventArgs>? ModelChanged;
    }
}

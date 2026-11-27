using System;
using System.Collections.Generic;
using System.Linq;

namespace CxPreflight.Core
{
    /// <summary>How an item shows in the panel.</summary>
    public enum ItemState { Pass, Part, Fail, Blocked }

    /// <summary>
    /// One scored item, such as "Building storeys set". Earned points are
    /// the item's ratio (0..1) times its weight; a blocked item earns 0.
    /// </summary>
    public sealed class ReadinessItem
    {
        public ReadinessItem(string id, string name, double weight, double ratio, bool blocked = false)
        {
            if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
            Id = id;
            Name = name;
            Weight = weight;
            Ratio = blocked ? 0 : Clamp01(ratio);
            Blocked = blocked;
        }

        public string Id { get; }
        public string Name { get; }
        public double Weight { get; }
        public double Ratio { get; }
        public bool Blocked { get; }
        public double Earned => Weight * Ratio;

        public ItemState State =>
            Blocked ? ItemState.Blocked
            : Ratio >= 1 ? ItemState.Pass
            : Ratio <= 0 ? ItemState.Fail
            : ItemState.Part;

        internal static double Clamp01(double v) => double.IsNaN(v) ? 0 : Math.Max(0, Math.Min(1, v));
    }

    /// <summary>A group of items: Foundations, Classification and so on.</summary>
    public sealed class ReadinessGroup
    {
        public ReadinessGroup(string id, string name, IReadOnlyList<ReadinessItem> items)
        {
            Id = id;
            Name = name;
            Items = items;
        }

        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<ReadinessItem> Items { get; }
        public double Weight => Items.Sum(i => i.Weight);
        public double Earned => Items.Sum(i => i.Earned);
        public double Ratio => Weight > 0 ? Earned / Weight : 0;
        public bool AnyBlocked => Items.Any(i => i.Blocked);
    }

    /// <summary>
    /// The readiness of one model file: five weighted groups adding up to 100.
    /// READY needs the full 100 with nothing blocked (agreed 27 Nov, rule 4).
    /// </summary>
    public sealed class FileReadiness
    {
        public FileReadiness(IReadOnlyList<ReadinessGroup> groups) => Groups = groups;

        public IReadOnlyList<ReadinessGroup> Groups { get; }
        public double Total => Groups.Sum(g => g.Earned);
        public int Percent => (int)Math.Round(Total, MidpointRounding.AwayFromZero);
        public bool IsReady => Total >= 100 - 1e-9 && !Groups.Any(g => g.AnyBlocked);

        public ReadinessGroup Group(string id) => Groups.First(g => g.Id == id);
    }

    /// <summary>What the checks measured, in plain numbers. Filled by the Revit layer or by tests.</summary>
    public sealed class ReadinessInputs
    {
        public bool ProjectSetupDone { get; set; }
        public bool ExportSettingsMatch { get; set; }
        public bool BuildingStoreysSet { get; set; }
        public bool CoordinatesOk { get; set; }
        public int ParametersRequired { get; set; }
        public int ParametersLoaded { get; set; }

        public int ExportedElements { get; set; }
        public int CorrectlyClassified { get; set; }

        /// <summary>Required CX value slots for the chosen gateway.</summary>
        public int ValuesRequired { get; set; }
        public int ValuesFilled { get; set; }
        /// <summary>Slots marked "not required"; they count as met (rule 2).</summary>
        public int ValuesNotRequired { get; set; }

        public int SpacesTotal { get; set; }
        public int SpacesValid { get; set; }

        public int AgencyRulesRun { get; set; }
        public int AgencyRulesPassed { get; set; }
    }

    /// <summary>
    /// Scoring with the weights agreed on 27 Nov:
    /// Foundations 10 (5 items x 2), Classification 10, Data completeness 50,
    /// Spaces and areas 20, Agency rules 10.
    /// </summary>
    public static class ReadinessCalculator
    {
        public const double FoundationItemWeight = 2;
        public const double ClassificationWeight = 10;
        public const double DataWeight = 50;
        public const double SpacesWeight = 20;
        public const double AgencyWeight = 10;

        public static FileReadiness Score(ReadinessInputs x)
        {
            bool paramsMissing = x.ParametersRequired > 0 && x.ParametersLoaded < x.ParametersRequired;

            var foundations = new ReadinessGroup("A", "Foundations", new[]
            {
                new ReadinessItem("A1", "Project setup", FoundationItemWeight, x.ProjectSetupDone ? 1 : 0),
                new ReadinessItem("A2", "IFC export settings", FoundationItemWeight, x.ExportSettingsMatch ? 1 : 0),
                new ReadinessItem("A3", "Building storeys", FoundationItemWeight, x.BuildingStoreysSet ? 1 : 0),
                new ReadinessItem("A4", "Coordinates and survey point", FoundationItemWeight, x.CoordinatesOk ? 1 : 0),
                new ReadinessItem("A5", "CX parameters loaded", FoundationItemWeight, Ratio(x.ParametersLoaded, x.ParametersRequired)),
            });

            var classification = new ReadinessGroup("B", "Classification", new[]
            {
                new ReadinessItem("B1", "IFC class and subtype", ClassificationWeight, Ratio(x.CorrectlyClassified, x.ExportedElements)),
            });

            // Rule 3: values cannot be checked until the parameters exist, so the group is blocked.
            var data = new ReadinessGroup("C", "Data completeness", new[]
            {
                new ReadinessItem("C1", "Mandatory CX values", DataWeight,
                    Ratio(x.ValuesFilled + x.ValuesNotRequired, x.ValuesRequired), blocked: paramsMissing),
            });

            var spaces = new ReadinessGroup("D", "Spaces and areas", new[]
            {
                new ReadinessItem("D1", "Rooms, spaces and GFA", SpacesWeight, Ratio(x.SpacesValid, x.SpacesTotal)),
            });

            var agency = new ReadinessGroup("E", "Agency rules", new[]
            {
                new ReadinessItem("E1", "URA / BCA rules", AgencyWeight,
                    Ratio(x.AgencyRulesPassed, x.AgencyRulesRun), blocked: paramsMissing),
            });

            return new FileReadiness(new[] { foundations, classification, data, spaces, agency });
        }

        /// <summary>part / whole; nothing required counts as complete.</summary>
        public static double Ratio(int part, int whole) =>
            whole <= 0 ? 1 : ReadinessItem.Clamp01((double)part / whole);
    }

    /// <summary>One file's share of the project.</summary>
    public sealed class ProjectFile
    {
        public ProjectFile(string name, int exportedElements, double percent)
        {
            Name = name;
            ExportedElements = exportedElements;
            Percent = percent;
        }

        public string Name { get; }
        /// <summary>Weight source: the file's exported elements at its last check.</summary>
        public int ExportedElements { get; }
        public double Percent { get; }
        /// <summary>Set by the BIM Lead to override the automatic weight, as a share 0..1.</summary>
        public double? WeightOverride { get; set; }
    }

    /// <summary>Project readiness: each file's % weighted by its share of exported elements.</summary>
    public static class ProjectReadiness
    {
        public static IReadOnlyList<double> Weights(IReadOnlyList<ProjectFile> files)
        {
            double fixedShare = files.Where(f => f.WeightOverride.HasValue).Sum(f => f.WeightOverride!.Value);
            double autoElements = files.Where(f => !f.WeightOverride.HasValue).Sum(f => (double)f.ExportedElements);
            double remaining = Math.Max(0, 1 - fixedShare);
            return files.Select(f =>
                f.WeightOverride ?? (autoElements > 0 ? remaining * f.ExportedElements / autoElements : 0)).ToList();
        }

        public static double Percent(IReadOnlyList<ProjectFile> files)
        {
            var w = Weights(files);
            double total = w.Sum();
            if (total <= 0) return 0;
            return files.Select((f, i) => f.Percent * w[i]).Sum() / total;
        }
    }
}

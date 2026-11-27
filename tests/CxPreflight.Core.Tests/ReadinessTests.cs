using CxPreflight.Core;
using Xunit;

namespace CxPreflight.Core.Tests;

/// <summary>
/// The worked examples from the agreed weighting board (27 Nov), using the
/// ABCD BLK3 figures from the October run.
/// </summary>
public class ReadinessTests
{
    /// <summary>BLK3 as of October: 18 of 338 parameters, 226 proxies, 17 open GFA areas.</summary>
    private static ReadinessInputs Blk3Now() => new()
    {
        ProjectSetupDone = true,
        ExportSettingsMatch = true,
        BuildingStoreysSet = false,
        CoordinatesOk = true,
        ParametersRequired = 338,
        ParametersLoaded = 18,
        ExportedElements = 1000,
        CorrectlyClassified = 960,
        ValuesRequired = 0,
        SpacesTotal = 10,
        SpacesValid = 7,
        AgencyRulesRun = 0,
    };

    [Fact]
    public void Weights_add_up_to_100()
    {
        var r = ReadinessCalculator.Score(new ReadinessInputs());
        Assert.Equal(100, r.Groups.Sum(g => g.Weight), 6);
        Assert.Equal(new[] { 10.0, 10, 50, 20, 10 }, r.Groups.Select(g => g.Weight));
        Assert.All(r.Group("A").Items, i => Assert.Equal(2, i.Weight));
    }

    [Fact]
    public void Blk3_scores_30_percent_with_data_and_agency_blocked()
    {
        var r = ReadinessCalculator.Score(Blk3Now());

        Assert.Equal(6.1, r.Group("A").Earned, 1);
        Assert.Equal(9.6, r.Group("B").Earned, 1);
        Assert.Equal(0, r.Group("C").Earned);
        Assert.Equal(14, r.Group("D").Earned, 6);
        Assert.Equal(0, r.Group("E").Earned);
        Assert.Equal(30, r.Percent);

        Assert.Equal(ItemState.Fail, r.Group("A").Items.Single(i => i.Id == "A3").State);
        Assert.Equal(ItemState.Part, r.Group("A").Items.Single(i => i.Id == "A5").State);
        Assert.Equal(ItemState.Blocked, r.Group("C").Items[0].State);
        Assert.Equal(ItemState.Blocked, r.Group("E").Items[0].State);
        Assert.False(r.IsReady);
    }

    [Fact]
    public void Loading_parameters_unblocks_values_and_lifts_blk3_to_42_percent()
    {
        var x = Blk3Now();
        x.ParametersLoaded = 338;
        x.ValuesRequired = 1000;
        x.ValuesFilled = 100;
        x.ValuesNotRequired = 20;   // rule 2: not required counts as met -> 12%
        x.AgencyRulesRun = 10;
        x.AgencyRulesPassed = 4;

        var r = ReadinessCalculator.Score(x);

        Assert.Equal(8, r.Group("A").Earned, 6);
        Assert.Equal(6, r.Group("C").Earned, 6);
        Assert.Equal(4, r.Group("E").Earned, 6);
        Assert.Equal(42, r.Percent);
        Assert.Equal(ItemState.Part, r.Group("C").Items[0].State);
    }

    [Fact]
    public void Ready_only_at_100_with_nothing_blocked()
    {
        var all = new ReadinessInputs
        {
            ProjectSetupDone = true, ExportSettingsMatch = true, BuildingStoreysSet = true, CoordinatesOk = true,
            ParametersRequired = 5, ParametersLoaded = 5,
            ExportedElements = 10, CorrectlyClassified = 10,
            ValuesRequired = 10, ValuesFilled = 9, ValuesNotRequired = 1,
            SpacesTotal = 3, SpacesValid = 3,
            AgencyRulesRun = 2, AgencyRulesPassed = 2,
        };
        Assert.True(ReadinessCalculator.Score(all).IsReady);

        all.ValuesFilled = 8;
        var r = ReadinessCalculator.Score(all);
        Assert.False(r.IsReady);
        Assert.Equal(95, r.Percent);
    }

    [Fact]
    public void Project_is_weighted_by_exported_elements()
    {
        // The five ABCD files from the Project Setup mockup: weights 28/31/22/9/10%.
        var files = new List<ProjectFile>
        {
            new("ABCD_BLK1_ARC.rvt", 2800, 64),
            new("ABCD_BLK3_ARC.rvt", 3100, 30),
            new("ABCD_PODIUM_ARC.rvt", 2200, 48),
            new("ABCD_SITE_EXT.rvt", 900, 71),
            new("ABCD_CS_BLK1-3.ifc", 1000, 55),
        };
        Assert.Equal(50, Math.Round(ProjectReadiness.Percent(files)));

        files[1] = new ProjectFile("ABCD_BLK3_ARC.rvt", 3100, 42);
        Assert.Equal(53, Math.Round(ProjectReadiness.Percent(files)));
    }

    [Fact]
    public void Bim_lead_override_takes_its_share_and_the_rest_split_by_elements()
    {
        var files = new List<ProjectFile>
        {
            new("A.rvt", 100, 100) { WeightOverride = 0.5 },
            new("B.rvt", 300, 0),
            new("C.rvt", 100, 0),
        };
        var w = ProjectReadiness.Weights(files);
        Assert.Equal(0.5, w[0], 6);
        Assert.Equal(0.375, w[1], 6);
        Assert.Equal(0.125, w[2], 6);
        Assert.Equal(50, ProjectReadiness.Percent(files), 6);
    }
}

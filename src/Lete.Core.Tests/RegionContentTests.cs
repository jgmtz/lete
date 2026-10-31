using System.Text.RegularExpressions;
using Lete.Core.World;

namespace Lete.Core.Tests;

/// <summary>As regiões (<c>content/regions</c>) e as cenas delas: chegada padrão e passagens apontando para lugares que existem.</summary>
public class RegionContentTests
{
    private static readonly string Root = FindRoot();

    private static readonly Regex Marker = new(@"\[node name=""([^""]+)"" type=""Marker3D""[^\]]*teleport_points");

    private static readonly Regex Exit = new(@"TargetRegion = ""([^""]*)""\s+TargetEntry = ""([^""]*)""");

    private static IReadOnlyDictionary<string, RegionDefinition> Regions() =>
        Directory.GetFiles(Path.Combine(Root, "content", "regions"), "*.json")
            .Select(f => WorldData.RegionFromJson(File.ReadAllText(f)))
            .ToDictionary(r => r.Id);

    private static string SceneText(RegionDefinition region) =>
        File.ReadAllText(Path.Combine(Root, region.Scene["res://".Length..]));

    private static HashSet<string> Markers(RegionDefinition region) =>
        Marker.Matches(SceneText(region)).Select(m => m.Groups[1].Value).ToHashSet();

    [Fact]
    public void EveryRegion_HasItsSceneAndDefaultEntry_AndIdMatchesTheFile()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "content", "regions"), "*.json"))
        {
            var region = WorldData.RegionFromJson(File.ReadAllText(file));
            Assert.Equal(Path.GetFileNameWithoutExtension(file), region.Id);
            Assert.True(File.Exists(Path.Combine(Root, region.Scene["res://".Length..])), region.Scene);
            Assert.Contains(region.Entry, Markers(region));
        }

        Assert.Contains("tenaro", Regions().Keys);
    }

    [Fact]
    public void EveryPassage_LeadsToAnExistingRegionAndMarker()
    {
        var regions = Regions();
        var passages = 0;
        foreach (var region in regions.Values)
        {
            foreach (Match exit in Exit.Matches(SceneText(region)))
            {
                passages++;
                var target = exit.Groups[1].Value;
                Assert.True(regions.ContainsKey(target), $"{region.Id} → região '{target}' não existe.");
                Assert.Contains(exit.Groups[2].Value, Markers(regions[target]));
            }
        }

        Assert.True(passages >= 2);
    }

    [Fact]
    public void TriviasOfARegion_PointToARegionThatExists()
    {
        var regions = Regions();
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "content", "trivias"), "*.json"))
        {
            Assert.Contains(WorldData.TriviaFromJson(File.ReadAllText(file)).RegionId, regions.Keys);
        }
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("project.godot não encontrado.");
    }
}

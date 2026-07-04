using System.Collections.Generic;
using System.Linq;
using BlueBeard.Core.Validation;
using Rocket.API;
using Xunit;

namespace BlueBeard.Tests.Core;

public class Reward
{
    [Range(0, 100)] public double Chance { get; set; } = 50;
    [NotEmpty] public string ItemName { get; set; } = "item";
}

public class SampleRocketConfig : IRocketPluginConfiguration
{
    [Range(1, 100)] public int MaxPlayers { get; set; }
    [MinValue(0)] public double SpawnRate { get; set; }
    [MaxValue(60)] public int CooldownSeconds { get; set; }
    [NotEmpty] public string WelcomeMessage { get; set; }
    [RegexMatch("^#?[0-9a-fA-F]{6}$")] public string ChatColor { get; set; }
    [OneOf("easy", "normal", "hard")] public string Difficulty { get; set; }
    [ValidateNested] public List<Reward> Rewards { get; set; }
    [ValidateNested] public Reward Featured { get; set; }

    public void LoadDefaults()
    {
        MaxPlayers = 24;
        SpawnRate = 1.5;
        CooldownSeconds = 30;
        WelcomeMessage = "Welcome!";
        ChatColor = "#00ff00";
        Difficulty = "normal";
        Rewards = [new Reward()];
        Featured = new Reward();
    }
}

public class ConfigValidatorValidateTests
{
    private static SampleRocketConfig Valid()
    {
        var c = new SampleRocketConfig();
        c.LoadDefaults();
        return c;
    }

    [Fact]
    public void Valid_Config_Produces_No_Errors()
    {
        Assert.True(ConfigValidator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Range_Min_Max_Violations_Are_Reported()
    {
        var c = Valid();
        c.MaxPlayers = 500;
        c.SpawnRate = -3;
        c.CooldownSeconds = 999;

        var result = ConfigValidator.Validate(c);

        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.PropertyPath == "MaxPlayers");
        Assert.Contains(result.Errors, e => e.PropertyPath == "SpawnRate");
        Assert.Contains(result.Errors, e => e.PropertyPath == "CooldownSeconds");
    }

    [Fact]
    public void NotEmpty_Rejects_Null_Whitespace_And_Empty_Collections()
    {
        var c = Valid();
        c.WelcomeMessage = "   ";
        var result = ConfigValidator.Validate(c);
        Assert.Contains(result.Errors, e => e.PropertyPath == "WelcomeMessage");
    }

    [Fact]
    public void RegexMatch_And_OneOf_Are_Enforced()
    {
        var c = Valid();
        c.ChatColor = "not-a-color";
        c.Difficulty = "nightmare";

        var result = ConfigValidator.Validate(c);

        Assert.Contains(result.Errors, e => e.PropertyPath == "ChatColor");
        Assert.Contains(result.Errors, e => e.PropertyPath == "Difficulty");
    }

    [Fact]
    public void OneOf_Is_Case_Insensitive()
    {
        var c = Valid();
        c.Difficulty = "NORMAL";
        Assert.True(ConfigValidator.Validate(c).IsValid);
    }

    [Fact]
    public void Nested_Objects_And_List_Elements_Are_Validated_With_Paths()
    {
        var c = Valid();
        c.Rewards.Add(new Reward { Chance = 250, ItemName = "" });
        c.Featured.Chance = -5;

        var result = ConfigValidator.Validate(c);

        Assert.Contains(result.Errors, e => e.PropertyPath == "Rewards[1].Chance");
        Assert.Contains(result.Errors, e => e.PropertyPath == "Rewards[1].ItemName");
        Assert.Contains(result.Errors, e => e.PropertyPath == "Featured.Chance");
    }
}

public class ConfigValidatorCorrectTests
{
    [Fact]
    public void Range_Violations_Clamp_To_Nearest_Bound()
    {
        var c = new SampleRocketConfig();
        c.LoadDefaults();
        c.MaxPlayers = 500;
        c.SpawnRate = -3;

        var report = ConfigValidator.ValidateAndCorrect(c);

        Assert.Equal(100, c.MaxPlayers);
        Assert.Equal(0, c.SpawnRate);
        Assert.Equal(2, report.Corrections.Count);
        Assert.All(report.Corrections, f => Assert.Contains("clamped", f.Reason));
    }

    [Fact]
    public void Non_Numeric_Violations_Reset_From_LoadDefaults()
    {
        var c = new SampleRocketConfig();
        c.LoadDefaults();
        c.WelcomeMessage = "";
        c.Difficulty = "nightmare";

        var report = ConfigValidator.ValidateAndCorrect(c);

        Assert.Equal("Welcome!", c.WelcomeMessage);
        Assert.Equal("normal", c.Difficulty);
        Assert.Equal(2, report.Corrections.Count);
    }

    [Fact]
    public void Nested_Object_Violations_Correct_Against_Defaults_Counterpart()
    {
        var c = new SampleRocketConfig();
        c.LoadDefaults();
        c.Featured.Chance = 999;

        var report = ConfigValidator.ValidateAndCorrect(c);

        Assert.Equal(100, c.Featured.Chance); // clamped
        Assert.Single(report.Corrections);
        Assert.Equal("Featured.Chance", report.Corrections[0].PropertyPath);
    }

    [Fact]
    public void Nested_List_Elements_Clamp_But_Report_Unresettable_Violations()
    {
        var c = new SampleRocketConfig();
        c.LoadDefaults();
        c.Rewards[0].Chance = 250;    // clampable
        c.Rewards[0].ItemName = "";   // needs defaults, which lists can't pair

        var report = ConfigValidator.ValidateAndCorrect(c);

        Assert.Equal(100, c.Rewards[0].Chance);
        Assert.Contains(report.Corrections, f => f.PropertyPath == "Rewards[0].Chance");
        Assert.Contains(report.Uncorrectable, e => e.PropertyPath == "Rewards[0].ItemName");
    }

    [Fact]
    public void Valid_Config_Is_Untouched()
    {
        var c = new SampleRocketConfig();
        c.LoadDefaults();

        var report = ConfigValidator.ValidateAndCorrect(c);

        Assert.False(report.ChangedAnything);
        Assert.Empty(report.Uncorrectable);
    }

    [Fact]
    public void Plain_Poco_Without_LoadDefaults_Uses_Ctor_Defaults()
    {
        var reward = new Reward { Chance = -10, ItemName = "" };

        var report = ConfigValidator.ValidateAndCorrect(reward);

        Assert.Equal(0, reward.Chance);        // clamped to Min
        Assert.Equal("item", reward.ItemName); // reset from new Reward()
        Assert.Equal(2, report.Corrections.Count);
    }
}

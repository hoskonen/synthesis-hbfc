using System.ComponentModel.DataAnnotations;
using Mutagen.Bethesda.Synthesis.Settings;

namespace HBFC;

public sealed class Settings
{
    // Initial v1 defaults reproduce the historical Higher Bounties baseline
    // and hold-specific values. They are intentionally easy to rebalance later.
    [SynthesisSettingName("Eastmarch")]
    public CrimeFactionSettings Eastmarch { get; set; } = new()
    {
        Murder = 10000,
        Assault = 100,
        Trespass = 100,
        Pickpocket = 100,
        StealMultiplier = 3.0f,
        Escape = 1000,
        Werewolf = 10000,
    };

    [SynthesisSettingName("Whiterun")]
    public CrimeFactionSettings Whiterun { get; set; } = new()
    {
        Murder = 10000,
        Assault = 100,
        Trespass = 10,
        Pickpocket = 100,
        StealMultiplier = 3.0f,
        Escape = 1000,
        Werewolf = 10000,
    };

    [SynthesisSettingName("The Rift")]
    public CrimeFactionSettings Rift { get; set; } = new()
    {
        Murder = 10000,
        Assault = 100,
        Trespass = 100,
        Pickpocket = 100,
        StealMultiplier = 4.0f,
        Escape = 1000,
        Werewolf = 10000,
    };

    [SynthesisSettingName("The Reach")]
    public CrimeFactionSettings Reach { get; set; } = new()
    {
        Murder = 12000,
        Assault = 200,
        Trespass = 10,
        Pickpocket = 50,
        StealMultiplier = 2.0f,
        Escape = 0,
        Werewolf = 10000,
    };

    [SynthesisSettingName("Hjaalmarch")]
    public CrimeFactionSettings Hjaalmarch { get; set; } = HistoricalBaseline(15000);

    [SynthesisSettingName("The Pale")]
    public CrimeFactionSettings Pale { get; set; } = HistoricalBaseline(
        stealMultiplier: 4.0f);

    [SynthesisSettingName("Winterhold")]
    public CrimeFactionSettings Winterhold { get; set; } = HistoricalBaseline(
        murder: 15000,
        assault: 500);

    [SynthesisSettingName("Falkreath")]
    public CrimeFactionSettings Falkreath { get; set; } = HistoricalBaseline(
        werewolf: 15000);

    [SynthesisSettingName("Haafingar")]
    public CrimeFactionSettings Haafingar { get; set; } = HistoricalBaseline(
        murder: 15000,
        assault: 500);

    [SynthesisSettingName("Orc Crime Faction")]
    public CrimeFactionSettings Orcs { get; set; } = HistoricalBaseline();

    [SynthesisSettingName("Raven Rock")]
    public CrimeFactionSettings RavenRock { get; set; } = HistoricalBaseline();

    private static CrimeFactionSettings HistoricalBaseline(
        int murder = 10000,
        int assault = 100,
        float stealMultiplier = 2.0f,
        int werewolf = 10000)
    {
        return new CrimeFactionSettings
        {
            Murder = murder,
            Assault = assault,
            Trespass = 10,
            Pickpocket = 50,
            StealMultiplier = stealMultiplier,
            Escape = 1000,
            Werewolf = werewolf,
        };
    }
}

public sealed class CrimeFactionSettings
{
    // int is used so hand-edited JSON outside the ushort range reaches the
    // patcher's explicit validation instead of silently wrapping or truncating.
    [Range(0, ushort.MaxValue)]
    public int Murder { get; set; }

    [Range(0, ushort.MaxValue)]
    public int Assault { get; set; }

    [Range(0, ushort.MaxValue)]
    public int Trespass { get; set; }

    [Range(0, ushort.MaxValue)]
    public int Pickpocket { get; set; }

    [SynthesisSettingName("Steal Multiplier")]
    [Range(0.0, float.MaxValue)]
    public float StealMultiplier { get; set; }

    [Range(0, ushort.MaxValue)]
    public int Escape { get; set; }

    [Range(0, ushort.MaxValue)]
    public int Werewolf { get; set; }
}

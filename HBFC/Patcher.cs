using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace HBFC;

public static class Patcher
{
    private sealed record TargetDefinition(
        string DisplayName,
        string EditorId,
        FormKey FormKey,
        Func<Settings, CrimeFactionSettings?> SelectSettings);

    private static readonly ModKey Skyrim =
        ModKey.FromNameAndExtension("Skyrim.esm");

    private static readonly ModKey Dragonborn =
        ModKey.FromNameAndExtension("Dragonborn.esm");

    private static readonly IReadOnlyList<TargetDefinition> Targets =
    [
        Target("Eastmarch", "CrimeFactionEastmarch", Skyrim, 0x000267E3, s => s.Eastmarch),
        Target("Whiterun", "CrimeFactionWhiterun", Skyrim, 0x000267EA, s => s.Whiterun),
        Target("The Rift", "CrimeFactionRift", Skyrim, 0x0002816B, s => s.Rift),
        Target("The Reach", "CrimeFactionReach", Skyrim, 0x0002816C, s => s.Reach),
        Target("Hjaalmarch", "CrimeFactionHjaalmarch", Skyrim, 0x0002816D, s => s.Hjaalmarch),
        Target("The Pale", "CrimeFactionPale", Skyrim, 0x0002816E, s => s.Pale),
        Target("Winterhold", "CrimeFactionWinterhold", Skyrim, 0x0002816F, s => s.Winterhold),
        Target("Falkreath", "CrimeFactionFalkreath", Skyrim, 0x00028170, s => s.Falkreath),
        Target("Haafingar", "CrimeFactionHaafingar", Skyrim, 0x00029DB0, s => s.Haafingar),
        Target("Orc Crime Faction", "CrimeFactionOrcs", Skyrim, 0x00028713, s => s.Orcs),
        Target("Raven Rock", "DLC2CrimeRavenRockFaction", Dragonborn, 0x00018279, s => s.RavenRock),
    ];

    public static void Run(
        IPatcherState<ISkyrimMod, ISkyrimModGetter> state,
        Settings settings,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(output);

        ValidateSettings(settings, output);

        int examined = 0;
        int patched = 0;
        int unchanged = 0;
        int defaultFlagsCleared = 0;
        int missing = 0;
        int deleted = 0;

        foreach (TargetDefinition target in Targets)
        {
            examined++;
            CrimeFactionSettings desired = target.SelectSettings(settings)!;

            if (!state.LinkCache.TryResolve<IFactionGetter>(
                    target.FormKey,
                    out var winningFaction,
                    ResolveTarget.Winner))
            {
                missing++;
                output.WriteLine(
                    $"WARN: Missing target {FormatTarget(target)}; skipped.");
                continue;
            }

            if (winningFaction.IsDeleted)
            {
                deleted++;
                output.WriteLine(
                    $"WARN: Winning target {FormatTarget(target)} is deleted; skipped.");
                continue;
            }

            ICrimeValuesGetter? current = winningFaction.CrimeValues;
            if (current is null)
            {
                missing++;
                output.WriteLine(
                    $"WARN: Winning target {FormatTarget(target)} has no CrimeValues; skipped.");
                continue;
            }
            bool clearDefaults =
                (winningFaction.Flags &
                 Faction.FactionFlag.CrimeGoldUseDefaults) != 0;

            bool murderChanged = current.Murder != desired.Murder;
            bool assaultChanged = current.Assault != desired.Assault;
            bool trespassChanged = current.Trespass != desired.Trespass;
            bool pickpocketChanged = current.Pickpocket != desired.Pickpocket;
            bool stealMultiplierChanged = current.StealMult != desired.StealMultiplier;
            bool escapeChanged = current.Escape != desired.Escape;
            bool werewolfChanged = current.Werewolf != desired.Werewolf;

            if (!murderChanged &&
                !assaultChanged &&
                !trespassChanged &&
                !pickpocketChanged &&
                !stealMultiplierChanged &&
                !escapeChanged &&
                !werewolfChanged &&
                !clearDefaults)
            {
                unchanged++;
                continue;
            }

            Faction overrideFaction =
                state.PatchMod.Factions.GetOrAddAsOverride(winningFaction);
            CrimeValues crimeValues = overrideFaction.CrimeValues!;

            crimeValues.Murder = (ushort)desired.Murder;
            crimeValues.Assault = (ushort)desired.Assault;
            crimeValues.Trespass = (ushort)desired.Trespass;
            crimeValues.Pickpocket = (ushort)desired.Pickpocket;
            crimeValues.StealMult = desired.StealMultiplier;
            crimeValues.Escape = (ushort)desired.Escape;
            crimeValues.Werewolf = (ushort)desired.Werewolf;
            overrideFaction.Flags &=
                ~Faction.FactionFlag.CrimeGoldUseDefaults;

            patched++;
            if (clearDefaults)
            {
                defaultFlagsCleared++;
            }

            WritePatchLog(
                output,
                target,
                winningFaction.EditorID,
                current,
                desired,
                murderChanged,
                assaultChanged,
                trespassChanged,
                pickpocketChanged,
                stealMultiplierChanged,
                escapeChanged,
                werewolfChanged,
                clearDefaults);
        }

        WriteSummary(
            output,
            examined,
            patched,
            unchanged,
            defaultFlagsCleared,
            missing,
            deleted);
    }

    private static void ValidateSettings(Settings settings, TextWriter output)
    {
        var errors = new List<string>();

        foreach (TargetDefinition target in Targets)
        {
            CrimeFactionSettings? values = target.SelectSettings(settings);
            if (values is null)
            {
                errors.Add($"{target.DisplayName}: settings section is null.");
                continue;
            }

            ValidateUShort(errors, target.DisplayName, nameof(values.Murder), values.Murder);
            ValidateUShort(errors, target.DisplayName, nameof(values.Assault), values.Assault);
            ValidateUShort(errors, target.DisplayName, nameof(values.Trespass), values.Trespass);
            ValidateUShort(errors, target.DisplayName, nameof(values.Pickpocket), values.Pickpocket);
            ValidateUShort(errors, target.DisplayName, nameof(values.Escape), values.Escape);
            ValidateUShort(errors, target.DisplayName, nameof(values.Werewolf), values.Werewolf);

            if (!float.IsFinite(values.StealMultiplier) ||
                values.StealMultiplier < 0.0f)
            {
                errors.Add(
                    $"{target.DisplayName}.{nameof(values.StealMultiplier)} must be " +
                    "a finite number greater than or equal to zero.");
            }
        }

        if (errors.Count == 0)
        {
            return;
        }

        output.WriteLine("FATAL: HBFC settings validation failed. No records were modified.");
        foreach (string error in errors)
        {
            output.WriteLine($"  - {error}");
        }

        throw new ValidationException(
            $"HBFC settings validation failed with {errors.Count} error(s).");
    }

    private static void ValidateUShort(
        ICollection<string> errors,
        string section,
        string field,
        int value)
    {
        if (value is < ushort.MinValue or > ushort.MaxValue)
        {
            errors.Add(
                $"{section}.{field} must be between {ushort.MinValue} and " +
                $"{ushort.MaxValue}; received {value}.");
        }
    }

    private static void WritePatchLog(
        TextWriter output,
        TargetDefinition target,
        string? winningEditorId,
        ICrimeValuesGetter current,
        CrimeFactionSettings desired,
        bool murderChanged,
        bool assaultChanged,
        bool trespassChanged,
        bool pickpocketChanged,
        bool stealMultiplierChanged,
        bool escapeChanged,
        bool werewolfChanged,
        bool defaultFlagCleared)
    {
        string editorId = winningEditorId ?? target.EditorId;
        output.WriteLine(
            $"Patched {target.DisplayName} ({editorId}, {FormatFormKey(target.FormKey)})");

        WriteChange(output, "Murder", current.Murder, desired.Murder, murderChanged);
        WriteChange(output, "Assault", current.Assault, desired.Assault, assaultChanged);
        WriteChange(output, "Trespass", current.Trespass, desired.Trespass, trespassChanged);
        WriteChange(output, "Pickpocket", current.Pickpocket, desired.Pickpocket, pickpocketChanged);
        if (stealMultiplierChanged)
        {
            output.WriteLine(
                $"  StealMultiplier: {FormatFloat(current.StealMult)} -> " +
                FormatFloat(desired.StealMultiplier));
        }
        WriteChange(output, "Escape", current.Escape, desired.Escape, escapeChanged);
        WriteChange(output, "Werewolf", current.Werewolf, desired.Werewolf, werewolfChanged);
        output.WriteLine(
            $"  CrimeGoldUseDefaults: " +
            (defaultFlagCleared ? "cleared" : "already clear"));
    }

    private static void WriteChange(
        TextWriter output,
        string field,
        ushort oldValue,
        int newValue,
        bool changed)
    {
        if (changed)
        {
            output.WriteLine($"  {field}: {oldValue} -> {newValue}");
        }
    }

    private static void WriteSummary(
        TextWriter output,
        int examined,
        int patched,
        int unchanged,
        int defaultFlagsCleared,
        int missing,
        int deleted)
    {
        output.WriteLine($"""
            HBFC summary
              Targets examined: {examined}
              Patched: {patched}
              Unchanged/no-op: {unchanged}
              Default flags cleared: {defaultFlagsCleared}
              Missing: {missing}
              Deleted/skipped: {deleted}
            """);
    }

    private static TargetDefinition Target(
        string displayName,
        string editorId,
        ModKey modKey,
        uint localFormId,
        Func<Settings, CrimeFactionSettings?> selectSettings)
    {
        return new TargetDefinition(
            displayName,
            editorId,
            new FormKey(modKey, localFormId),
            selectSettings);
    }

    private static string FormatTarget(TargetDefinition target) =>
        $"{target.DisplayName} ({target.EditorId}, {FormatFormKey(target.FormKey)})";

    private static string FormatFormKey(FormKey formKey) =>
        $"{formKey.ID:X8}:{formKey.ModKey.FileName.String}";

    private static string FormatFloat(float value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);
}

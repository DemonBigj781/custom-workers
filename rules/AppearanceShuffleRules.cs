namespace CustomWorkers;

internal enum AppearanceShuffleTarget
{
    Customers,
    Workers
}

internal enum AppearanceShufflePart
{
    Shirt,
    Pants,
    Shoes,
    Hair
}

internal sealed class AppearanceShuffleOptions
{
    internal KillSwitchHelper.RuntimeMode RuntimeMode { get; set; } = KillSwitchHelper.RuntimeMode.Full;
    internal bool EnableMod { get; set; } = true;
    internal bool EnableArcUi { get; set; } = true;
    internal bool EnableArcAssetLoader { get; set; } = true;
    internal bool EnablePhoneOverhaulHooks { get; set; } = true;
    internal bool EnableRosterExtension { get; set; } = true;
    internal bool EnableWorkerRuntimePatch { get; set; } = true;
    internal bool EnableNpcMutator { get; set; } = true;
    internal bool EnableCustomerAppearancePatch { get; set; }
    internal bool EnableWorkerAppearancePatch { get; set; }
    internal bool EnableGeneratedWorkerModelValidation { get; set; }
    internal bool CustomerShirt { get; set; } = true;
    internal bool CustomerPants { get; set; } = true;
    internal bool CustomerShoes { get; set; } = true;
    internal bool CustomerHair { get; set; } = true;
    internal bool WorkerShirt { get; set; } = true;
    internal bool WorkerPants { get; set; } = true;
    internal bool WorkerShoes { get; set; } = true;
    internal bool WorkerHair { get; set; } = true;
    internal AppearanceSkinMode CustomerSkinMode { get; set; } = AppearanceSkinMode.Off;
    internal AppearanceGenderFilter CustomerSkinGenderFilter { get; set; } = AppearanceGenderFilter.Both;
    internal AppearanceSkinMode WorkerSkinMode { get; set; } = AppearanceSkinMode.Off;
    internal AppearanceGenderFilter WorkerSkinGenderFilter { get; set; } = AppearanceGenderFilter.Both;
    internal AppearanceSkinMode PlayerSkinMode { get; set; } = AppearanceSkinMode.Off;
    internal AppearanceGenderFilter PlayerSkinGenderFilter { get; set; } = AppearanceGenderFilter.Both;
    internal bool MrBurnsMode { get; set; }
    internal bool DebugMapPopulationEvery10Seconds { get; set; }
    internal int GeneratedWorkerCount { get; set; } = 7;
}

internal static class AppearanceShuffleRules
{
    internal static bool IsEnabled(AppearanceShuffleOptions options, AppearanceShuffleTarget target, AppearanceShufflePart part)
    {
        return (target, part) switch
        {
            (AppearanceShuffleTarget.Customers, AppearanceShufflePart.Shirt) => options.CustomerShirt,
            (AppearanceShuffleTarget.Customers, AppearanceShufflePart.Pants) => options.CustomerPants,
            (AppearanceShuffleTarget.Customers, AppearanceShufflePart.Shoes) => options.CustomerShoes,
            (AppearanceShuffleTarget.Customers, AppearanceShufflePart.Hair) => options.CustomerHair,
            (AppearanceShuffleTarget.Workers, AppearanceShufflePart.Shirt) => options.WorkerShirt,
            (AppearanceShuffleTarget.Workers, AppearanceShufflePart.Pants) => options.WorkerPants,
            (AppearanceShuffleTarget.Workers, AppearanceShufflePart.Shoes) => options.WorkerShoes,
            (AppearanceShuffleTarget.Workers, AppearanceShufflePart.Hair) => options.WorkerHair,
            _ => false
        };
    }

    internal static bool AnyEnabled(AppearanceShuffleOptions options, AppearanceShuffleTarget target)
    {
        if (target == AppearanceShuffleTarget.Customers && !options.EnableCustomerAppearancePatch)
        {
            return false;
        }

        if (target == AppearanceShuffleTarget.Workers && !options.EnableWorkerAppearancePatch)
        {
            return false;
        }

        return IsEnabled(options, target, AppearanceShufflePart.Shirt)
            || IsEnabled(options, target, AppearanceShufflePart.Pants)
            || IsEnabled(options, target, AppearanceShufflePart.Shoes)
            || IsEnabled(options, target, AppearanceShufflePart.Hair);
    }

    internal static AppearanceSkinMode GetSkinMode(AppearanceShuffleOptions options, AppearanceShuffleTarget target)
    {
        return target switch
        {
            AppearanceShuffleTarget.Customers => options.CustomerSkinMode,
            AppearanceShuffleTarget.Workers => options.WorkerSkinMode,
            _ => AppearanceSkinMode.Off
        };
    }

    internal static AppearanceGenderFilter GetSkinGenderFilter(AppearanceShuffleOptions options, AppearanceShuffleTarget target)
    {
        return target switch
        {
            AppearanceShuffleTarget.Customers => options.CustomerSkinGenderFilter,
            AppearanceShuffleTarget.Workers => options.WorkerSkinGenderFilter,
            _ => AppearanceGenderFilter.Both
        };
    }
}

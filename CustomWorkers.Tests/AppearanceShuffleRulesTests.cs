using Xunit;

namespace CustomWorkers.Tests;

public sealed class AppearanceShuffleRulesTests
{
    [Fact]
    public void IsEnabled_KeepsCustomerAndWorkerTogglesIndependent()
    {
        var options = new AppearanceShuffleOptions
        {
            CustomerShirt = true,
            CustomerPants = false,
            CustomerShoes = true,
            CustomerHair = false,
            WorkerShirt = false,
            WorkerPants = true,
            WorkerShoes = false,
            WorkerHair = true
        };

        Assert.True(AppearanceShuffleRules.IsEnabled(options, AppearanceShuffleTarget.Customers, AppearanceShufflePart.Shirt));
        Assert.False(AppearanceShuffleRules.IsEnabled(options, AppearanceShuffleTarget.Customers, AppearanceShufflePart.Pants));
        Assert.False(AppearanceShuffleRules.IsEnabled(options, AppearanceShuffleTarget.Workers, AppearanceShufflePart.Shirt));
        Assert.True(AppearanceShuffleRules.IsEnabled(options, AppearanceShuffleTarget.Workers, AppearanceShufflePart.Hair));
    }

    [Fact]
    public void AnyEnabled_ReturnsFalseWhenEveryToggleForTargetIsOff()
    {
        var options = new AppearanceShuffleOptions
        {
            CustomerShirt = false,
            CustomerPants = false,
            CustomerShoes = false,
            CustomerHair = false
        };

        Assert.False(AppearanceShuffleRules.AnyEnabled(options, AppearanceShuffleTarget.Customers));
        Assert.True(AppearanceShuffleRules.AnyEnabled(options, AppearanceShuffleTarget.Workers));
    }

    [Fact]
    public void GetSkinSettings_ReturnPerTargetValues()
    {
        var options = new AppearanceShuffleOptions
        {
            CustomerSkinMode = AppearanceSkinMode.Simpsons,
            CustomerSkinGenderFilter = AppearanceGenderFilter.GirlsOnly,
            WorkerSkinMode = AppearanceSkinMode.Random,
            WorkerSkinGenderFilter = AppearanceGenderFilter.BoysOnly
        };

        Assert.Equal(AppearanceSkinMode.Simpsons, AppearanceShuffleRules.GetSkinMode(options, AppearanceShuffleTarget.Customers));
        Assert.Equal(AppearanceGenderFilter.GirlsOnly, AppearanceShuffleRules.GetSkinGenderFilter(options, AppearanceShuffleTarget.Customers));
        Assert.Equal(AppearanceSkinMode.Random, AppearanceShuffleRules.GetSkinMode(options, AppearanceShuffleTarget.Workers));
        Assert.Equal(AppearanceGenderFilter.BoysOnly, AppearanceShuffleRules.GetSkinGenderFilter(options, AppearanceShuffleTarget.Workers));
    }
}

using Xunit;

namespace CustomWorkers.Tests;

public sealed class MrBurnsRulesTests
{
    [Fact]
    public void OverrideMoneyCost_ReturnsZeroWhenEnabled()
    {
        Assert.Equal(0f, MrBurnsRules.OverrideMoneyCost(true, 2000f));
        Assert.Equal(2000f, MrBurnsRules.OverrideMoneyCost(false, 2000f));
    }

    [Fact]
    public void OverrideRequiredLevel_ReturnsZeroWhenEnabled()
    {
        Assert.Equal(0, MrBurnsRules.OverrideRequiredLevel(true, 8));
        Assert.Equal(8, MrBurnsRules.OverrideRequiredLevel(false, 8));
    }
}

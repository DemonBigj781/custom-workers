using Xunit;

namespace CustomWorkers.Tests;

public sealed class ScrollStabilizationRulesTests
{
    [Fact]
    public void GetRefreshPassCount_UsesMultiplePassesWhenPanelsGrow()
    {
        Assert.Equal(3, ScrollStabilizationRules.GetRefreshPassCount(8, 15));
    }

    [Fact]
    public void GetRefreshPassCount_UsesSinglePassWhenPanelCountIsStable()
    {
        Assert.Equal(1, ScrollStabilizationRules.GetRefreshPassCount(15, 15));
    }
}

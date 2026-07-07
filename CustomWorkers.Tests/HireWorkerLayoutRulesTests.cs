using Xunit;

namespace CustomWorkers.Tests;

public sealed class HireWorkerLayoutRulesTests
{
    [Fact]
    public void NeedsLayoutRefresh_IsTrueWhenHirePanelsExpanded()
    {
        Assert.True(HireWorkerLayoutRules.NeedsLayoutRefresh(8, 10));
    }

    [Fact]
    public void NeedsLayoutRefresh_IsFalseWhenPanelCountUnchanged()
    {
        Assert.False(HireWorkerLayoutRules.NeedsLayoutRefresh(10, 10));
    }
}

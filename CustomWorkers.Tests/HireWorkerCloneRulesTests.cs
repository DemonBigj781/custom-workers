using Xunit;

namespace CustomWorkers.Tests;

public sealed class HireWorkerCloneRulesTests
{
    [Fact]
    public void GetVisibleWorkerCount_LeavesCloneOnExpandedRoster()
    {
        Assert.Equal(15, HireWorkerCloneRules.GetVisibleWorkerCount(isCloneScreen: true, totalWorkerCount: 15));
    }

    [Fact]
    public void GetVisibleWorkerCount_ClampsOriginalScreenToVanillaCount()
    {
        Assert.Equal(HireWorkerLayoutRules.VanillaHireWorkerPanelCount, HireWorkerCloneRules.GetVisibleWorkerCount(isCloneScreen: false, totalWorkerCount: 15));
        Assert.Equal(6, HireWorkerCloneRules.GetVisibleWorkerCount(isCloneScreen: false, totalWorkerCount: 6));
    }
}

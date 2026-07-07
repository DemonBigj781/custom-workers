using Xunit;

namespace CustomWorkers.Tests;

public sealed class HireWorkerNavigationRulesTests
{
    [Fact]
    public void GetPanelNeighbors_ReturnsNextPanelInsteadOfBouncingAtFormerLimit()
    {
        var neighbors = HireWorkerNavigationRules.GetPanelNeighbors(10, 8);

        Assert.Equal(7, neighbors.up);
        Assert.Equal(9, neighbors.down);
    }

    [Fact]
    public void GetPanelNeighbors_LastPanelHasNoDownNeighbor()
    {
        var neighbors = HireWorkerNavigationRules.GetPanelNeighbors(10, 9);

        Assert.Equal(8, neighbors.up);
        Assert.Null(neighbors.down);
    }
}

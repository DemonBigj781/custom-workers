using Xunit;

namespace CustomWorkers.Tests;

public sealed class ControllerRowRulesTests
{
    [Fact]
    public void NeedsAdditionalRows_IsTrueWhenPanelsExceedExistingControllerRows()
    {
        Assert.True(ControllerRowRules.NeedsAdditionalRows(8, 10));
    }

    [Fact]
    public void NeedsAdditionalRows_IsFalseWhenRowsAlreadyCoverPanels()
    {
        Assert.False(ControllerRowRules.NeedsAdditionalRows(10, 10));
    }

    [Fact]
    public void GetMissingRowCount_ReturnsExactShortfall()
    {
        Assert.Equal(2, ControllerRowRules.GetMissingRowCount(8, 10));
        Assert.Equal(0, ControllerRowRules.GetMissingRowCount(10, 10));
    }
}

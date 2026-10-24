using System.Linq;
using CustomWorkers;
using Xunit;

namespace CustomWorkers.Tests;

public sealed class WorkerTaskBoardRosterRulesTests
{
    [Fact]
    public void MissingHiredWorkersRetainVanillaAndAddOnlyNewIndexes()
    {
        var missing = WorkerTaskBoardRosterRules.GetMissingHiredWorkerIndexes(
            new[] { 0, 1, 2, 2, 3 },
            new[] { 0, 2, 3 },
            new[] { 0, 1 });

        Assert.Equal(new[] { 2, 3 }, missing.ToArray());
    }

    [Fact]
    public void UnhiredWorkersAreNotAdded()
    {
        var missing = WorkerTaskBoardRosterRules.GetMissingHiredWorkerIndexes(
            new[] { 4, 5 },
            new[] { 5 },
            new[] { 0 });

        Assert.Equal(new[] { 5 }, missing.ToArray());
    }
}

using Xunit;

namespace CustomWorkers.Tests;

public sealed class WorkerRosterRulesTests
{
    [Fact]
    public void CreateGeneratedWorkerData_IsDeterministicForSeed1337()
    {
        var template = new WorkerData
        {
            name = "Base Worker",
            restockSpeed = 1.5f,
            checkoutSpeed = 1.75f,
            walkSpeedMultiplier = 0.15f,
            costPerDay = 120f,
            hiringCost = 600f,
            arriveEarlySpeedMin = 0.1f,
            arriveEarlySpeedMax = 0.2f,
            shopLevelRequired = 12,
            description = "desc",
            bonusConversation = "bonus",
            goBackOnTime = true,
            prologueShow = false
        };

        var usedNamesA = WorkerRosterRules.CollectUsedNames(new[] { template });
        var usedNamesB = WorkerRosterRules.CollectUsedNames(new[] { template });
        var usedFirstNamesA = WorkerRosterRules.CollectUsedFirstNames(new[] { template });
        var usedFirstNamesB = WorkerRosterRules.CollectUsedFirstNames(new[] { template });
        var usedLastNamesA = WorkerRosterRules.CollectUsedLastNames(new[] { template });
        var usedLastNamesB = WorkerRosterRules.CollectUsedLastNames(new[] { template });

        WorkerData generatedA = WorkerRosterRules.CreateGeneratedWorkerData(template, 8, 0, isFemale: true, usedNamesA, usedFirstNamesA, usedLastNamesA);
        WorkerData generatedB = WorkerRosterRules.CreateGeneratedWorkerData(template, 8, 0, isFemale: true, usedNamesB, usedFirstNamesB, usedLastNamesB);

        Assert.Equal(1337, WorkerRosterRules.Seed);
        Assert.Equal(generatedA.name, generatedB.name);
        Assert.Contains(" ", generatedA.name);
        Assert.Equal(generatedA.restockSpeed, generatedB.restockSpeed);
        Assert.Equal(generatedA.checkoutSpeed, generatedB.checkoutSpeed);
        Assert.Equal(generatedA.walkSpeedMultiplier, generatedB.walkSpeedMultiplier);
        Assert.Equal(generatedA.costPerDay, generatedB.costPerDay);
        Assert.Equal(generatedA.hiringCost, generatedB.hiringCost);
    }

    [Fact]
    public void CreateGeneratedWorkerData_UsesEachNameOnce()
    {
        var template = new WorkerData
        {
            name = "Base Worker",
            restockSpeed = 1.5f,
            checkoutSpeed = 1.75f,
            walkSpeedMultiplier = 0.15f,
            costPerDay = 120f,
            hiringCost = 600f,
            arriveEarlySpeedMin = 0.1f,
            arriveEarlySpeedMax = 0.2f,
            shopLevelRequired = 12,
            description = "desc",
            bonusConversation = "bonus",
            goBackOnTime = true,
            prologueShow = false
        };

        var existingWorkers = new[]
        {
            new WorkerData { name = "Bertha" },
            new WorkerData { name = "Dolly" }
        };
        var usedNames = WorkerRosterRules.CollectUsedNames(existingWorkers);
        var usedFirstNames = WorkerRosterRules.CollectUsedFirstNames(existingWorkers);
        var usedLastNames = WorkerRosterRules.CollectUsedLastNames(existingWorkers);

        WorkerData generatedA = WorkerRosterRules.CreateGeneratedWorkerData(template, 8, 0, isFemale: true, usedNames, usedFirstNames, usedLastNames);
        WorkerData generatedB = WorkerRosterRules.CreateGeneratedWorkerData(template, 9, 1, isFemale: true, usedNames, usedFirstNames, usedLastNames);

        Assert.NotEqual(generatedA.name, generatedB.name);
        Assert.DoesNotContain(generatedA.name, new[] { "Bertha", "Dolly", "Bertha Wang", "Dolly Wang" });
        Assert.DoesNotContain(generatedB.name, new[] { "Bertha", "Dolly", generatedA.name, "Bertha Wang", "Dolly Wang" });
    }

    [Fact]
    public void GeneratedWorkerCount_CoversCombinedDistinctFirstNamePool()
    {
        Assert.Equal(7, WorkerRosterRules.GetGeneratedWorkerCount(new[] { new WorkerData { name = "Base Worker" } }));
    }

    [Theory]
    [InlineData(9, 17, 9, 17)]
    [InlineData(26, 0, 9, 17)]
    [InlineData(9, 0, 9, 0)]
    public void WorkerSaveDataCapacity_CoversEveryRuntimeWorker(
        int runtimeWorkerCount,
        int generatedWorkerCount,
        int existingSaveDataCount,
        int expectedEntriesToAdd)
    {
        Assert.Equal(
            expectedEntriesToAdd,
            WorkerRosterRules.GetWorkerSaveDataEntriesToAdd(runtimeWorkerCount, generatedWorkerCount, existingSaveDataCount));
    }
}

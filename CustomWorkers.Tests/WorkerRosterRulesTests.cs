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

        WorkerData generatedA = WorkerRosterRules.CreateGeneratedWorkerData(template, 8, 0, isFemale: true, usedNamesA);
        WorkerData generatedB = WorkerRosterRules.CreateGeneratedWorkerData(template, 8, 0, isFemale: true, usedNamesB);

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

        var usedNames = WorkerRosterRules.CollectUsedNames(new[]
        {
            new WorkerData { name = "Bertha" },
            new WorkerData { name = "Dolly" }
        });

        WorkerData generatedA = WorkerRosterRules.CreateGeneratedWorkerData(template, 8, 0, isFemale: true, usedNames);
        WorkerData generatedB = WorkerRosterRules.CreateGeneratedWorkerData(template, 9, 1, isFemale: true, usedNames);

        Assert.NotEqual(generatedA.name, generatedB.name);
        Assert.DoesNotContain(generatedA.name, new[] { "Bertha", "Dolly", "Bertha Wang", "Dolly Wang" });
        Assert.DoesNotContain(generatedB.name, new[] { "Bertha", "Dolly", generatedA.name, "Bertha Wang", "Dolly Wang" });
    }

    [Fact]
    public void GeneratedWorkerCount_CoversCombinedDistinctFirstNamePool()
    {
        Assert.Equal(7, WorkerRosterRules.GetGeneratedWorkerCount());
    }
}

using Xunit;

namespace CustomWorkers.Tests;

public sealed class GeneratedWorkerLogRulesTests
{
    [Fact]
    public void BuildJson_CapturesGeneratedWorkerFields()
    {
        var entries = new[]
        {
            new GeneratedWorkerLogEntry(
                workerIndex: 8,
                generatedSlotIndex: 0,
                name: "Arthur Lin",
                isFemale: false,
                characterModelIndex: 4,
                characterScale: (0.97f, 1.08f, 1.01f),
                hairColor: (32, 23, 17, 255),
                costPerDay: 50f,
                hiringCost: 2000f,
                restockSpeed: 1.23f,
                checkoutSpeed: 1.34f,
                walkSpeedMultiplier: 0.21f,
                description: "Keeps a paper trail.",
                bonusConversation: "Trusts the scanner.")
        };

        string json = GeneratedWorkerLogRules.BuildJson(entries);

        Assert.Contains("\"workerIndex\":8", json);
        Assert.Contains("\"name\":\"Arthur Lin\"", json);
        Assert.Contains("\"isFemale\":false", json);
        Assert.Contains("\"characterModelIndex\":4", json);
        Assert.Contains("\"x\":0.97", json);
        Assert.Contains("\"hairColor\":{\"r\":32,\"g\":23,\"b\":17,\"a\":255}", json);
        Assert.Contains("\"description\":\"Keeps a paper trail.\"", json);
        Assert.Contains("\"bonusConversation\":\"Trusts the scanner.\"", json);
    }

    [Fact]
    public void BuildJson_EscapesQuotesAndNewlines()
    {
        var entries = new[]
        {
            new GeneratedWorkerLogEntry(
                workerIndex: 9,
                generatedSlotIndex: 1,
                name: "Alex \"Ace\" Lin",
                isFemale: true,
                characterModelIndex: 7,
                characterScale: (1f, 1f, 1f),
                hairColor: (122, 122, 118, 255),
                costPerDay: 100f,
                hiringCost: 2000f,
                restockSpeed: 1.1f,
                checkoutSpeed: 1.2f,
                walkSpeedMultiplier: 0.15f,
                description: "Line one\nLine two",
                bonusConversation: "Says \"hi\".")
        };

        string json = GeneratedWorkerLogRules.BuildJson(entries);

        Assert.Contains("Alex \\\"Ace\\\" Lin", json);
        Assert.Contains("Line one\\nLine two", json);
        Assert.Contains("Says \\\"hi\\\".", json);
    }
}

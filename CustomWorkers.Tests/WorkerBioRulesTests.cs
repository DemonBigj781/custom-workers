using Xunit;

namespace CustomWorkers.Tests;

public sealed class WorkerBioRulesTests
{
    [Fact]
    public void BuildRawDescription_IsDeterministic()
    {
        string a = WorkerBioRules.BuildRawDescription(8, isFemale: true);
        string b = WorkerBioRules.BuildRawDescription(8, isFemale: true);

        Assert.Equal(a, b);
        Assert.StartsWith(WorkerBioRules.RawTextPrefix, a);
        Assert.NotEmpty(WorkerBioRules.StripRawPrefix(a));
    }

    [Fact]
    public void BuildRawBonusConversation_IsDeterministic()
    {
        string a = WorkerBioRules.BuildRawBonusConversation(8, isFemale: false);
        string b = WorkerBioRules.BuildRawBonusConversation(8, isFemale: false);

        Assert.Equal(a, b);
        Assert.StartsWith(WorkerBioRules.RawTextPrefix, a);
    }

    [Fact]
    public void StripRawPrefix_RemovesMarker()
    {
        string raw = WorkerBioRules.RawTextPrefix + "I challenged a thunderstorm to a rematch.";
        Assert.Equal("I challenged a thunderstorm to a rematch.", WorkerBioRules.StripRawPrefix(raw));
    }
}

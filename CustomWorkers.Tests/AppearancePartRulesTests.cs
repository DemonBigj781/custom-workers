using Xunit;

namespace CustomWorkers.Tests;

public sealed class AppearancePartRulesTests
{
    [Theory]
    [InlineData("Shirt", 0)]
    [InlineData("Upper Top", 0)]
    [InlineData("Jeans", 1)]
    [InlineData("Shorts", 1)]
    [InlineData("Boots", 2)]
    [InlineData("Sneakers", 2)]
    public void TryGetClothingPart_ClassifiesKnownClothingGroups(string label, int expected)
    {
        Assert.True(AppearancePartRules.TryGetClothingPart(label, out AppearanceShufflePart part));
        Assert.Equal((AppearanceShufflePart)expected, part);
    }

    [Theory]
    [InlineData("Hair_Main")]
    [InlineData("Eyebrow_L")]
    [InlineData("Mustache")]
    public void IsHairObjectName_RecognizesHairRelatedObjects(string objectName)
    {
        Assert.True(AppearancePartRules.IsHairObjectName(objectName));
    }

    [Theory]
    [InlineData("Gloves")]
    [InlineData("")]
    [InlineData(null)]
    public void TryGetClothingPart_RejectsUnknownLabels(string? label)
    {
        Assert.False(AppearancePartRules.TryGetClothingPart(label, out _));
    }
}

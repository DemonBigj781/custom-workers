using Xunit;

namespace CustomWorkers.Tests;

public sealed class GeneratedWorkerAppearanceRulesTests
{
    [Fact]
    public void GetCharacterName_UsesMalePrefixForMaleWorkers()
    {
        Assert.Equal("Male4", GeneratedWorkerAppearanceRules.GetCharacterName(isFemale: false, characterModelIndex: 4));
    }

    [Fact]
    public void GetCharacterName_UsesFemalePrefixForFemaleWorkers()
    {
        Assert.Equal("Female7", GeneratedWorkerAppearanceRules.GetCharacterName(isFemale: true, characterModelIndex: 7));
    }

    [Fact]
    public void ShouldApplyGeneratedCharacter_RequiresCustomizationInstance()
    {
        Assert.True(GeneratedWorkerAppearanceRules.ShouldApplyGeneratedCharacter(hasCharacterCustomization: true));
        Assert.False(GeneratedWorkerAppearanceRules.ShouldApplyGeneratedCharacter(hasCharacterCustomization: false));
    }
}

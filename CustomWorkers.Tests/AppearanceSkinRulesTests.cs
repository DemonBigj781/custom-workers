using UnityEngine;
using Xunit;

namespace CustomWorkers.Tests;

public sealed class AppearanceSkinRulesTests
{
    [Theory]
    [InlineData(0, true, true)]
    [InlineData(0, false, true)]
    [InlineData(1, true, false)]
    [InlineData(1, false, true)]
    [InlineData(2, true, true)]
    [InlineData(2, false, false)]
    public void MatchesGender_FiltersAsExpected(int filterValue, bool isFemale, bool expected)
    {
        Assert.Equal(expected, AppearanceSkinRules.MatchesGender(AppearanceSkinMode.Simpsons, (AppearanceGenderFilter)filterValue, isFemale));
    }

    [Fact]
    public void ResolveSkinColor_SimpsonsModeReturnsYellow()
    {
        Color color = AppearanceSkinRules.ResolveSkinColor(AppearanceSkinMode.Simpsons, new System.Random(1), Color.white);
        Assert.Equal(new Color32(255, 217, 15, 255), (Color32)color);
    }

    [Fact]
    public void ResolveSkinColor_RandomModeIsDeterministicPerSeed()
    {
        Color a = AppearanceSkinRules.ResolveSkinColor(AppearanceSkinMode.Random, new System.Random(7), Color.white);
        Color b = AppearanceSkinRules.ResolveSkinColor(AppearanceSkinMode.Random, new System.Random(7), Color.black);
        Assert.Equal(a, b);
    }

    [Fact]
    public void BlueMenMode_RejectsFemaleTargets()
    {
        Assert.False(AppearanceSkinRules.MatchesGender(AppearanceSkinMode.BlueMen, AppearanceGenderFilter.Both, isFemale: true));
        Assert.True(AppearanceSkinRules.MatchesGender(AppearanceSkinMode.BlueMen, AppearanceGenderFilter.Both, isFemale: false));
    }

    [Theory]
    [InlineData((int)AppearanceSkinMode.Eiffel65, true)]
    [InlineData((int)AppearanceSkinMode.BlueMen, true)]
    [InlineData((int)AppearanceSkinMode.FBI, true)]
    [InlineData((int)AppearanceSkinMode.Simpsons, false)]
    public void TriesToOverrideOtherFeatures_ReturnsExpectedModes(int modeValue, bool expected)
    {
        Assert.Equal(expected, AppearanceSkinRules.TriesToOverrideOtherFeatures((AppearanceSkinMode)modeValue));
    }

    [Fact]
    public void BlueMenMode_HidesHairAndForcesBlackStyle()
    {
        Assert.True(AppearanceSkinRules.HidesHair(AppearanceSkinMode.BlueMen));
        Assert.True(AppearanceSkinRules.TryGetForcedHairColor(AppearanceSkinMode.BlueMen, out Color hairColor));
        Assert.Equal(new Color32(0, 0, 0, 255), (Color32)hairColor);
        Assert.True(AppearanceSkinRules.TryGetForcedClothingColor(AppearanceSkinMode.BlueMen, AppearanceShufflePart.Shirt, out Color clothingColor));
        Assert.Equal(new Color32(0, 0, 0, 255), (Color32)clothingColor);
    }

    [Fact]
    public void Eiffel65Mode_ForcesBlueHairAndClothing()
    {
        Assert.True(AppearanceSkinRules.TryGetForcedHairColor(AppearanceSkinMode.Eiffel65, out Color hairColor));
        Assert.Equal(new Color32(0, 102, 255, 255), (Color32)hairColor);
        Assert.True(AppearanceSkinRules.TryGetForcedClothingColor(AppearanceSkinMode.Eiffel65, AppearanceShufflePart.Pants, out Color clothingColor));
        Assert.Equal(new Color32(0, 102, 255, 255), (Color32)clothingColor);
    }

    [Fact]
    public void FBIMode_UsesGlowOnly()
    {
        Assert.True(AppearanceSkinRules.AppliesGreenGlow(AppearanceSkinMode.FBI));
        Assert.False(AppearanceSkinRules.TryGetForcedHairColor(AppearanceSkinMode.FBI, out _));
        Assert.False(AppearanceSkinRules.TryGetForcedClothingColor(AppearanceSkinMode.FBI, AppearanceShufflePart.Shirt, out _));
    }
}

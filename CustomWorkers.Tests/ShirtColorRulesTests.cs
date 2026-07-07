using UnityEngine;
using Xunit;

namespace CustomWorkers.Tests;

public sealed class ShirtColorRulesTests
{
    [Theory]
    [InlineData("Shirt", true)]
    [InlineData("Upper Top", true)]
    [InlineData("Torso Apparel", true)]
    [InlineData("Pants", true)]
    [InlineData("Trousers", true)]
    [InlineData("Shoes", true)]
    [InlineData("Boots", true)]
    [InlineData("Sneakers", true)]
    [InlineData("Socks", true)]
    [InlineData("pants", true)]
    [InlineData("gloves", false)]
    [InlineData(null, false)]
    public void IsColorableClothingLabel_DetectsExpectedLabels(string? label, bool expected)
    {
        Assert.Equal(expected, ShirtColorRules.IsColorableClothingLabel(label));
    }

    [Fact]
    public void BuildTintSet_ReturnsFourOpaqueTints()
    {
        Color[] tints = ShirtColorRules.BuildTintSet(new Color(0.25f, 0.5f, 0.75f, 1f));

        Assert.Equal(4, tints.Length);
        foreach (Color tint in tints)
        {
            Assert.Equal(1f, tint.a);
        }

        Assert.NotEqual(tints[0], tints[1]);
        Assert.NotEqual(tints[1], tints[2]);
    }
}

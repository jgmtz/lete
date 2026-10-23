using Lete.Core.Presentation;

namespace Lete.Core.Tests;

public class PixelDensityTests
{
    private static PixelDensity Make(int spritePx) => new(spritePx, 1.75f, 192, 1080);

    [Theory]
    [InlineData(32, 6)]
    [InlineData(48, 4)]
    [InlineData(64, 3)]
    public void LowResShrink_MapsOneArtPixelToOneRenderedPixel(int spritePx, int expected)
    {
        Assert.Equal(expected, Make(spritePx).LowResShrink);
    }

    [Fact]
    public void LowResShrink_NonIntegerFactor_Throws()
    {
        var density = new PixelDensity(40, 1.75f, 192, 1080);

        Assert.Throws<InvalidOperationException>(() => density.LowResShrink);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(48)]
    [InlineData(64)]
    public void CameraFraming_DoesNotDependOnSpriteSize(int spritePx)
    {
        Assert.Equal(1.75f * 1080 / 192, Make(spritePx).CameraSize, precision: 4);
    }

    [Fact]
    public void SpriteHeightInMeters_IsCharacterHeight()
    {
        var density = Make(48);

        Assert.Equal(1.75f, density.PixelSize * 48, precision: 4);
    }

    [Fact]
    public void TriplanarScale_GivesSameTexelDensityAsSprites()
    {
        var density = Make(48);

        // Uma textura de 32 px repetida a cada (1 / escala) metros tem PixelsPerMeter texels por metro.
        var texelsPerMeter = 32 * density.TriplanarScale(32);

        Assert.Equal(density.PixelsPerMeter, texelsPerMeter, precision: 4);
    }
}

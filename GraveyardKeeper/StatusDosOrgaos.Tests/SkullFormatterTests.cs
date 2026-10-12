using StatusDosOrgaos.Core;
using Xunit;

namespace StatusDosOrgaos.Tests;

public class SkullFormatterTests
{
    [Fact]
    public void Icons_RedThenWhite_LikeTheGame()
    {
        Assert.Equal("+(rskull)(rskull)(rskull)(rskull), +(skull)", SkullFormatter.Icons(4, 1));
    }

    [Fact]
    public void Icons_OnlyRed()
    {
        Assert.Equal("+(rskull)(rskull)", SkullFormatter.Icons(2, 0));
    }

    [Fact]
    public void Icons_OnlyWhite()
    {
        Assert.Equal("+(skull)(skull)(skull)", SkullFormatter.Icons(0, 3));
    }

    [Fact]
    public void Icons_NegativeValuesUseMinusSign()
    {
        Assert.Equal("-(rskull)(rskull), +(skull)(skull)(skull)", SkullFormatter.Icons(-2, 3));
    }

    [Fact]
    public void Icons_ZeroZeroIsEmpty()
    {
        Assert.Equal("", SkullFormatter.Icons(0, 0));
    }

    [Fact]
    public void Summary_ShowsBothNumbers()
    {
        Assert.Equal("(rskull) Vermelhas: 4   (skull) Brancas: 1", SkullFormatter.Summary(4, 1));
    }

    [Fact]
    public void Summary_ShowsZeros()
    {
        Assert.Equal("(rskull) Vermelhas: 0   (skull) Brancas: 0", SkullFormatter.Summary(0, 0));
    }

    [Fact]
    public void Summary_ShowsNegatives()
    {
        Assert.Equal("(rskull) Vermelhas: -1   (skull) Brancas: 3", SkullFormatter.Summary(-1, 3));
    }
}

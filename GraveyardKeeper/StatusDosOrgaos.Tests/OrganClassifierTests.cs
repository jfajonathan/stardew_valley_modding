using StatusDosOrgaos.Core;
using Xunit;

namespace StatusDosOrgaos.Tests;

public class OrganClassifierTests
{
    private const string BodyPart = "BodyUniversalPart";

    [Theory]
    [InlineData("heart:heart_4_1")]
    [InlineData("brain:brain_-2_3")]
    [InlineData("intestine:intestine_0_0")]
    [InlineData("lungs_dark")]
    [InlineData("heart_dark")]
    [InlineData("eye")]
    [InlineData("tongue")]
    [InlineData("scalp")]
    [InlineData("muscle")]
    public void Organs_AreAccepted(string id)
    {
        Assert.True(OrganClassifier.IsOrgan(id, BodyPart, OrganScope.Orgaos));
    }

    [Theory]
    [InlineData("flesh", BodyPart)]
    [InlineData("blood", BodyPart)]
    [InlineData("skull", BodyPart)]
    [InlineData("bone", BodyPart)]
    [InlineData("tr_bellas_ring", BodyPart)]
    [InlineData("heart:heart_4_1", "None")]
    [InlineData("axe", "Axe")]
    [InlineData(null, BodyPart)]
    [InlineData("", BodyPart)]
    public void NonOrgans_AreRejected(string id, string type)
    {
        Assert.False(OrganClassifier.IsOrgan(id, type, OrganScope.Orgaos));
    }

    [Fact]
    public void AllBodyPartsScope_AcceptsFleshButNotOtherTypes()
    {
        Assert.True(OrganClassifier.IsOrgan("flesh", BodyPart, OrganScope.TodasPartesDoCorpo));
        Assert.False(OrganClassifier.IsOrgan("axe", "Axe", OrganScope.TodasPartesDoCorpo));
    }
}

using System.Text.RegularExpressions;
using Xunit;

namespace FiwareNet.Tests;

public class RegexConverterTests : FiwareTestFixtureClass
{
    public RegexConverterTests(FiwareTestFixture fixture) : base(fixture) { }

    #region serialize
    [Fact]
    public async void RegexConverter_Serialize()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(RegexConverter_Serialize),
            Type = Fixture.EntityType,
            RegexValue = new Regex(@"^\s.*[^A-Z0-9]{2,3}(?:.*)?(?<name>\s|\w)+(?=abc)(?!abc)$")
        };

        var res = await client.CreateEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(entity.RegexValue.ToString(), check[nameof(UnitTestEntity.RegexValue)]?.Value<string>("value"));
    }
    #endregion

    #region deserialize
    [Fact]
    public async void RegexConverter_Deserialize()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(RegexConverter_Deserialize),
            Type = Fixture.EntityType,
            RegexValue = new Regex(@"^\s.*[^A-Z0-9]{2,3}(?:.*)?(?<name>\s|\w)+(?=abc)(?!abc)$")
        };

        await client.CreateEntity(entity);
        var (res, getEntity) = await client.GetEntity<UnitTestEntity>(entity.Id);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(getEntity);
        Assert.Equal(entity.RegexValue.ToString(), getEntity.RegexValue.ToString());
    }
    #endregion

    #region test classes
    private class UnitTestEntity : EntityBase
    {
        [FiwareType(FiwareTypes.TextUnrestricted)]
        public Regex RegexValue { get; init; } = null!;
    }
    #endregion
}
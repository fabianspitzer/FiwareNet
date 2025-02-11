using Xunit;
using FiwareNet.Utils;

namespace FiwareNet.Tests;

public class DateTimeConverterTests : FiwareTestFixtureClass
{
    public DateTimeConverterTests(FiwareTestFixture fixture) : base(fixture) { }

    #region serialize
    [Fact]
    public async void DateTimeConverter_SerializeLocal()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(DateTimeConverter_SerializeLocal),
            Type = Fixture.EntityType,
            DateTimeValue = DateTime.Now
        };

        var res = await client.CreateEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        var dt2 = check[nameof(UnitTestEntity.DateTimeValue)]!.Value<DateTime>("value")!;
        Assert.True(entity.DateTimeValue.Equals(dt2, DateTimePrecision.Millisecond), $"{entity.DateTimeValue:o} <-> {dt2:o}");
    }

    [Fact]
    public async void DateTimeConverter_SerializeUniversal()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(DateTimeConverter_SerializeUniversal),
            Type = Fixture.EntityType,
            DateTimeValue = DateTime.UtcNow
        };

        var res = await client.CreateEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        var dt2 = check[nameof(UnitTestEntity.DateTimeValue)]!.Value<DateTime>("value")!;
        Assert.True(entity.DateTimeValue.Equals(dt2, DateTimePrecision.Millisecond), $"{entity.DateTimeValue:o} <-> {dt2:o}");
    }
    #endregion

    #region deserialize
    [Fact]
    public async void DateTimeConverter_DeserializeLocal()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(DateTimeConverter_DeserializeLocal),
            Type = Fixture.EntityType,
            DateTimeValue = DateTime.Now
        };

        await client.CreateEntity(entity);
        var (res, getEntity) = await client.GetEntity<UnitTestEntity>(entity.Id);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(getEntity);
        Assert.True(entity.DateTimeValue.Equals(getEntity.DateTimeValue, DateTimePrecision.Millisecond), $"{entity.DateTimeValue:o} <-> {getEntity.DateTimeValue:o}");
    }

    [Fact]
    public async void DateTimeConverter_DeserializeUniversal()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(DateTimeConverter_DeserializeUniversal),
            Type = Fixture.EntityType,
            DateTimeValue = DateTime.UtcNow
        };

        await client.CreateEntity(entity);
        var (res, getEntity) = await client.GetEntity<UnitTestEntity>(entity.Id);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(getEntity);
        Assert.True(entity.DateTimeValue.Equals(getEntity.DateTimeValue, DateTimePrecision.Millisecond), $"{entity.DateTimeValue:o} <-> {getEntity.DateTimeValue:o}");
    }
    #endregion

    #region test classes
    private class UnitTestEntity : EntityBase
    {
        public DateTime DateTimeValue { get; init; }
    }
    #endregion
}
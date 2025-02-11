using Xunit;
using FiwareNet.Utils;

namespace FiwareNet.Tests;

public class NullableDateTimeConverterTests : FiwareTestFixtureClass
{
    public NullableDateTimeConverterTests(FiwareTestFixture fixture) : base(fixture) { }

    #region serialize
    [Fact]
    public async void NullableDateTimeConverter_SerializeNull()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(NullableDateTimeConverter_SerializeNull),
            Type = Fixture.EntityType,
            DateTimeValue = null
        };

        var res = await client.CreateEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Null(check[nameof(UnitTestEntity.DateTimeValue)]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void NullableDateTimeConverter_SerializeLocal()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(NullableDateTimeConverter_SerializeLocal),
            Type = Fixture.EntityType,
            DateTimeValue = DateTime.Now
        };

        var res = await client.CreateEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        var dt2 = check[nameof(UnitTestEntity.DateTimeValue)]!.Value<DateTime>("value")!;
        Assert.True(entity.DateTimeValue.Value.Equals(dt2, DateTimePrecision.Millisecond), $"{entity.DateTimeValue:o} <-> {dt2:o}");
    }

    [Fact]
    public async void NullableDateTimeConverter_SerializeUniversal()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(NullableDateTimeConverter_SerializeUniversal),
            Type = Fixture.EntityType,
            DateTimeValue = DateTime.UtcNow
        };

        var res = await client.CreateEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        var dt2 = check[nameof(UnitTestEntity.DateTimeValue)]!.Value<DateTime>("value")!;
        Assert.True(entity.DateTimeValue.Value.Equals(dt2, DateTimePrecision.Millisecond), $"{entity.DateTimeValue:o} <-> {dt2:o}");
    }
    #endregion

    #region deserialize
    [Fact]
    public async void NullableDateTimeConverter_DeserializeNull()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(NullableDateTimeConverter_DeserializeNull),
            Type = Fixture.EntityType,
            DateTimeValue = null
        };

        await client.CreateEntity(entity);
        var (res, getEntity) = await client.GetEntity<UnitTestEntity>(entity.Id);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(getEntity);
        Assert.Null(getEntity.DateTimeValue);
    }

    [Fact]
    public async void NullableDateTimeConverter_DeserializeLocal()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(NullableDateTimeConverter_DeserializeLocal),
            Type = Fixture.EntityType,
            DateTimeValue = DateTime.Now
        };

        await client.CreateEntity(entity);
        var (res, getEntity) = await client.GetEntity<UnitTestEntity>(entity.Id);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(getEntity);
        Assert.True(entity.DateTimeValue.Value.Equals(getEntity.DateTimeValue!.Value, DateTimePrecision.Millisecond), $"{entity.DateTimeValue:o} <-> {getEntity.DateTimeValue:o}");
    }

    [Fact]
    public async void NullableDateTimeConverter_DeserializeUniversal()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);
        var entity = new UnitTestEntity
        {
            Id = nameof(NullableDateTimeConverter_DeserializeUniversal),
            Type = Fixture.EntityType,
            DateTimeValue = DateTime.UtcNow
        };

        await client.CreateEntity(entity);
        var (res, getEntity) = await client.GetEntity<UnitTestEntity>(entity.Id);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(getEntity);
        Assert.True(entity.DateTimeValue.Value.Equals(getEntity.DateTimeValue!.Value, DateTimePrecision.Millisecond), $"{entity.DateTimeValue:o} <-> {getEntity.DateTimeValue:o}");
    }
    #endregion

    #region test classes
    private class UnitTestEntity : EntityBase
    {
        public DateTime? DateTimeValue { get; init; }
    }
    #endregion
}
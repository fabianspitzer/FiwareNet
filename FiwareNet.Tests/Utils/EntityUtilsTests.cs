using Xunit;
using FiwareNet.Utils;

namespace FiwareNet.Tests;

public class EntityUtilsTests
{
    #region IsEntity
    [Fact]
    public void IsEntity_NullObject()
    {
        var res = EntityUtils.IsEntity((object?) null);

        Assert.False(res);
    }

    [Fact]
    public void IsEntity_NullType()
    {
        Assert.Throws<ArgumentNullException>(() => EntityUtils.IsEntity(null));
    }

    [Fact]
    public void IsEntity_EntityBase()
    {
        var entity = new TestEntity();

        var res = EntityUtils.IsEntity(entity);

        Assert.True(res);
    }

    [Fact]
    public void IsEntity_EntityBaseGeneric()
    {
        var res = EntityUtils.IsEntity<EntityBase>();

        Assert.True(res);
    }

    [Fact]
    public void IsEntity_EntityBaseType()
    {
        var res = EntityUtils.IsEntity(typeof(EntityBase));

        Assert.True(res);
    }

    [Fact]
    public void IsEntity_DynamicEntity()
    {
        var entity = new DynamicEntity();

        var res = EntityUtils.IsEntity(entity);

        Assert.True(res);
    }

    [Fact]
    public void IsEntity_DynamicEntityGeneric()
    {
        var res = EntityUtils.IsEntity<DynamicEntity>();

        Assert.True(res);
    }

    [Fact]
    public void IsEntity_DynamicEntityType()
    {
        var res = EntityUtils.IsEntity(typeof(DynamicEntity));

        Assert.True(res);
    }

    [Fact]
    public void IsEntity_CustomEntity()
    {
        var entity = new CustomEntity();

        var res = EntityUtils.IsEntity(entity);

        Assert.True(res);
    }

    [Fact]
    public void IsEntity_CustomEntityGeneric()
    {
        var res = EntityUtils.IsEntity<CustomEntity>();

        Assert.True(res);
    }

    [Fact]
    public void IsEntity_CustomEntityType()
    {
        var res = EntityUtils.IsEntity(typeof(CustomEntity));

        Assert.True(res);
    }

    [Fact]
    public void IsEntity_InvalidObject()
    {
        var entity = new Record();

        var res = EntityUtils.IsEntity(entity);

        Assert.False(res);
    }

    [Fact]
    public void IsEntity_InvalidGeneric()
    {
        var res = EntityUtils.IsEntity<Record>();

        Assert.False(res);
    }

    [Fact]
    public void IsEntity_InvalidType()
    {
        var res = EntityUtils.IsEntity(typeof(Record));

        Assert.False(res);
    }

    [Fact]
    public void IsEntity_InvalidCustomEntity()
    {
        var entity = new InvalidCustomEntity();

        var res = EntityUtils.IsEntity(entity);

        Assert.False(res);
    }

    [Fact]
    public void IsEntity_InvalidCustomEntityGeneric()
    {
        var res = EntityUtils.IsEntity<InvalidCustomEntity>();

        Assert.False(res);
    }

    [Fact]
    public void IsEntity_InvalidCustomEntityType()
    {
        var res = EntityUtils.IsEntity(typeof(InvalidCustomEntity));

        Assert.False(res);
    }
    #endregion

    #region GetEntityId
    [Fact]
    public void GetEntityId_NullObject()
    {
        Assert.Throws<ArgumentNullException>(() => EntityUtils.GetEntityId<object?>(null));
    }

    [Fact]
    public void GetEntityId_EntityBase()
    {
        var entity = new TestEntity {Id = "Test_ID"};

        var res = EntityUtils.GetEntityId(entity);

        Assert.Equal(entity.Id, res);
    }

    [Fact]
    public void GetEntityId_DynamicEntity()
    {
        var entity = new DynamicEntity {Id = "Test_ID"};

        var res = EntityUtils.GetEntityId(entity);

        Assert.Equal(entity.Id, res);
    }

    [Fact]
    public void GetEntityId_CustomEntity()
    {
        var entity = new CustomEntity {Id = "Test_ID"};

        var res = EntityUtils.GetEntityId(entity);

        Assert.Equal(entity.Id, res);
    }

    [Fact]
    public void GetEntityId_InvalidCustomEntity()
    {
        var entity = new InvalidCustomEntity {NotAnId = "Test_ID"};

        Assert.Throws<FiwareContractException>(() => EntityUtils.GetEntityId(entity));
    }
    #endregion

    #region GetEntityType
    [Fact]
    public void GetEntityType_NullObject()
    {
        Assert.Throws<ArgumentNullException>(() => EntityUtils.GetEntityType<object?>(null));
    }

    [Fact]
    public void GetEntityType_EntityBase()
    {
        var entity = new TestEntity {Type = "Test_Type"};

        var res = EntityUtils.GetEntityType(entity);

        Assert.Equal(entity.Type, res);
    }

    [Fact]
    public void GetEntityType_DynamicEntity()
    {
        var entity = new DynamicEntity {Type = "Test_Type"};

        var res = EntityUtils.GetEntityType(entity);

        Assert.Equal(entity.Type, res);
    }

    [Fact]
    public void GetEntityType_CustomEntity()
    {
        var entity = new CustomEntity {Type = "Test_Type"};

        var res = EntityUtils.GetEntityType(entity);

        Assert.Equal(entity.Type, res);
    }

    [Fact]
    public void GetEntityType_InvalidCustomEntity()
    {
        var entity = new InvalidCustomEntity {NotAType = "Test_Type"};

        Assert.Throws<FiwareContractException>(() => EntityUtils.GetEntityType(entity));
    }
    #endregion

    #region test classes
    private class TestEntity : EntityBase
    { }

    private class CustomEntity
    {
        [FiwareEntityId]
        public string? Id { get; init; }

        [FiwareEntityType]
        public string? Type { get; init; }
    }

    private class InvalidCustomEntity
    {
        public string? NotAnId { get; init; }

        public string? NotAType { get; init; }
    }
    #endregion
}
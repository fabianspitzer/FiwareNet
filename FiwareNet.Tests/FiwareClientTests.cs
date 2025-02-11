using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using FiwareNet.Encoders;
using Xunit;

namespace FiwareNet.Tests;

public sealed class FiwareClientTests : FiwareTestFixtureClass
{
    public FiwareClientTests(FiwareTestFixture fixture) : base(fixture) { }

    #region FiwareClient
    [Fact]
    public void FiwareClient_NullEndpoint()
    {
        Assert.Throws<ArgumentNullException>(() => new FiwareClient(null));
        Assert.Throws<ArgumentNullException>(() => new FiwareClient(string.Empty));
    }

    [Fact]
    public void FiwareClient_InvalidEndpoint()
    {
        Assert.Throws<ArgumentException>(() => new FiwareClient("invalid url"));
        Assert.Throws<ArgumentException>(() => new FiwareClient("http://invalid.version.url/v1"));
        Assert.Throws<ArgumentException>(() => new FiwareClient("http://invalid.version.url/v3"));
        Assert.Throws<ArgumentException>(() => new FiwareClient("http://invalid.version.url/v123"));
    }

    [Fact]
    public void FiwareClientIncompleteEndpoint()
    {
        var client1 = new FiwareClient("missing.protocol.url/v2/");
        var client2 = new FiwareClient("http://missing.slash.url/v2");
        var client3 = new FiwareClient("http://missing.version.url/");

        Assert.Equal("http://missing.protocol.url/v2/", client1.BrokerEndpoint);
        Assert.Equal("http://missing.slash.url/v2/", client2.BrokerEndpoint);
        Assert.Equal("http://missing.version.url/v2/", client3.BrokerEndpoint);
    }

    [Fact]
    public void FiwareClient_NullSettings()
    {
        Assert.Throws<ArgumentNullException>(() => new FiwareClient(Fixture.ContextBrokerAddress, null));
    }

    [Fact]
    public void FiwareClient_InvalidEntitiesPerRequest()
    {
        Assert.Throws<IndexOutOfRangeException>(() => new FiwareClient(Fixture.ContextBrokerAddress, new FiwareSettings {EntitiesPerRequest = 0}));
        Assert.Throws<IndexOutOfRangeException>(() => new FiwareClient(Fixture.ContextBrokerAddress, new FiwareSettings {EntitiesPerRequest = -1}));
    }

    [Fact]
    public void FiwareClient_BrokerEndpoint()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress);

        Assert.Equal(Fixture.ContextBrokerAddress, client.BrokerEndpoint);
    }

    //todo: constructor tests
    #endregion

    #region GetInfo
    [Fact]
    public async void GetInfo()
    {
        var client = NewClient();

        var (res, info) = await client.GetInfo();

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(info);
        Assert.NotEqual(default, info.Version);
        Assert.NotEqual(default, info.Uptime);
        Assert.NotNull(info.GitHash);
        Assert.NotEqual(default, info.CompileTime);
        Assert.NotNull(info.CompiledBy);
        Assert.NotNull(info.CompiledIn);
        Assert.NotEqual(default, info.ReleaseDate);
        Assert.NotNull(info.Machine);
        Assert.NotNull(info.Doc);
        Assert.NotNull(info.LibraryVersions);
    }
    #endregion

    #region CreateEntity
    [Fact]
    public async void CreateEntity_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateEntity((UnitTestEntity?) null));
    }

    [Fact]
    public async void CreateEntity_InvalidType()
    {
        var client = NewClient();
        var entity = new InvalidEntity();

        await Assert.ThrowsAsync<FiwareContractException>(() => client.CreateEntity(entity));
    }

    [Fact]
    public async void CreateEntity_MissingEntityId()
    {
        var client = NewClient();
        var entity = NewEntity();

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.CreateEntity(entity));
    }

    [Fact]
    public async void CreateEntity_MissingEntityType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateEntity_MissingEntityType), null);

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.CreateEntity(entity));
    }

    [Fact]
    public async void CreateEntity_ValidEntity()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateEntity_ValidEntity));

        var res = await client.CreateEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.Created, res.Code);
        Assert.True(await Fixture.EntityExists(entity.Id));
    }

    [Fact]
    public async void CreateEntity_EntityAlreadyExists()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateEntity_EntityAlreadyExists));

        var res1 = await client.CreateEntity(entity);
        var res2 = await client.CreateEntity(entity);

        Assert.True(res1.IsGood, res1.ErrorDescription);
        Assert.Equal(HttpStatusCode.Created, res1.Code);
        Assert.True(res2.IsBad, res2.Code.ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res2.Code);
    }
    #endregion

    #region CreateEntities
    [Fact]
    public async void CreateEntities_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateEntities((IEnumerable<UnitTestEntity>?) null));
    }

    [Fact]
    public async void CreateEntities_Empty()
    {
        var client = NewClient();
        var entities = Array.Empty<UnitTestEntity>();

        var res = await client.CreateEntities(entities);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
    }

    [Fact]
    public async void CreateEntities_NullEntity()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity?> {null};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateEntities(entities));
    }

    [Fact]
    public async void CreateEntities_InvalidType()
    {
        var client = NewClient();
        var entities = new List<InvalidEntity> {new(), new(), new()};

        await Assert.ThrowsAsync<FiwareContractException>(() => client.CreateEntities(entities));
    }

    [Fact]
    public async void CreateEntities_MissingEntityId()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity> {new(), new(), new()};

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.CreateEntities(entities));
    }

    [Fact]
    public async void CreateEntities_MissingEntityType()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(CreateEntities_MissingEntityType) + "1", null),
            NewEntity(nameof(CreateEntities_MissingEntityType) + "2", null),
            NewEntity(nameof(CreateEntities_MissingEntityType) + "3", null)
        };

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.CreateEntities(entities));
    }

    [Fact]
    public async void CreateEntities_ValidCollection()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(CreateEntities_ValidCollection) + "1"),
            NewEntity(nameof(CreateEntities_ValidCollection) + "2"),
            NewEntity(nameof(CreateEntities_ValidCollection) + "3")
        };

        var res = await client.CreateEntities(entities);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.True(await Fixture.EntityExists(entities[0].Id));
        Assert.True(await Fixture.EntityExists(entities[1].Id));
        Assert.True(await Fixture.EntityExists(entities[2].Id));
    }

    [Fact]
    public async void CreateEntities_DuplicateEntity()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(CreateEntities_DuplicateEntity)),
            NewEntity(nameof(CreateEntities_DuplicateEntity))
        };

        var res = await client.CreateEntities(entities);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.Code);
    }

    [Fact]
    public async void CreateEntities_EntitiesAlreadyExist()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateEntities_EntitiesAlreadyExist) + "1");
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(CreateEntities_EntitiesAlreadyExist) + "1"),
            NewEntity(nameof(CreateEntities_EntitiesAlreadyExist) + "2"),
            NewEntity(nameof(CreateEntities_EntitiesAlreadyExist) + "3")
        };

        await client.CreateEntity(entity);
        var res = await client.CreateEntities(entities);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.Code);
    }
    #endregion

    #region UpdateEntity
    [Fact]
    public async void UpdateEntity_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateEntity((UnitTestEntity?) null));
    }

    [Fact]
    public async void UpdateEntity_InvalidType()
    {
        var client = NewClient();
        var entity = new InvalidEntity();

        await Assert.ThrowsAsync<FiwareContractException>(() => client.UpdateEntity(entity));
    }

    [Fact]
    public async void UpdateEntity_MissingEntityId()
    {
        var client = NewClient();
        var entity = NewEntity();

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.CreateEntity(entity));
    }

    [Fact]
    public async void UpdateEntity_MissingEntityType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateEntity_MissingEntityType), null);

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.UpdateEntity(entity));
    }

    [Fact]
    public async void UpdateEntity_EntityNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateEntity_EntityNotFound));

        var res = await client.UpdateEntity(entity);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(entity.Id));
    }

    [Fact]
    public async void UpdateEntity_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateEntity_EntityFound));

        await client.CreateEntity(entity);
        entity.StringValue = "TestValue2";
        var res = await client.UpdateEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);

        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(entity.StringValue, check["StringValue"]!["value"]!.ToObject<string>());
    }
    #endregion

    #region UpdateEntities
    [Fact]
    public async void UpdateEntities_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateEntities((IEnumerable<UnitTestEntity>?) null));
    }

    [Fact]
    public async void UpdateEntities_Empty()
    {
        var client = NewClient();
        var entities = Array.Empty<UnitTestEntity>();

        var res = await client.UpdateEntities(entities);

        Assert.True(res.IsBad);
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
    }

    [Fact]
    public async void UpdateEntities_NullEntity()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity?> {null};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateEntities(entities));
    }

    [Fact]
    public async void UpdateEntities_InvalidType()
    {
        var client = NewClient();
        var entities = new List<InvalidEntity> {new(), new(), new()};

        await Assert.ThrowsAsync<FiwareContractException>(() => client.UpdateEntities(entities));
    }

    [Fact]
    public async void UpdateEntities_MissingEntityId()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity> {new(), new(), new()};

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.UpdateEntities(entities));
    }

    [Fact]
    public async void UpdateEntities_MissingEntityType()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(UpdateEntities_MissingEntityType) + "1", null),
            NewEntity(nameof(UpdateEntities_MissingEntityType) + "2", null),
            NewEntity(nameof(UpdateEntities_MissingEntityType) + "3", null)
        };

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.UpdateEntities(entities));
    }

    [Fact]
    public async void UpdateEntities_DuplicateEntity()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(UpdateEntities_DuplicateEntity)),
            NewEntity(nameof(UpdateEntities_DuplicateEntity))
        };

        await client.CreateEntity(entities[0]);
        entities[0].StringValue = "TestValue2";
        entities[1].StringValue = "TestValue3";
        var res = await client.UpdateEntities(entities);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.True(await Fixture.EntityExists(entities[0].Id));

        //update batch is executed in order -> on duplicates, the last value in the batch will be the final value
        var check = await Fixture.GetEntity(entities[0].Id);
        Assert.NotNull(check);
        Assert.Equal(entities[1].StringValue, check["StringValue"]!["value"]!.ToObject<string>());
    }

    [Fact]
    public async void UpdateEntities_EntitiesNotFound()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(UpdateEntities_EntitiesNotFound) + "1"),
            NewEntity(nameof(UpdateEntities_EntitiesNotFound) + "2"),
            NewEntity(nameof(UpdateEntities_EntitiesNotFound) + "3")
        };

        var res = await client.UpdateEntities(entities);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(entities[0].Id));
        Assert.False(await Fixture.EntityExists(entities[1].Id));
        Assert.False(await Fixture.EntityExists(entities[2].Id));
    }

    [Fact]
    public async void UpdateEntities_SomeEntitiesFound()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(UpdateEntities_SomeEntitiesFound) + "1"),
            NewEntity(nameof(UpdateEntities_SomeEntitiesFound) + "2"),
            NewEntity(nameof(UpdateEntities_SomeEntitiesFound) + "3")
        };

        await client.CreateEntity(entities[0]);
        entities[0].StringValue = "TestValue2";
        var res = await client.UpdateEntities(entities);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.True(await Fixture.EntityExists(entities[0].Id));
        Assert.False(await Fixture.EntityExists(entities[1].Id));
        Assert.False(await Fixture.EntityExists(entities[2].Id));

        var check1 = await Fixture.GetEntity(entities[0].Id);
        Assert.NotNull(check1);
        Assert.Equal(entities[0].StringValue, check1["StringValue"]!["value"]!.ToObject<string>());
    }

    [Fact]
    public async void UpdateEntities_SomeEntitiesFound2()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(UpdateEntities_SomeEntitiesFound2) + "1"),
            NewEntity(nameof(UpdateEntities_SomeEntitiesFound2) + "2"),
            NewEntity(nameof(UpdateEntities_SomeEntitiesFound2) + "3")
        };

        //create only last entity to check whether the entire batch is processed or aborted early
        await client.CreateEntity(entities[2]);
        entities[2].StringValue = "TestValue2";
        var res = await client.UpdateEntities(entities);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(entities[0].Id));
        Assert.False(await Fixture.EntityExists(entities[1].Id));
        Assert.True(await Fixture.EntityExists(entities[2].Id));

        var check3 = await Fixture.GetEntity(entities[2].Id);
        Assert.NotNull(check3);
        Assert.Equal(entities[2].StringValue, check3["StringValue"]!["value"]!.ToObject<string>());
    }

    [Fact]
    public async void UpdateEntities_AllEntitiesFound()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(UpdateEntities_AllEntitiesFound) + "1"),
            NewEntity(nameof(UpdateEntities_AllEntitiesFound) + "2"),
            NewEntity(nameof(UpdateEntities_AllEntitiesFound) + "3")
        };

        await client.CreateEntities(entities);
        entities[0].StringValue = "TestValue2";
        entities[1].StringValue = "TestValue2";
        entities[2].StringValue = "TestValue2";
        var res = await client.UpdateEntities(entities);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);

        var check1 = await Fixture.GetEntity(entities[0].Id);
        Assert.NotNull(check1);
        Assert.Equal(entities[0].StringValue, check1["StringValue"]!["value"]!.ToObject<string>());

        var check2 = await Fixture.GetEntity(entities[1].Id);
        Assert.NotNull(check2);
        Assert.Equal(entities[1].StringValue, check2["StringValue"]!["value"]!.ToObject<string>());

        var check3 = await Fixture.GetEntity(entities[2].Id);
        Assert.NotNull(check3);
        Assert.Equal(entities[2].StringValue, check3["StringValue"]!["value"]!.ToObject<string>());
    }
    #endregion

    #region CreateOrUpdateEntity
    [Fact]
    public async void CreateOrUpdateEntity_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateOrUpdateEntity((UnitTestEntity?) null));
    }

    [Fact]
    public async void CreateOrUpdateEntity_InvalidType()
    {
        var client = NewClient();
        var entity = new InvalidEntity();

        await Assert.ThrowsAsync<FiwareContractException>(() => client.CreateOrUpdateEntity(entity));
    }

    [Fact]
    public async void CreateOrUpdateEntity_MissingEntityId()
    {
        var client = NewClient();
        var entity = NewEntity();

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.CreateOrUpdateEntity(entity));
    }

    [Fact]
    public async void CreateOrUpdateEntity_MissingEntityType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateOrUpdateEntity_MissingEntityType), null);

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.CreateOrUpdateEntity(entity));
    }

    [Fact]
    public async void CreateOrUpdateEntity_EntityNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateOrUpdateEntity_EntityNotFound));

        var res = await client.CreateOrUpdateEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.True(await Fixture.EntityExists(entity.Id));
    }

    [Fact]
    public async void CreateOrUpdateEntity_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateOrUpdateEntity_EntityFound));

        await client.CreateEntity(entity);
        entity.StringValue = "TestValue2";
        var res = await client.CreateOrUpdateEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);

        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(entity.StringValue, check["StringValue"]!["value"]!.ToObject<string>());
    }
    #endregion

    #region CreateOrUpdateEntities
    [Fact]
    public async void CreateOrUpdateEntities_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateOrUpdateEntities((IEnumerable<UnitTestEntity>?) null));
    }

    [Fact]
    public async void CreateOrUpdateEntities_Empty()
    {
        var client = NewClient();
        var entities = Array.Empty<UnitTestEntity>();

        var res = await client.CreateOrUpdateEntities(entities);

        Assert.True(res.IsBad);
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
    }

    [Fact]
    public async void CreateOrUpdateEntities_NullEntity()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity?> {null};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateOrUpdateEntities(entities));
    }

    [Fact]
    public async void CreateOrUpdateEntities_InvalidType()
    {
        var client = NewClient();
        var entities = new List<InvalidEntity> {new(), new(), new()};

        await Assert.ThrowsAsync<FiwareContractException>(() => client.CreateOrUpdateEntities(entities));
    }

    [Fact]
    public async void CreateOrUpdateEntities_MissingEntityId()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity> {new(), new(), new()};

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.CreateOrUpdateEntities(entities));
    }

    [Fact]
    public async void CreateOrUpdateEntities_MissingEntityType()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(CreateOrUpdateEntities_MissingEntityType) + "1", null),
            NewEntity(nameof(CreateOrUpdateEntities_MissingEntityType) + "2", null),
            NewEntity(nameof(CreateOrUpdateEntities_MissingEntityType) + "3", null)
        };

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.CreateOrUpdateEntities(entities));
    }

    [Fact]
    public async void CreateOrUpdateEntities_NonExistingDuplicateEntity()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(CreateOrUpdateEntities_NonExistingDuplicateEntity)),
            NewEntity(nameof(CreateOrUpdateEntities_NonExistingDuplicateEntity))
        };

        entities[0].StringValue = "TestValue2";
        entities[1].StringValue = "TestValue3";
        var res = await client.CreateOrUpdateEntities(entities);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.True(await Fixture.EntityExists(entities[0].Id));

        //update batch is executed in order -> on duplicates, the last value in the batch will be the final value
        var check = await Fixture.GetEntity(entities[0].Id);
        Assert.NotNull(check);
        Assert.Equal(entities[1].StringValue, check["StringValue"]!["value"]!.ToObject<string>());
    }

    [Fact]
    public async void CreateOrUpdateEntities_ExistingDuplicateEntity()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(CreateOrUpdateEntities_ExistingDuplicateEntity)),
            NewEntity(nameof(CreateOrUpdateEntities_ExistingDuplicateEntity))
        };

        await client.CreateEntity(entities[0]);
        entities[0].StringValue = "TestValue2";
        entities[1].StringValue = "TestValue3";
        var res = await client.CreateOrUpdateEntities(entities);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.True(await Fixture.EntityExists(entities[0].Id));

        //update batch is executed in order -> on duplicates, the last value in the batch will be the final value
        var check = await Fixture.GetEntity(entities[0].Id);
        Assert.NotNull(check);
        Assert.Equal(entities[1].StringValue, check["StringValue"]!["value"]!.ToObject<string>());
    }

    [Fact]
    public async void CreateOrUpdateEntities_EntitiesNotFound()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(CreateOrUpdateEntities_EntitiesNotFound) + "1"),
            NewEntity(nameof(CreateOrUpdateEntities_EntitiesNotFound) + "2"),
            NewEntity(nameof(CreateOrUpdateEntities_EntitiesNotFound) + "3")
        };

        var res = await client.CreateOrUpdateEntities(entities);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.True(await Fixture.EntityExists(entities[0].Id));
        Assert.True(await Fixture.EntityExists(entities[1].Id));
        Assert.True(await Fixture.EntityExists(entities[2].Id));
    }

    [Fact]
    public async void CreateOrUpdateEntities_SomeEntitiesFound()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(CreateOrUpdateEntities_SomeEntitiesFound) + "1"),
            NewEntity(nameof(CreateOrUpdateEntities_SomeEntitiesFound) + "2"),
            NewEntity(nameof(CreateOrUpdateEntities_SomeEntitiesFound) + "3")
        };

        await client.CreateEntity(entities[0]);
        entities[0].StringValue = "TestValue2";
        var res = await client.CreateOrUpdateEntities(entities);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.True(await Fixture.EntityExists(entities[0].Id));
        Assert.True(await Fixture.EntityExists(entities[1].Id));
        Assert.True(await Fixture.EntityExists(entities[2].Id));

        var check1 = await Fixture.GetEntity(entities[0].Id);
        Assert.NotNull(check1);
        Assert.Equal(entities[0].StringValue, check1["StringValue"]!["value"]!.ToObject<string>());
    }

    [Fact]
    public async void CreateOrUpdateEntities_SomeEntitiesFound2()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(CreateOrUpdateEntities_SomeEntitiesFound2) + "1"),
            NewEntity(nameof(CreateOrUpdateEntities_SomeEntitiesFound2) + "2"),
            NewEntity(nameof(CreateOrUpdateEntities_SomeEntitiesFound2) + "3")
        };

        //create only last entity to check whether the entire batch is processed or aborted early
        await client.CreateEntity(entities[2]);
        entities[2].StringValue = "TestValue2";
        var res = await client.CreateOrUpdateEntities(entities);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.True(await Fixture.EntityExists(entities[0].Id));
        Assert.True(await Fixture.EntityExists(entities[1].Id));
        Assert.True(await Fixture.EntityExists(entities[2].Id));

        var check3 = await Fixture.GetEntity(entities[2].Id);
        Assert.NotNull(check3);
        Assert.Equal(entities[2].StringValue, check3["StringValue"]!["value"]!.ToObject<string>());
    }

    [Fact]
    public async void CreateOrUpdateEntities_AllEntitiesFound()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(CreateOrUpdateEntities_AllEntitiesFound) + "1"),
            NewEntity(nameof(CreateOrUpdateEntities_AllEntitiesFound) + "2"),
            NewEntity(nameof(CreateOrUpdateEntities_AllEntitiesFound) + "3")
        };

        await client.CreateEntities(entities);
        entities[0].StringValue = "TestValue2";
        entities[1].StringValue = "TestValue2";
        entities[2].StringValue = "TestValue2";
        var res = await client.CreateOrUpdateEntities(entities);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);

        var check1 = await Fixture.GetEntity(entities[0].Id);
        Assert.NotNull(check1);
        Assert.Equal(entities[0].StringValue, check1["StringValue"]!["value"]!.ToObject<string>());

        var check2 = await Fixture.GetEntity(entities[1].Id);
        Assert.NotNull(check2);
        Assert.Equal(entities[1].StringValue, check2["StringValue"]!["value"]!.ToObject<string>());

        var check3 = await Fixture.GetEntity(entities[2].Id);
        Assert.NotNull(check3);
        Assert.Equal(entities[2].StringValue, check3["StringValue"]!["value"]!.ToObject<string>());
    }
    #endregion

    #region GetEntity
    [Fact]
    public async void GetEntity1_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntity<UnitTestEntity>(null));
    }

    [Fact]
    public async void GetEntity1_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntity<UnitTestEntity>(string.Empty));
    }

    [Fact]
    public async void GetEntity1_InvalidType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetEntity1_InvalidType));

        await client.CreateEntity(entity);
        await Assert.ThrowsAsync<FiwareContractException>(() => client.GetEntity<InvalidEntity>(entity.Id));
    }

    [Fact]
    public async void GetEntity1_EntityNotFound()
    {
        var client = NewClient();

        var (res, entity) = await client.GetEntity<UnitTestEntity>(nameof(GetEntity1_EntityNotFound));

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.Null(entity);
    }

    [Fact]
    public async void GetEntity1_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetEntity1_EntityFound));

        await client.CreateEntity(entity);
        var (res, getEntity) = await client.GetEntity<UnitTestEntity>(entity.Id);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(getEntity);
    }

    [Fact]
    public async void GetEntity1_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetEntity1_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(GetEntity1_MultipleEntitiesFound), Fixture.EntityType + "2");

        await client.CreateEntity(entity);
        await client.CreateEntity(entity2);
        var (res, getEntity) = await client.GetEntity<UnitTestEntity>(entity.Id);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.Conflict, res.Code);
        Assert.Null(getEntity);
    }

    [Fact]
    public async void GetEntity2_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntity<UnitTestEntity>(null, Fixture.EntityType));
    }

    [Fact]
    public async void GetEntity2_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntity<UnitTestEntity>(string.Empty, Fixture.EntityType));
    }

    [Fact]
    public async void GetEntity2_NullType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntity<UnitTestEntity>(nameof(GetEntity2_NullType), null));
    }

    [Fact]
    public async void GetEntity2_EmptyType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntity<UnitTestEntity>(nameof(GetEntity2_EmptyType), string.Empty));
    }

    [Fact]
    public async void GetEntity2_InvalidType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetEntity2_InvalidType));

        await client.CreateEntity(entity);
        await Assert.ThrowsAsync<FiwareContractException>(() => client.GetEntity<InvalidEntity>(entity.Id, entity.Type));
    }

    [Fact]
    public async void GetEntity2_EntityNotFound()
    {
        var client = NewClient();

        var (res, entity) = await client.GetEntity<UnitTestEntity>(nameof(GetEntity2_EntityNotFound), Fixture.EntityType);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.Null(entity);
    }

    [Fact]
    public async void GetEntity2_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetEntity2_EntityFound));

        await client.CreateEntity(entity);
        var (res, getEntity) = await client.GetEntity<UnitTestEntity>(entity.Id, entity.Type);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(getEntity);
    }

    [Fact]
    public async void GetEntity2_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetEntity2_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(GetEntity2_MultipleEntitiesFound), Fixture.EntityType + "2");

        await client.CreateEntity(entity);
        await client.CreateEntity(entity2);
        var (res, getEntity) = await client.GetEntity<UnitTestEntity>(entity.Id, entity.Type);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(getEntity);
    }
    #endregion

    #region GetEntities
    [Fact]
    public async void GetEntities1_Dynamic()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetEntities1_Dynamic));

        await client.CreateEntity(entity);
        var (res, entities) = await client.GetEntities();

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.True(entities.Count > 0);
    }

    [Fact]
    public async void GetEntities1_Specific()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetEntities1_Specific));

        await client.CreateEntity(entity);
        var (res, entities) = await client.GetEntities<UnitTestEntity>();

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.True(entities.Count > 0);
    }

    [Fact]
    public async void GetEntities2_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>((string?) null));
    }

    [Fact]
    public async void GetEntities2_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>(string.Empty));
    }

    [Fact]
    public async void GetEntities2_InvalidType()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities2_InvalidType));
        var entity2 = NewEntity(nameof(GetEntities2_InvalidType), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await Assert.ThrowsAsync<FiwareContractException>(() => client.GetEntities<InvalidEntity>(entity1.Id));
    }

    [Fact]
    public async void GetEntities2_EntitiesNotFound()
    {
        var client = NewClient();

        var (res, entities) = await client.GetEntities<UnitTestEntity>(nameof(GetEntities2_EntitiesNotFound));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntities2_EntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities2_EntitiesFound));
        var entity2 = NewEntity(nameof(GetEntities2_EntitiesFound), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(entity1.Id);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities3_NullIdPattern()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>((Regex?) null));
    }

    [Fact]
    public async void GetEntities3_InvalidType()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities3_InvalidType));
        var entity2 = NewEntity(nameof(GetEntities3_InvalidType), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await Assert.ThrowsAsync<FiwareContractException>(() => client.GetEntities<InvalidEntity>(new Regex(entity1.Id)));
    }

    [Fact]
    public async void GetEntities3_EntitiesNotFound()
    {
        var client = NewClient();

        var (res, entities) = await client.GetEntities<UnitTestEntity>(new Regex(nameof(GetEntities3_EntitiesNotFound)));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntities3_EntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities3_EntitiesFound));
        var entity2 = NewEntity(nameof(GetEntities3_EntitiesFound) + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(new Regex(entity1.Id));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities4_NullIdPattern()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>(null, Fixture.EntityType));
    }

    [Fact]
    public async void GetEntities4_NullType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>(new Regex(nameof(GetEntities4_NullType)), (string?) null));
    }

    [Fact]
    public async void GetEntities4_EmptyType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>(new Regex(nameof(GetEntities4_EmptyType)), string.Empty));
    }

    [Fact]
    public async void GetEntities4_InvalidType()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities4_InvalidType));
        var entity2 = NewEntity(nameof(GetEntities4_InvalidType) + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await Assert.ThrowsAsync<FiwareContractException>(() => client.GetEntities<InvalidEntity>(new Regex(entity1.Id), entity1.Type));
    }

    [Fact]
    public async void GetEntities4_EntitiesNotFound()
    {
        var client = NewClient();

        var (res, entities) = await client.GetEntities<UnitTestEntity>(new Regex(nameof(GetEntities4_EntitiesNotFound)), Fixture.EntityType);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntities4_EntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities4_EntitiesFound));
        var entity2 = NewEntity(nameof(GetEntities4_EntitiesFound) + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(new Regex(entity1.Id), entity1.Type);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities5_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>((string?) null, new Regex(Fixture.EntityType)));
    }

    [Fact]
    public async void GetEntities5_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>(string.Empty, new Regex(Fixture.EntityType)));
    }

    [Fact]
    public async void GetEntities5_NullTypePattern()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>(nameof(GetEntities5_NullTypePattern), null));
    }

    [Fact]
    public async void GetEntities5_InvalidType()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities5_InvalidType));
        var entity2 = NewEntity(nameof(GetEntities5_InvalidType), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        //await Assert.ThrowsAsync<FiwareContractException>(() => client.GetCollection<InvalidEntity>(entity1.Id, new Regex(entity1.Type)));
        var (res, entities) = await client.GetEntities<InvalidEntity>(entity1.Id, new Regex(entity1.Type));

        //seems to be a bug with FIWARE/Orion when using typePattern
        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
        Assert.True(await Fixture.EntityExists(entity1.Id, entity1.Type));
        Assert.True(await Fixture.EntityExists(entity2.Id, entity2.Type));
    }

    [Fact]
    public async void GetEntities5_EntitiesNotFound()
    {
        var client = NewClient();

        var (res, entities) = await client.GetEntities<UnitTestEntity>(nameof(GetEntities5_EntitiesNotFound), new Regex(Fixture.EntityType));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntities5_EntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities5_EntitiesFound));
        var entity2 = NewEntity(nameof(GetEntities5_EntitiesFound), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(entity1.Id, new Regex(entity1.Type));

        //seems to be a bug with FIWARE/Orion when using typePattern
        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        //Assert.Equal(2, entities.Count);
        //Assert.Equal(entity1.Id, entities[0].Id);
        //Assert.Equal(entity2.Id, entities[1].Id);
        Assert.Equal(0, entities.Count);
        Assert.True(await Fixture.EntityExists(entity1.Id, entity1.Type));
        Assert.True(await Fixture.EntityExists(entity2.Id, entity2.Type));
    }

    [Fact]
    public async void GetEntities6_NullIdPattern()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>((Regex?) null, new Regex(Fixture.EntityType)));
    }

    [Fact]
    public async void GetEntities6_NullTypePattern()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>(new Regex(nameof(GetEntities6_NullTypePattern)), (Regex?) null));
    }

    [Fact]
    public async void GetEntities6_InvalidType()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities6_InvalidType) + "1");
        var entity2 = NewEntity(nameof(GetEntities6_InvalidType), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await Assert.ThrowsAsync<FiwareContractException>(() => client.GetEntities<InvalidEntity>(new Regex(entity1.Id), new Regex(entity1.Type)));
    }

    [Fact]
    public async void GetEntities6_EntitiesNotFound()
    {
        var client = NewClient();

        var (res, entities) = await client.GetEntities<UnitTestEntity>(new Regex(nameof(GetEntities6_EntitiesNotFound)), new Regex(Fixture.EntityType));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntities6_EntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities6_EntitiesFound) + "1");
        var entity2 = NewEntity(nameof(GetEntities6_EntitiesFound), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(new Regex(entity1.Id), new Regex(entity1.Type));

        //seems to be a bug with FIWARE/Orion when using typePattern
        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        //Assert.Equal(2, entities.Count);
        //Assert.Equal(entity1.Id, entities[0].Id);
        //Assert.Equal(entity2.Id, entities[1].Id);
        Assert.Equal(1, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.True(await Fixture.EntityExists(entity1.Id, entity1.Type));
        Assert.True(await Fixture.EntityExists(entity2.Id, entity2.Type));
    }

    [Fact]
    public async void GetEntities7_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<InvalidEntity>((EntityFilter?) null));
    }

    [Fact]
    public async void GetEntities7_InvalidType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetEntities7_InvalidType));
        var filter = new EntityFilter {IdPattern = new Regex(entity.Id)};

        await client.CreateEntity(entity);
        await Assert.ThrowsAsync<FiwareContractException>(() => client.GetEntities<InvalidEntity>(filter));
    }

    [Fact]
    public async void GetEntities7_InvalidFilter()
    {
        var client = NewClient();
        var filter1 = new EntityFilter {Id = nameof(GetEntities7_InvalidFilter), IdPattern = new Regex(nameof(GetEntities7_InvalidFilter))};
        var filter2 = new EntityFilter {Type = Fixture.EntityType, TypePattern = new Regex(Fixture.EntityType)};

        var (res1, entities1) = await client.GetEntities<InvalidEntity>(filter1);
        var (res2, entities2) = await client.GetEntities<InvalidEntity>(filter2);

        Assert.True(res1.IsBad, res1.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res1.Code);
        Assert.Null(entities1);
        Assert.True(res2.IsBad, res2.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res2.Code);
        Assert.Null(entities2);
    }

    [Fact]
    public async void GetEntities7_EntityNotFound()
    {
        var client = NewClient();
        var filter = new EntityFilter {Id = nameof(GetEntities7_EntityNotFound)};

        var (res, entities) = await client.GetEntities<UnitTestEntity>(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntities7_ValidIdFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities7_ValidIdFilter));
        var entity2 = NewEntity(nameof(GetEntities7_ValidIdFilter) + "1");
        var filter = new EntityFilter {Id = entity1.Id};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(1, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.True(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void GetEntities7_ValidIdPatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities7_ValidIdPatternFilter));
        var entity2 = NewEntity(nameof(GetEntities7_ValidIdPatternFilter) + "1");
        var filter = new EntityFilter {IdPattern = new Regex(entity1.Id)};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities7_ValidTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities7_ValidTypeFilter), Fixture.EntityType + nameof(GetEntities7_ValidTypeFilter));
        var entity2 = NewEntity(nameof(GetEntities7_ValidTypeFilter) + "1", Fixture.EntityType + nameof(GetEntities7_ValidTypeFilter));
        var filter = new EntityFilter {Type = entity1.Type};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities7_ValidTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities7_ValidTypePatternFilter), Fixture.EntityType + nameof(GetEntities7_ValidTypePatternFilter));
        var entity2 = NewEntity(nameof(GetEntities7_ValidTypePatternFilter) + "1", Fixture.EntityType + nameof(GetEntities7_ValidTypePatternFilter) + "1");
        var filter = new EntityFilter {TypePattern = new Regex(entity1.Type)};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities7_ValidIdTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities7_ValidIdTypeFilter));
        var entity2 = NewEntity(nameof(GetEntities7_ValidIdTypeFilter) + "1");
        var filter = new EntityFilter {Id = entity1.Id, Type = entity1.Type};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(1, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.True(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void GetEntities7_ValidIdPatternTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities7_ValidIdPatternTypeFilter));
        var entity2 = NewEntity(nameof(GetEntities7_ValidIdPatternTypeFilter) + "1");
        var filter = new EntityFilter {IdPattern = new Regex(entity1.Id), Type = entity1.Type};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities7_ValidIdTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities7_ValidIdTypePatternFilter));
        var entity2 = NewEntity(nameof(GetEntities7_ValidIdTypePatternFilter), Fixture.EntityType + "1");
        var filter = new EntityFilter {Id = entity1.Id, TypePattern = new Regex(entity1.Type)};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filter);

        //FIWARE bug when combining id and typePattern
        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
        Assert.True(await Fixture.EntityExists(entity1.Id, entity1.Type));
        Assert.True(await Fixture.EntityExists(entity2.Id, entity2.Type));
    }

    [Fact]
    public async void GetEntities7_ValidIdPatternTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities7_ValidIdPatternTypePatternFilter) + "1");
        var entity2 = NewEntity(nameof(GetEntities7_ValidIdPatternTypePatternFilter), Fixture.EntityType + "1");
        var filter = new EntityFilter {IdPattern = new Regex(entity2.Id), TypePattern = new Regex(entity1.Type)};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities7_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities7_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(GetEntities7_MultipleEntitiesFound), Fixture.EntityType + "1");
        var filter = new EntityFilter {Id = entity1.Id};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities8_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntities<UnitTestEntity>((IEnumerable<EntityFilter>?) null));
    }

    [Fact]
    public async void GetEntities8_EmptyFilter()
    {
        var client = NewClient();
        var filters = Array.Empty<EntityFilter>();
        var entity1 = NewEntity(nameof(GetEntities8_EmptyFilter));
        var entity2 = NewEntity(nameof(GetEntities8_EmptyFilter), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filters);

        //empty filter returns all entities
        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.True(entities.Count >= 2, entities.Count.ToString()); //might return entities from other tests
    }

    [Fact]
    public async void GetEntities8_InvalidType()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities8_InvalidType) + "1");
        var entity2 = NewEntity(nameof(GetEntities8_InvalidType), Fixture.EntityType + "1");
        var filters = new EntityFilter[] {new() {IdPattern = new Regex(entity2.Id), TypePattern = new Regex(Fixture.EntityType)}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await Assert.ThrowsAsync<FiwareContractException>(() => client.GetEntities<InvalidEntity>(filters));
    }

    [Fact]
    public async void GetEntities8_InvalidFilter()
    {
        var client = NewClient();
        var filters1 = new EntityFilter[] {new() {Id = nameof(GetEntities8_InvalidFilter), IdPattern = new Regex(nameof(GetEntities8_InvalidFilter))}};
        var filters2 = new EntityFilter[] {new() {Type = Fixture.EntityType, TypePattern = new Regex(Fixture.EntityType)}};

        var (res1, entities1) = await client.GetEntities<UnitTestEntity>(filters1);
        var (res2, entities2) = await client.GetEntities<UnitTestEntity>(filters2);

        Assert.True(res1.IsBad, res1.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res1.Code);
        Assert.Null(entities1);
        //this is a bug with FIWARE queries
        //Assert.True(res2.IsBad, res2.Code.ToString());
        //Assert.Equal(HttpStatusCode.BadRequest, res2.Code);
        //Assert.Null(entities2);
        Assert.True(res2.IsGood, res2.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res2.Code);
        Assert.NotNull(entities2);
    }

    [Fact]
    public async void GetEntities8_EntityNotFound()
    {
        var client = NewClient();
        var filters = new EntityFilter[] {new() {Id = nameof(GetEntities8_EntityNotFound)}};

        var (res, entities) = await client.GetEntities<UnitTestEntity>(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntities8_ValidIdFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities8_ValidIdFilter));
        var entity2 = NewEntity(nameof(GetEntities8_ValidIdFilter) + "1");
        var filters = new EntityFilter[] {new() {Id = entity1.Id}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(1, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.True(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void GetEntities8_ValidIdPatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities8_ValidIdPatternFilter));
        var entity2 = NewEntity(nameof(GetEntities8_ValidIdPatternFilter) + "1");
        var filters = new EntityFilter[] {new() {IdPattern = new Regex(entity1.Id)}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities8_ValidTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities8_ValidTypeFilter), Fixture.EntityType + nameof(GetEntities8_ValidTypeFilter));
        var entity2 = NewEntity(nameof(GetEntities8_ValidTypeFilter) + "1", Fixture.EntityType + nameof(GetEntities8_ValidTypeFilter));
        var filters = new EntityFilter[] {new() {Type = entity1.Type}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities8_ValidTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities8_ValidTypePatternFilter), Fixture.EntityType + nameof(GetEntities8_ValidTypePatternFilter));
        var entity2 = NewEntity(nameof(GetEntities8_ValidTypePatternFilter) + "1", Fixture.EntityType + nameof(GetEntities8_ValidTypePatternFilter) + "1");
        var filters = new EntityFilter[] {new() {TypePattern = new Regex(entity1.Type)}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities8_ValidIdTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities8_ValidIdTypeFilter));
        var entity2 = NewEntity(nameof(GetEntities8_ValidIdTypeFilter) + "1");
        var filters = new EntityFilter[] {new() {Id = entity1.Id, Type = entity1.Type}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(1, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.True(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void GetEntities8_ValidIdPatternTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities8_ValidIdPatternTypeFilter));
        var entity2 = NewEntity(nameof(GetEntities8_ValidIdPatternTypeFilter) + "1");
        var filters = new EntityFilter[] {new() {IdPattern = new Regex(entity1.Id), Type = entity1.Type}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities8_ValidIdTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities8_ValidIdTypePatternFilter));
        var entity2 = NewEntity(nameof(GetEntities8_ValidIdTypePatternFilter), Fixture.EntityType + "1");
        var filters = new EntityFilter[] {new() {Id = entity1.Id, TypePattern = new Regex(entity1.Type)}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities8_ValidIdPatternTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities8_ValidIdPatternTypePatternFilter) + "1");
        var entity2 = NewEntity(nameof(GetEntities8_ValidIdPatternTypePatternFilter), Fixture.EntityType + "1");
        var filters = new EntityFilter[] {new() {IdPattern = new Regex(entity2.Id), TypePattern = new Regex(entity1.Type)}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }

    [Fact]
    public async void GetEntities8_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntities8_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(GetEntities8_MultipleEntitiesFound), Fixture.EntityType + "1");
        var filters = new EntityFilter[] {new() {Id = entity1.Id}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<UnitTestEntity>(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
    }
    #endregion

    #region GetEntitiesByType
    [Fact]
    public async void GetEntitiesByType1_NullType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByType<UnitTestEntity>((string?) null));
    }

    [Fact]
    public async void GetEntitiesByType1_EmptyType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByType<UnitTestEntity>(string.Empty));
    }

    [Fact]
    public async void GetEntitiesByType1_EntitiesNotFound()
    {
        var client = NewClient();

        var (res, entities) = await client.GetEntitiesByType<UnitTestEntity>(Fixture.EntityType + "123");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntitiesByType1_EntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntitiesByType1_EntitiesFound), Fixture.EntityType + nameof(GetEntitiesByType1_EntitiesFound));
        var entity2 = NewEntity(nameof(GetEntitiesByType1_EntitiesFound) + "1", Fixture.EntityType + nameof(GetEntitiesByType1_EntitiesFound));
        var entity3 = NewEntity(nameof(GetEntitiesByType1_EntitiesFound), Fixture.EntityType + nameof(GetEntitiesByType1_EntitiesFound) + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await client.CreateEntity(entity3);
        var (res, entities) = await client.GetEntitiesByType<UnitTestEntity>(entity1.Type);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
        Assert.True(await Fixture.EntityExists(entity3.Id, entity3.Type));
    }

    [Fact]
    public async void GetEntitiesByType2_NullTypePattern()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByType<UnitTestEntity>((Regex?) null));
    }

    [Fact]
    public async void GetEntitiesByType2_EntitiesNotFound()
    {
        var client = NewClient();

        var (res, entities) = await client.GetEntitiesByType<UnitTestEntity>(new Regex(Fixture.EntityType) + "123");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntitiesByType2_EntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntitiesByType2_EntitiesFound), Fixture.EntityType + nameof(GetEntitiesByType2_EntitiesFound));
        var entity2 = NewEntity(nameof(GetEntitiesByType2_EntitiesFound) + "1", Fixture.EntityType + nameof(GetEntitiesByType2_EntitiesFound));
        var entity3 = NewEntity(nameof(GetEntitiesByType2_EntitiesFound), Fixture.EntityType + nameof(GetEntitiesByType2_EntitiesFound) + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await client.CreateEntity(entity3);
        var (res, entities) = await client.GetEntitiesByType<UnitTestEntity>(new Regex(entity1.Type));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(3, entities.Count);
        Assert.Equal(entity1.Id, entities[0].Id);
        Assert.Equal(entity2.Id, entities[1].Id);
        Assert.Equal(entity3.Type, entities[2].Type);
    }
    #endregion

    #region GetEntitiesByQuery
    [Fact]
    public async void GetEntitiesByQuery1_NullQuery()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>(null));
    }

    [Fact]
    public async void GetEntitiesByQuery1_EmptyQuery()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>(string.Empty));
    }

    [Fact]
    public async void GetEntitiesByQuery1_EntitiesNotFound()
    {
        var client = NewClient();

        var (res, entities) = await client.GetEntitiesByQuery<UnitTestEntity>("StringValue==TestValue1");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntitiesByQuery1_EntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntitiesByQuery1_EntitiesFound));
        entity1.StringValue = entity1.Id;
        var entity2 = NewEntity(nameof(GetEntitiesByQuery1_EntitiesFound), Fixture.EntityType + "1");
        entity2.StringValue = entity2.Id;
        var entity3 = NewEntity(nameof(GetEntitiesByQuery1_EntitiesFound) + "1");
        entity3.StringValue = "OtherTestValue";

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await client.CreateEntity(entity3);
        var (res, entities) = await client.GetEntitiesByQuery<UnitTestEntity>($"StringValue=={entity1.StringValue}");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Type, entities[0].Type);
        Assert.Equal(entity2.Type, entities[1].Type);
        Assert.True(await Fixture.EntityExists(entity3.Id));
    }

    [Fact]
    public async void GetEntitiesByQuery2_NullType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>((string?) null, "StringValue==TestValue1"));
    }

    [Fact]
    public async void GetEntitiesByQuery2_EmptyType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>(string.Empty, "StringValue==TestValue1"));
    }

    [Fact]
    public async void GetEntitiesByQuery2_NullQuery()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>(Fixture.EntityType, null));
    }

    [Fact]
    public async void GetEntitiesByQuery2_EmptyQuery()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>(Fixture.EntityType, string.Empty));
    }

    [Fact]
    public async void GetEntitiesByQuery2_EntitiesNotFound()
    {
        var client = NewClient();

        var (res, entities) = await client.GetEntitiesByQuery<UnitTestEntity>(Fixture.EntityType, "StringValue==TestValue1");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntitiesByQuery2_EntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntitiesByQuery2_EntitiesFound));
        entity1.StringValue = entity1.Id;
        var entity2 = NewEntity(nameof(GetEntitiesByQuery2_EntitiesFound), Fixture.EntityType + "1");
        entity2.StringValue = entity2.Id;
        var entity3 = NewEntity(nameof(GetEntitiesByQuery2_EntitiesFound) + "1");
        entity3.StringValue = "OtherTestValue";

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await client.CreateEntity(entity3);
        var (res, entities) = await client.GetEntitiesByQuery<UnitTestEntity>(entity1.Type, $"StringValue=={entity1.StringValue}");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(1, entities.Count);
        Assert.Equal(entity1.Type, entities[0].Type);
        Assert.True(await Fixture.EntityExists(entity2.Id, entity2.Type));
        Assert.True(await Fixture.EntityExists(entity3.Id));
    }

    [Fact]
    public async void GetEntitiesByQuery3_NullTypePattern()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>((Regex?) null, "StringValue==TestValue1"));
    }

    [Fact]
    public async void GetEntitiesByQuery3_NullQuery()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>(new Regex(Fixture.EntityType), null));
    }

    [Fact]
    public async void GetEntitiesByQuery3_EmptyQuery()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>(new Regex(Fixture.EntityType), string.Empty));
    }

    [Fact]
    public async void GetEntitiesByQuery3_EntitiesNotFound()
    {
        var client = NewClient();

        var (res, entities) = await client.GetEntitiesByQuery<UnitTestEntity>(new Regex(Fixture.EntityType), "StringValue==TestValue1");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
    }

    [Fact]
    public async void GetEntitiesByQuery3_EntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetEntitiesByQuery3_EntitiesFound));
        entity1.StringValue = entity1.Id;
        var entity2 = NewEntity(nameof(GetEntitiesByQuery3_EntitiesFound), Fixture.EntityType + "1");
        entity2.StringValue = entity2.Id;
        var entity3 = NewEntity(nameof(GetEntitiesByQuery3_EntitiesFound) + "1");
        entity3.StringValue = "OtherTestValue";

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await client.CreateEntity(entity3);
        var (res, entities) = await client.GetEntitiesByQuery<UnitTestEntity>(new Regex(entity1.Type), $"StringValue=={entity1.StringValue}");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Type, entities[0].Type);
        Assert.Equal(entity2.Type, entities[1].Type);
        Assert.True(await Fixture.EntityExists(entity3.Id));
    }

    [Fact]
    public async void GetEntitiesByQuery4_NullFilter()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>((EntityFilter?) null, "StringValue==TestValue"));
    }

    [Fact]
    public async void GetEntitiesByQuery4_InvalidFilter()
    {
        var client = NewClient();
        var filter1 = new EntityFilter {Id = nameof(GetEntitiesByQuery4_InvalidFilter), IdPattern = new Regex(nameof(GetEntitiesByQuery4_InvalidFilter))};
        var filter2 = new EntityFilter {Type = Fixture.EntityType + nameof(GetEntitiesByQuery4_InvalidFilter), TypePattern = new Regex(Fixture.EntityType + nameof(GetEntitiesByQuery4_InvalidFilter))};

        var (res1, entities1) = await client.GetEntitiesByQuery<UnitTestEntity>(filter1, "StringValue==TestValue");
        var (res2, entities2) = await client.GetEntitiesByQuery<UnitTestEntity>(filter2, "StringValue==TestValue");

        Assert.True(res1.IsBad, res1.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res1.Code);
        Assert.Null(entities1);
        //this is a bug with FIWARE queries
        //Assert.True(res2.IsBad, res2.Code.ToString());
        //Assert.Equal(HttpStatusCode.BadRequest, res2.Code);
        //Assert.Null(entities2);
        Assert.True(res2.IsGood, res2.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res2.Code);
        Assert.NotNull(entities2);
    }

    [Fact]
    public async void GetEntitiesByQuery4_NullQuery()
    {
        var client = NewClient();
        var filter = new EntityFilter {Id = nameof(GetEntitiesByQuery4_NullQuery), Type = Fixture.EntityType};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>(filter, null));
    }

    [Fact]
    public async void GetEntitiesByQuery4_EmptyQuery()
    {
        var client = NewClient();
        var filter = new EntityFilter {Id = nameof(GetEntitiesByQuery4_EmptyQuery), Type = Fixture.EntityType};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>(filter, string.Empty));
    }

    [Fact]
    public async void GetEntitiesByQuery4_EntitiesNotFound()
    {
        var client = NewClient();
        var filter = new EntityFilter {Id = nameof(GetEntitiesByQuery4_EntitiesNotFound)};

        var (res, entities) = await client.GetEntitiesByQuery<UnitTestEntity>(filter, "StringValue==TestValue");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
        Assert.False(await Fixture.EntityExists(filter.Id));
    }

    [Fact]
    public async void GetEntitiesByQuery4_EntitiesFound()
    {
        var client = NewClient();
        var filter = new EntityFilter {IdPattern = new Regex(nameof(GetEntitiesByQuery4_EntitiesFound))};
        var entity1 = NewEntity(nameof(GetEntitiesByQuery4_EntitiesFound));
        var entity2 = NewEntity(nameof(GetEntitiesByQuery4_EntitiesFound), Fixture.EntityType + "1");
        var entity3 = NewEntity(nameof(GetEntitiesByQuery4_EntitiesFound) + "1");
        entity3.StringValue = "OtherTestValue";

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await client.CreateEntity(entity3);
        var (res, entities) = await client.GetEntitiesByQuery<UnitTestEntity>(filter, $"StringValue=={entity1.StringValue}");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Type, entities[0].Type);
        Assert.Equal(entity2.Type, entities[1].Type);
        Assert.True(await Fixture.EntityExists(entity3.Id));
    }

    [Fact]
    public async void GetEntitiesByQuery5_NullFilter()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>((IEnumerable<EntityFilter>?) null, "StringValue==TestValue"));
    }

    [Fact]
    public async void GetEntitiesByQuery5_EmptyFilter()
    {
        var client = NewClient();
        var filters = Array.Empty<EntityFilter>();
        var entity1 = NewEntity(nameof(GetEntitiesByQuery5_EmptyFilter));
        var entity2 = NewEntity(nameof(GetEntitiesByQuery5_EmptyFilter), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntitiesByQuery<UnitTestEntity>(filters, "StringValue==TestValue");

        //empty filter returns all entities that match the query
        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.True(entities.Count >= 2, entities.Count.ToString()); //might return entities from other tests
    }

    [Fact]
    public async void GetEntitiesByQuery5_InvalidFilter()
    {
        var client = NewClient();
        var filters1 = new EntityFilter[] {new() {Id = nameof(GetEntitiesByQuery5_InvalidFilter), IdPattern = new Regex(nameof(GetEntitiesByQuery5_InvalidFilter))}};
        var filters2 = new EntityFilter[] {new() {Type = Fixture.EntityType + nameof(GetEntitiesByQuery5_InvalidFilter), TypePattern = new Regex(Fixture.EntityType + nameof(GetEntitiesByQuery5_InvalidFilter))}};

        var (res1, entities1) = await client.GetEntitiesByQuery<UnitTestEntity>(filters1, "StringValue==TestValue");
        var (res2, entities2) = await client.GetEntitiesByQuery<UnitTestEntity>(filters2, "StringValue==TestValue");

        Assert.True(res1.IsBad, res1.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res1.Code);
        Assert.Null(entities1);
        //this is a bug with FIWARE queries
        //Assert.True(res2.IsBad, res2.Code.ToString());
        //Assert.Equal(HttpStatusCode.BadRequest, res2.Code);
        //Assert.Null(entities2);
        Assert.True(res2.IsGood, res2.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res2.Code);
        Assert.NotNull(entities2);
    }

    [Fact]
    public async void GetEntitiesByQuery5_NullQuery()
    {
        var client = NewClient();
        var filters = new EntityFilter[] {new() {Id = nameof(GetEntitiesByQuery5_NullQuery), Type = Fixture.EntityType}};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>(filters, null));
    }

    [Fact]
    public async void GetEntitiesByQuery5_EmptyQuery()
    {
        var client = NewClient();
        var filters = new EntityFilter[] {new() {Id = nameof(GetEntitiesByQuery5_EmptyQuery), Type = Fixture.EntityType}};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetEntitiesByQuery<UnitTestEntity>(filters, string.Empty));
    }

    [Fact]
    public async void GetEntitiesByQuery5_EntitiesNotFound()
    {
        var client = NewClient();
        var filters = new EntityFilter[] {new() {Id = nameof(GetEntitiesByQuery5_EntitiesNotFound)}};

        var (res, entities) = await client.GetEntitiesByQuery<UnitTestEntity>(filters, "StringValue==TestValue");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(0, entities.Count);
        Assert.False(await Fixture.EntityExists(filters[0].Id));
    }

    [Fact]
    public async void GetEntitiesByQuery5_EntitiesFound()
    {
        var client = NewClient();
        var filters = new EntityFilter[] {new() {IdPattern = new Regex(nameof(GetEntitiesByQuery5_EntitiesFound))}};
        var entity1 = NewEntity(nameof(GetEntitiesByQuery5_EntitiesFound));
        var entity2 = NewEntity(nameof(GetEntitiesByQuery5_EntitiesFound), Fixture.EntityType + "1");
        var entity3 = NewEntity(nameof(GetEntitiesByQuery5_EntitiesFound) + "1");
        entity3.StringValue = "OtherTestValue";

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await client.CreateEntity(entity3);
        var (res, entities) = await client.GetEntitiesByQuery<UnitTestEntity>(filters, $"StringValue=={entity1.StringValue}");

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.Equal(entity1.Type, entities[0].Type);
        Assert.Equal(entity2.Type, entities[1].Type);
        Assert.True(await Fixture.EntityExists(entity3.Id));
    }
    #endregion

    #region DeleteEntity
    [Fact]
    public async void DeleteEntity1_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntity((UnitTestEntity?) null));
    }

    [Fact]
    public async void DeleteEntity1_InvalidType()
    {
        var client = NewClient();
        var entity = new InvalidEntity();

        await Assert.ThrowsAsync<FiwareContractException>(() => client.DeleteEntity(entity));
    }

    [Fact]
    public async void DeleteEntity1_MissingEntityId()
    {
        var client = NewClient();
        var entity = NewEntity();

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.DeleteEntity(entity));
    }

    [Fact]
    public async void DeleteEntity1_MissingEntityType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteEntity1_MissingEntityType), null);

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.DeleteEntity(entity));
    }

    [Fact]
    public async void DeleteEntity1_EntityNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteEntity1_EntityNotFound));

        var res = await client.DeleteEntity(entity);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
    }

    [Fact]
    public async void DeleteEntity1_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteEntity1_EntityFound));

        await client.CreateEntity(entity);
        var res = await client.DeleteEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity.Id));
    }

    [Fact]
    public async void DeleteEntity1_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntity(null));
    }

    [Fact]
    public async void DeleteEntity1_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntity(string.Empty));
    }

    [Fact]
    public async void DeleteEntity1_IdNotFound()
    {
        var client = NewClient();

        var res = await client.DeleteEntity(nameof(DeleteEntity1_IdNotFound));

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
    }

    [Fact]
    public async void DeleteEntity1_IdFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteEntity1_IdFound));

        await client.CreateEntity(entity);
        var res = await client.DeleteEntity(entity.Id);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity.Id));
    }

    [Fact]
    public async void DeleteEntity1_MultipleIdsFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntity1_MultipleIdsFound));
        var entity2 = NewEntity(nameof(DeleteEntity1_MultipleIdsFound), Fixture.EntityType + "2");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntity(entity1.Id);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.Conflict, res.Code);
        Assert.True(await Fixture.EntityExists(entity1.Id, entity1.Type));
        Assert.True(await Fixture.EntityExists(entity2.Id, entity2.Type));
    }

    [Fact]
    public async void DeleteEntity2_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntity(null, Fixture.EntityType));
    }

    [Fact]
    public async void DeleteEntity2_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntity(string.Empty, Fixture.EntityType));
    }

    [Fact]
    public async void DeleteEntity2_NullType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntity(nameof(DeleteEntity2_NullType), null));
    }

    [Fact]
    public async void DeleteEntity2_EmptyType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntity(nameof(DeleteEntity2_EmptyType), string.Empty));
    }

    [Fact]
    public async void DeleteEntity2_EntityNotFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntity2_EntityNotFound) + "1");
        var entity2 = NewEntity(nameof(DeleteEntity2_EntityNotFound), Fixture.EntityType + "2");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntity(nameof(DeleteEntity2_EntityNotFound), Fixture.EntityType);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.True(await Fixture.EntityExists(entity1.Id));
        Assert.True(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntity2_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteEntity2_EntityFound));

        await client.CreateEntity(entity);
        var res = await client.DeleteEntity(entity.Id, entity.Type);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity.Id));
    }

    [Fact]
    public async void DeleteEntity2_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntity2_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(DeleteEntity2_MultipleEntitiesFound), Fixture.EntityType + "2");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntity(entity1.Id, entity1.Type);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id, entity1.Type));
        Assert.True(await Fixture.EntityExists(entity2.Id, entity2.Type));
    }
    #endregion

    #region DeleteEntities
    [Fact]
    public async void DeleteEntities1_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntities((IEnumerable<UnitTestEntity>?) null));
    }

    [Fact]
    public async void DeleteEntities1_Empty()
    {
        var client = NewClient();
        var entities = Array.Empty<UnitTestEntity>();

        var res = await client.DeleteEntities(entities);

        Assert.True(res.IsBad);
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
    }

    [Fact]
    public async void DeleteEntities1_NullEntity()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity?> {null};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntities(entities));
    }

    [Fact]
    public async void DeleteEntities1_InvalidType()
    {
        var client = NewClient();
        var entities = new List<InvalidEntity> {new(), new(), new()};

        await Assert.ThrowsAsync<FiwareContractException>(() => client.DeleteEntities(entities));
    }

    [Fact]
    public async void DeleteEntities1_MissingEntityId()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity> {new(), new(), new()};

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.DeleteEntities(entities));
    }

    [Fact]
    public async void DeleteEntities1_MissingEntityType()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(DeleteEntities1_MissingEntityType) + "1", null),
            NewEntity(nameof(DeleteEntities1_MissingEntityType) + "2", null),
            NewEntity(nameof(DeleteEntities1_MissingEntityType) + "3", null)
        };

        await Assert.ThrowsAsync<FiwareSerializationException>(() => client.DeleteEntities(entities));
    }

    [Fact]
    public async void DeleteEntities1_DuplicateEntity()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(DeleteEntities1_DuplicateEntity)),
            NewEntity(nameof(DeleteEntities1_DuplicateEntity))
        };

        await client.CreateEntity(entities[0]);
        var res = await client.DeleteEntities(entities);

        //batch will process correctly but report last error
        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(entities[0].Id));
    }

    [Fact]
    public async void DeleteEntities1_EntitiesNotFound()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(DeleteEntities1_EntitiesNotFound) + "1"),
            NewEntity(nameof(DeleteEntities1_EntitiesNotFound) + "2"),
            NewEntity(nameof(DeleteEntities1_EntitiesNotFound) + "3")
        };

        var res = await client.DeleteEntities(entities);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(entities[0].Id));
        Assert.False(await Fixture.EntityExists(entities[1].Id));
        Assert.False(await Fixture.EntityExists(entities[2].Id));
    }

    [Fact]
    public async void DeleteEntities1_SomeEntitiesFound()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(DeleteEntities1_SomeEntitiesFound) + "1"),
            NewEntity(nameof(DeleteEntities1_SomeEntitiesFound) + "2"),
            NewEntity(nameof(DeleteEntities1_SomeEntitiesFound) + "3")
        };

        await client.CreateEntity(entities[0]);
        var res = await client.DeleteEntities(entities);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(entities[0].Id));
        Assert.False(await Fixture.EntityExists(entities[1].Id));
        Assert.False(await Fixture.EntityExists(entities[2].Id));
    }

    [Fact]
    public async void DeleteEntities1_SomeEntitiesFound2()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(DeleteEntities1_SomeEntitiesFound2) + "1"),
            NewEntity(nameof(DeleteEntities1_SomeEntitiesFound2) + "2"),
            NewEntity(nameof(DeleteEntities1_SomeEntitiesFound2) + "3")
        };

        //create only last entity to check whether the entire batch is processed or aborted early
        await client.CreateEntity(entities[2]);
        var res = await client.DeleteEntities(entities);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(entities[0].Id));
        Assert.False(await Fixture.EntityExists(entities[1].Id));
        Assert.False(await Fixture.EntityExists(entities[2].Id));
    }

    [Fact]
    public async void DeleteEntities1_AllEntitiesFound()
    {
        var client = NewClient();
        var entities = new List<UnitTestEntity>
        {
            NewEntity(nameof(DeleteEntities1_AllEntitiesFound) + "1"),
            NewEntity(nameof(DeleteEntities1_AllEntitiesFound) + "2"),
            NewEntity(nameof(DeleteEntities1_AllEntitiesFound) + "3")
        };

        await client.CreateEntities(entities);
        var res = await client.DeleteEntities(entities);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entities[0].Id));
        Assert.False(await Fixture.EntityExists(entities[1].Id));
        Assert.False(await Fixture.EntityExists(entities[2].Id));
    }

    [Fact]
    public async void DeleteEntities2_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntities((IEnumerable<string>?) null));
    }

    [Fact]
    public async void DeleteEntities2_EmptyCollection()
    {
        var client = NewClient();
        var ids = Array.Empty<string>();

        var res = await client.DeleteEntities(ids);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
    }

    [Fact]
    public async void DeleteEntities2_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntities(new string?[] {null}));
    }

    [Fact]
    public async void DeleteEntities2_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntities(new[] {string.Empty}));
    }

    [Fact]
    public async void DeleteEntities2_IdsNotFound()
    {
        var client = NewClient();
        var ids = new List<string>
        {
            nameof(DeleteEntities2_IdsNotFound) + "1",
            nameof(DeleteEntities2_IdsNotFound) + "2",
            nameof(DeleteEntities2_IdsNotFound) + "3"
        };

        var res = await client.DeleteEntities(ids);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
    }

    [Fact]
    public async void DeleteEntities2_SomeIdsFound()
    {
        var client = NewClient();
        var ids = new List<string>
        {
            nameof(DeleteEntities2_SomeIdsFound) + "1",
            nameof(DeleteEntities2_SomeIdsFound) + "2",
            nameof(DeleteEntities2_SomeIdsFound) + "3"
        };

        await client.CreateEntity(NewEntity(ids[0]));
        var res = await client.DeleteEntities(ids);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(ids[0]));
        Assert.False(await Fixture.EntityExists(ids[1]));
        Assert.False(await Fixture.EntityExists(ids[2]));
    }

    [Fact]
    public async void DeleteEntities2_SomeIdsFound2()
    {
        var client = NewClient();
        var ids = new List<string>
        {
            nameof(DeleteEntities2_SomeIdsFound2) + "1",
            nameof(DeleteEntities2_SomeIdsFound2) + "2",
            nameof(DeleteEntities2_SomeIdsFound2) + "3"
        };

        //create only last entity to check whether the entire batch is processed or aborted early
        await client.CreateEntity(NewEntity(ids[2]));
        var res = await client.DeleteEntities(ids);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(ids[0]));
        Assert.False(await Fixture.EntityExists(ids[1]));
        Assert.False(await Fixture.EntityExists(ids[2]));
    }

    [Fact]
    public async void DeleteEntities2_AllIdsFound()
    {
        var client = NewClient();
        var ids = new List<string>
        {
            nameof(DeleteEntities2_AllIdsFound) + "1",
            nameof(DeleteEntities2_AllIdsFound) + "2",
            nameof(DeleteEntities2_AllIdsFound) + "3"
        };

        await client.CreateEntity(NewEntity(ids[0]));
        await client.CreateEntity(NewEntity(ids[1]));
        await client.CreateEntity(NewEntity(ids[2]));
        var res = await client.DeleteEntities(ids);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(ids[0]));
        Assert.False(await Fixture.EntityExists(ids[1]));
        Assert.False(await Fixture.EntityExists(ids[2]));
    }

    [Fact]
    public async void DeleteEntities3_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntities((EntityFilter?) null));
    }

    [Fact]
    public async void DeleteEntities3_InvalidFilter()
    {
        var client = NewClient();
        var filter1 = new EntityFilter {Id = nameof(DeleteEntities3_InvalidFilter), IdPattern = new Regex(nameof(DeleteEntities3_InvalidFilter))};
        var filter2 = new EntityFilter {Type = Fixture.EntityType, TypePattern = new Regex(Fixture.EntityType)};

        var res1 = await client.DeleteEntities(filter1);
        var res2 = await client.DeleteEntities(filter2);

        Assert.True(res1.IsBad, res1.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res1.Code);
        Assert.True(res2.IsBad, res2.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res2.Code);
    }

    [Fact]
    public async void DeleteEntities3_EntityNotFound()
    {
        var client = NewClient();
        var filter = new EntityFilter {Id = nameof(DeleteEntities3_EntityNotFound)};

        var res = await client.DeleteEntities(filter);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
    }

    [Fact]
    public async void DeleteEntities3_ValidIdFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities3_ValidIdFilter));
        var entity2 = NewEntity(nameof(DeleteEntities3_ValidIdFilter) + "1");
        var filter = new EntityFilter {Id = entity1.Id};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.True(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities3_ValidIdPatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities3_ValidIdPatternFilter));
        var entity2 = NewEntity(nameof(DeleteEntities3_ValidIdPatternFilter) + "1");
        var filter = new EntityFilter {IdPattern = new Regex(entity1.Id)};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities3_ValidTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities3_ValidTypeFilter), Fixture.EntityType + nameof(DeleteEntities3_ValidTypeFilter));
        var entity2 = NewEntity(nameof(DeleteEntities3_ValidTypeFilter) + "1", Fixture.EntityType + nameof(DeleteEntities3_ValidTypeFilter));
        var filter = new EntityFilter {Type = entity1.Type};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities3_ValidTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities3_ValidTypePatternFilter), Fixture.EntityType + nameof(DeleteEntities3_ValidTypePatternFilter));
        var entity2 = NewEntity(nameof(DeleteEntities3_ValidTypePatternFilter) + "1", Fixture.EntityType + nameof(DeleteEntities3_ValidTypePatternFilter) + "1");
        var filter = new EntityFilter {TypePattern = new Regex(entity1.Type)};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities3_ValidIdTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities3_ValidIdTypeFilter));
        var entity2 = NewEntity(nameof(DeleteEntities3_ValidIdTypeFilter) + "1");
        var filter = new EntityFilter {Id = entity1.Id, Type = entity1.Type};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.True(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities3_ValidIdPatternTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities3_ValidIdPatternTypeFilter));
        var entity2 = NewEntity(nameof(DeleteEntities3_ValidIdPatternTypeFilter) + "1");
        var filter = new EntityFilter {IdPattern = new Regex(entity1.Id), Type = entity1.Type};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities3_ValidIdTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities3_ValidIdTypePatternFilter));
        var entity2 = NewEntity(nameof(DeleteEntities3_ValidIdTypePatternFilter), Fixture.EntityType + "1");
        var filter = new EntityFilter {Id = entity1.Id, TypePattern = new Regex(entity1.Type)};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filter);

        //FIWARE bug when combining id and typePattern
        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.True(await Fixture.EntityExists(entity1.Id, entity1.Type));
        Assert.True(await Fixture.EntityExists(entity2.Id, entity2.Type));
    }

    [Fact]
    public async void DeleteEntities3_ValidIdPatternTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities3_ValidIdPatternTypePatternFilter) + "1");
        var entity2 = NewEntity(nameof(DeleteEntities3_ValidIdPatternTypePatternFilter), Fixture.EntityType + "1");
        var filter = new EntityFilter {IdPattern = new Regex(entity2.Id), TypePattern = new Regex(entity1.Type)};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities3_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities3_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(DeleteEntities3_MultipleEntitiesFound), Fixture.EntityType + "1");
        var filter = new EntityFilter {Id = entity1.Id};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filter);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities4_NullObject()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteEntities((IEnumerable<EntityFilter>?) null));
    }

    [Fact]
    public async void DeleteEntities4_EmptyCollection()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.DeleteEntities(Array.Empty<EntityFilter>()));
    }

    [Fact]
    public async void DeleteEntities4_InvalidFilter()
    {
        var client = NewClient();
        var filters1 = new EntityFilter[] {new() {Id = nameof(DeleteEntities4_InvalidFilter), IdPattern = new Regex(nameof(DeleteEntities4_InvalidFilter))}};
        var filters2 = new EntityFilter[] {new() {Type = Fixture.EntityType + nameof(DeleteEntities4_InvalidFilter), TypePattern = new Regex(Fixture.EntityType + nameof(DeleteEntities4_InvalidFilter))}};

        var res1 = await client.DeleteEntities(filters1);
        var res2 = await client.DeleteEntities(filters2);

        Assert.True(res1.IsBad, res1.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res1.Code);
        //this is a bug with FIWARE queries
        //Assert.True(res2.IsBad, res2.Code.ToString());
        //Assert.Equal(HttpStatusCode.BadRequest, res2.Code);
        Assert.True(res2.IsBad, res2.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res2.Code);
    }

    [Fact]
    public async void DeleteEntities4_EntityNotFound()
    {
        var client = NewClient();
        var filters = new EntityFilter[] {new() {Id = nameof(DeleteEntities4_EntityNotFound)}};

        var res = await client.DeleteEntities(filters);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
    }

    [Fact]
    public async void DeleteEntities4_ValidIdFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities4_ValidIdFilter));
        var entity2 = NewEntity(nameof(DeleteEntities4_ValidIdFilter) + "1");
        var filters = new EntityFilter[] {new() {Id = entity1.Id}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.True(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities4_ValidIdPatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities4_ValidIdPatternFilter));
        var entity2 = NewEntity(nameof(DeleteEntities4_ValidIdPatternFilter) + "1");
        var filters = new EntityFilter[] {new() {IdPattern = new Regex(entity1.Id)}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities4_ValidTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities4_ValidTypeFilter), Fixture.EntityType + nameof(DeleteEntities4_ValidTypeFilter));
        var entity2 = NewEntity(nameof(DeleteEntities4_ValidTypeFilter) + "1", Fixture.EntityType + nameof(DeleteEntities4_ValidTypeFilter));
        var filters = new EntityFilter[] {new() {Type = entity1.Type}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities4_ValidTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities4_ValidTypePatternFilter), Fixture.EntityType + nameof(DeleteEntities4_ValidTypePatternFilter));
        var entity2 = NewEntity(nameof(DeleteEntities4_ValidTypePatternFilter) + "1", Fixture.EntityType + nameof(DeleteEntities4_ValidTypePatternFilter) + "1");
        var filters = new EntityFilter[] {new() {TypePattern = new Regex(entity1.Type)}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities4_ValidIdTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities4_ValidIdTypeFilter));
        var entity2 = NewEntity(nameof(DeleteEntities4_ValidIdTypeFilter) + "1");
        var filters = new EntityFilter[] {new() {Id = entity1.Id, Type = entity1.Type}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.True(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities4_ValidIdPatternTypeFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities4_ValidIdPatternTypeFilter));
        var entity2 = NewEntity(nameof(DeleteEntities4_ValidIdPatternTypeFilter) + "1");
        var filters = new EntityFilter[] {new() {IdPattern = new Regex(entity1.Id), Type = entity1.Type}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities4_ValidIdTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities4_ValidIdTypePatternFilter));
        var entity2 = NewEntity(nameof(DeleteEntities4_ValidIdTypePatternFilter), Fixture.EntityType + "1");
        var filters = new EntityFilter[] {new() {Id = entity1.Id, TypePattern = new Regex(entity1.Type)}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities4_ValidIdPatternTypePatternFilter()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities4_ValidIdPatternTypePatternFilter) + "1");
        var entity2 = NewEntity(nameof(DeleteEntities4_ValidIdPatternTypePatternFilter), Fixture.EntityType + "1");
        var filters = new EntityFilter[] {new() {IdPattern = new Regex(entity2.Id), TypePattern = new Regex(entity1.Type)}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }

    [Fact]
    public async void DeleteEntities4_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteEntities4_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(DeleteEntities4_MultipleEntitiesFound), Fixture.EntityType + "1");
        var filters = new EntityFilter[] {new() {Id = entity1.Id}};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteEntities(filters);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.False(await Fixture.EntityExists(entity1.Id));
        Assert.False(await Fixture.EntityExists(entity2.Id));
    }
    #endregion

    #region CreateAttribute
    [Fact]
    public async void CreateAttribute1_NullId()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(null, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void CreateAttribute1_EmptyId()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(string.Empty, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void CreateAttribute1_NullAttribute()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(nameof(CreateAttribute1_NullAttribute), null, attribute));
    }

    [Fact]
    public async void CreateAttribute1_EmptyAttribute()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(nameof(CreateAttribute1_EmptyAttribute), string.Empty, attribute));
    }

    [Fact]
    public async void CreateAttribute1_NullData()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(nameof(CreateAttribute1_NullData), nameof(UnitTestEntity.StringValue), null));
    }

    [Fact]
    public async void CreateAttribute1_EntityNotFound()
    {
        var client = NewClient();
        var attribute = new AttributeData
        {
            Value = "TestValue",
            Type = FiwareTypes.Text,
            Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
        };
        var attributeName = nameof(UnitTestEntity.StringValue);

        var res = await client.CreateAttribute(nameof(CreateAttribute1_EntityNotFound), attributeName, attribute);

        //if the entity does not yet exist, a new entity with the given ID and the default type "Thing" is created
        //Assert.True(res.IsBad, res.Code.ToString());
        //Assert.Equal(HttpStatusCode.NotFound, res.Code);
        //Assert.False(await Fixture.EntityExists(nameof(CreateAttribute1_EntityNotFound)));
        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(nameof(CreateAttribute1_EntityNotFound));
        Assert.NotNull(check);
        Assert.Equal("TestValue", check[attributeName]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check[attributeName]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttribute1_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttribute1_EntityFound));
        var attribute = new AttributeData
        {
            Value = "TestValue",
            Type = FiwareTypes.Text,
            Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
        };
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, attributeName, attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check[attributeName]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check[attributeName]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttribute1_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(CreateAttribute1_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(CreateAttribute1_MultipleEntitiesFound), Fixture.EntityType + "1");
        var attribute = new AttributeData
        {
            Value = "TestValue",
            Type = FiwareTypes.Text,
            Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
        };
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.CreateAttribute(entity1.Id, attributeName, attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.Conflict, res.Code);
        var check = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey(attributeName));
    }

    [Fact]
    public async void CreateAttribute1_AttributeAlreadyExists()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttribute1_AttributeAlreadyExists));
        var attribute = new AttributeData {Type = FiwareTypes.Text};

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.Code);
    }

    [Fact]
    public async void CreateAttribute1_NullDataValue()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttribute1_NullDataValue));
        var attribute = new AttributeData {Value = null, Type = FiwareTypes.Text};
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, attributeName, attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Null(check[attributeName]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttribute1_NullDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttribute1_NullDataType));
        var attribute = new AttributeData {Value = entity.StringValue, Type = null};
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, attributeName, attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(FiwareTypes.Text, check[attributeName]?["type"]?.ToString());
    }

    [Fact]
    public async void CreateAttribute1_EmptyDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttribute1_EmptyDataType));
        var attribute = new AttributeData {Value = entity.StringValue, Type = string.Empty};
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, attributeName, attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey(attributeName));
    }

    [Fact]
    public async void CreateAttribute1_DecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(CreateAttribute1_DecodeValue));
        var attribute = new AttributeData {Value = "<test>:=(value)", Type = FiwareTypes.Text};
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, attributeName, attribute, false);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(encoder.EncodeValue(attribute.Value.ToObject<string>()), check[attributeName]?["value"]?.ToString());
    }

    [Fact]
    public async void CreateAttribute1_SkipDecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(CreateAttribute1_SkipDecodeValue));
        var attribute = new AttributeData {Value = encoder.EncodeValue("<test>:=(value)"), Type = FiwareTypes.Text};
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, attributeName, attribute, true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(attribute.Value.ToObject<string>(), check[attributeName]?["value"]?.ToString());
    }

    [Fact]
    public async void CreateAttribute2_NullId()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(null, Fixture.EntityType, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void CreateAttribute2_EmptyId()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(string.Empty, Fixture.EntityType, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void CreateAttribute2_NullType()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(nameof(CreateAttribute2_NullType), null, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void CreateAttribute2_EmptyType()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(nameof(CreateAttribute2_EmptyType), string.Empty, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void CreateAttribute2_NullAttribute()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(nameof(CreateAttribute2_NullAttribute), Fixture.EntityType, null, attribute));
    }

    [Fact]
    public async void CreateAttribute2_EmptyAttribute()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(nameof(CreateAttribute2_EmptyAttribute), Fixture.EntityType, string.Empty, attribute));
    }

    [Fact]
    public async void CreateAttribute2_NullData()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttribute(nameof(CreateAttribute2_NullData), Fixture.EntityType, nameof(UnitTestEntity.StringValue), null));
    }

    [Fact]
    public async void CreateAttribute2_EntityNotFound()
    {
        var client = NewClient();
        var attribute = new AttributeData
        {
            Value = "TestValue",
            Type = FiwareTypes.Text,
            Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
        };
        var attributeName = nameof(UnitTestEntity.StringValue);

        var res = await client.CreateAttribute(nameof(CreateAttribute2_EntityNotFound), Fixture.EntityType, attributeName, attribute);

        //if the entity does not yet exist, a new entity with the given ID and type is created
        //Assert.True(res.IsBad, res.Code.ToString());
        //Assert.Equal(HttpStatusCode.NotFound, res.Code);
        //Assert.False(await Fixture.EntityExists(nameof(CreateAttribute2_EntityNotFound)));
        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(nameof(CreateAttribute2_EntityNotFound), Fixture.EntityType);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check[attributeName]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check[attributeName]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttribute2_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttribute2_EntityFound));
        var attribute = new AttributeData
        {
            Value = "TestValue",
            Type = FiwareTypes.Text,
            Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
        };
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, entity.Type, attributeName, attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check[attributeName]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check[attributeName]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttribute2_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(CreateAttribute2_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(CreateAttribute2_MultipleEntitiesFound), Fixture.EntityType + "1");
        var attribute = new AttributeData
        {
            Value = "TestValue",
            Type = FiwareTypes.Text,
            Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
        };
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.CreateAttribute(entity1.Id, entity1.Type, attributeName, attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check[attributeName]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttribute2_AttributeAlreadyExists()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttribute2_AttributeAlreadyExists));
        var attribute = new AttributeData {Type = FiwareTypes.Text};

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.Code);
    }

    [Fact]
    public async void CreateAttribute2_NullDataValue()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttribute2_NullDataValue));
        var attribute = new AttributeData {Value = null, Type = FiwareTypes.Text};
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, entity.Type, attributeName, attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Null(check[attributeName]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttribute2_NullDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttribute2_NullDataType));
        var attribute = new AttributeData {Value = entity.StringValue, Type = null};
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, entity.Type, attributeName, attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal(FiwareTypes.Text, check[attributeName]?["type"]?.ToString());
    }

    [Fact]
    public async void CreateAttribute2_EmptyDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttribute2_EmptyDataType));
        var attribute = new AttributeData {Value = entity.StringValue, Type = string.Empty};
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, entity.Type, attributeName, attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey(attributeName));
    }

    [Fact]
    public async void CreateAttribute2_DecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(CreateAttribute2_DecodeValue));
        var attribute = new AttributeData {Value = "<test>:=(value)", Type = FiwareTypes.Text};
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, entity.Type, attributeName, attribute, false);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal(encoder.EncodeValue(attribute.Value.ToObject<string>()), check[attributeName]?["value"]?.ToString());
    }

    [Fact]
    public async void CreateAttribute2_SkipDecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(CreateAttribute2_SkipDecodeValue));
        var attribute = new AttributeData {Value = encoder.EncodeValue("<test>:=(value)"), Type = FiwareTypes.Text};
        var attributeName = nameof(UnitTestEntity.StringValue) + "1";

        await client.CreateEntity(entity);
        var res = await client.CreateAttribute(entity.Id, entity.Type, attributeName, attribute, true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal(attribute.Value.ToObject<string>(), check[attributeName]?["value"]?.ToString());
    }
    #endregion

    #region CreateAttributes
    [Fact]
    public async void CreateAttributes1_NullId()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttributes(null, attributes));
    }

    [Fact]
    public async void CreateAttributes1_EmptyId()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttributes(string.Empty, attributes));
    }

    [Fact]
    public async void CreateAttributes1_NullAttributes()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttributes(nameof(CreateAttributes1_NullAttributes), null));
    }

    [Fact]
    public async void CreateAttributes1_EmptyAttributes()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        var res = await client.CreateAttributes(nameof(CreateAttributes1_EmptyAttributes), attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
    }

    [Fact]
    public async void CreateAttributes1_NullData()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData> {{"TestAttribute", null!}};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttributes(nameof(CreateAttributes1_NullData), attributes));
    }

    [Fact]
    public async void CreateAttributes1_EntityNotFound()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData
                {
                    Value = "TestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            }
        };

        var res = await client.CreateAttributes(nameof(CreateAttributes1_EntityNotFound), attributes);

        //if the entity does not yet exist, a new entity with the given ID and the default type "Thing" is created
        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(nameof(CreateAttributes1_EntityNotFound));
        Assert.NotNull(check);
        Assert.Equal("TestValue", check["TestAttribute"]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check["TestAttribute"]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttributes1_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes1_EntityFound));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData
                {
                    Value = "TestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check["TestAttribute"]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check["TestAttribute"]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttributes1_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(CreateAttributes1_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(CreateAttributes1_MultipleEntitiesFound), Fixture.EntityType + "1");
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData
                {
                    Value = "TestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            }
        };

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.CreateAttributes(entity1.Id, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.Conflict, res.Code);
        var check = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey("TestAttribute"));
    }

    [Fact]
    public async void CreateAttributes1_AttributeAlreadyExists()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes1_AttributeAlreadyExists));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                nameof(UnitTestEntity.StringValue), new AttributeData
                {
                    Value = "NewTestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            },
            {
                "TestAttribute", new AttributeData
                {
                    Value = "TestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check["TestAttribute"]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check["TestAttribute"]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
        Assert.Equal("TestValue", check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToObject<string>());
        Assert.False(check[nameof(UnitTestEntity.StringValue)]?["metadata"] is JObject obj && obj.ContainsKey("TestMetadata"));
    }

    [Fact]
    public async void CreateAttributes1_AttributeAlreadyExists2()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes1_AttributeAlreadyExists2));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData
                {
                    Value = "TestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            },
            {
                nameof(UnitTestEntity.StringValue), new AttributeData
                {
                    Value = "NewTestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check["TestAttribute"]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check["TestAttribute"]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
        Assert.Equal("TestValue", check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToObject<string>());
        Assert.False(check[nameof(UnitTestEntity.StringValue)]?["metadata"] is JObject obj && obj.ContainsKey("TestMetadata"));
    }

    [Fact]
    public async void CreateAttributes1_NullDataValue()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes1_NullDataValue));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData {Value = null, Type = FiwareTypes.Text}
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Null(check["TestAttribute"]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttributes1_NullDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes1_NullDataType));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData {Value = "TestValue", Type = null}
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(FiwareTypes.Text, check["TestAttribute"]?["type"]?.ToString());
    }

    [Fact]
    public async void CreateAttributes1_EmptyDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes1_EmptyDataType));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData {Value = "TestValue", Type = string.Empty}
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey("TestAttribute"));
    }

    [Fact]
    public async void CreateAttributes1_DecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(CreateAttributes1_DecodeValue));
        var attributeValue = "<test>:=(value)";
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData {Value = attributeValue, Type = FiwareTypes.Text}
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, attributes, false);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(encoder.EncodeValue(attributeValue), check["TestAttribute"]?["value"]?.ToString());
    }

    [Fact]
    public async void CreateAttributes1_SkipDecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(CreateAttributes1_SkipDecodeValue));
        var attributeValue = encoder.EncodeValue("<test>:=(value)");
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData {Value = attributeValue, Type = FiwareTypes.Text}
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, attributes, true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(attributeValue, check["TestAttribute"]?["value"]?.ToString());
    }

    [Fact]
    public async void CreateAttributes2_NullId()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttributes(null, Fixture.EntityType, attributes));
    }

    [Fact]
    public async void CreateAttributes2_EmptyId()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttributes(string.Empty, Fixture.EntityType, attributes));
    }

    [Fact]
    public async void CreateAttributes2_NullType()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttributes(nameof(CreateAttributes2_NullType), null, attributes));
    }

    [Fact]
    public async void CreateAttributes2_EmptyType()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttributes(nameof(CreateAttributes2_EmptyType), string.Empty, attributes));
    }

    [Fact]
    public async void CreateAttributes2_NullAttributes()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttributes(nameof(CreateAttributes2_NullAttributes), Fixture.EntityType, null));
    }

    [Fact]
    public async void CreateAttributes2_EmptyAttributes()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        var res = await client.CreateAttributes(nameof(CreateAttributes2_EmptyAttributes), Fixture.EntityType, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
    }

    [Fact]
    public async void CreateAttributes2_NullData()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData> {{"TestAttribute", null!}};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAttributes(nameof(CreateAttributes2_NullData), Fixture.EntityType, attributes));
    }

    [Fact]
    public async void CreateAttributes2_EntityNotFound()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData
                {
                    Value = "TestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            }
        };

        var res = await client.CreateAttributes(nameof(CreateAttributes2_EntityNotFound), Fixture.EntityType, attributes);

        //if the entity does not yet exist, a new entity with the given ID and type is created
        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(nameof(CreateAttributes2_EntityNotFound), Fixture.EntityType);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check["TestAttribute"]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check["TestAttribute"]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttributes2_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes2_EntityFound));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData
                {
                    Value = "TestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, entity.Type, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check["TestAttribute"]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check["TestAttribute"]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttributes2_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(CreateAttributes2_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(CreateAttributes2_MultipleEntitiesFound), Fixture.EntityType + "1");
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData
                {
                    Value = "TestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            }
        };

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.CreateAttributes(entity1.Id, entity1.Type, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check["TestAttribute"]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check["TestAttribute"]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttributes2_AttributeAlreadyExists()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes2_AttributeAlreadyExists));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                nameof(UnitTestEntity.StringValue), new AttributeData
                {
                    Value = "NewTestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            },
            {
                "TestAttribute", new AttributeData
                {
                    Value = "TestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, entity.Type, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check["TestAttribute"]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check["TestAttribute"]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
        Assert.Equal("TestValue", check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToObject<string>());
        Assert.False(check[nameof(UnitTestEntity.StringValue)]?["metadata"] is JObject obj && obj.ContainsKey("TestMetadata"));
    }

    [Fact]
    public async void CreateAttributes2_AttributeAlreadyExists2()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes2_AttributeAlreadyExists2));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData
                {
                    Value = "TestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            },
            {
                nameof(UnitTestEntity.StringValue), new AttributeData
                {
                    Value = "NewTestValue",
                    Type = FiwareTypes.Text,
                    Metadata = new MetadataCollection {{"TestMetadata", new MetadataItem("TestMetadataValue")}}
                }
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, entity.Type, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal("TestValue", check["TestAttribute"]?["value"]?.ToObject<string>());
        Assert.Equal("TestMetadataValue", check["TestAttribute"]?["metadata"]?["TestMetadata"]?["value"]?.ToObject<string>());
        Assert.Equal("TestValue", check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToObject<string>());
        Assert.False(check[nameof(UnitTestEntity.StringValue)]?["metadata"] is JObject obj && obj.ContainsKey("TestMetadata"));
    }

    [Fact]
    public async void CreateAttributes2_NullDataValue()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes2_NullDataValue));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData {Value = null, Type = FiwareTypes.Text}
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, entity.Type, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Null(check["TestAttribute"]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void CreateAttributes2_NullDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes2_NullDataType));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData {Value = "TestValue", Type = null}
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, entity.Type, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal(FiwareTypes.Text, check["TestAttribute"]?["type"]?.ToString());
    }

    [Fact]
    public async void CreateAttributes2_EmptyDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(CreateAttributes2_EmptyDataType));
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData {Value = "TestValue", Type = string.Empty}
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, entity.Type, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey("TestAttribute"));
    }

    [Fact]
    public async void CreateAttributes2_DecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(CreateAttributes2_DecodeValue));
        var attributeValue = "<test>:=(value)";
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData {Value = attributeValue, Type = FiwareTypes.Text}
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, entity.Type, attributes, false);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal(encoder.EncodeValue(attributeValue), check["TestAttribute"]?["value"]?.ToString());
    }

    [Fact]
    public async void CreateAttributes2_SkipDecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(CreateAttributes2_SkipDecodeValue));
        var attributeValue = encoder.EncodeValue("<test>:=(value)");
        var attributes = new Dictionary<string, AttributeData>
        {
            {
                "TestAttribute", new AttributeData {Value = attributeValue, Type = FiwareTypes.Text}
            }
        };

        await client.CreateEntity(entity);
        var res = await client.CreateAttributes(entity.Id, entity.Type, attributes, true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal(attributeValue, check["TestAttribute"]?["value"]?.ToString());
    }
    #endregion

    #region UpdateAttribute
    [Fact]
    public async void UpdateAttribute1_NullId()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(null, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void UpdateAttribute1_EmptyId()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(string.Empty, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void UpdateAttribute1_NullAttribute()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(nameof(UpdateAttribute1_NullAttribute), null, attribute));
    }

    [Fact]
    public async void UpdateAttribute1_EmptyAttribute()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(nameof(UpdateAttribute1_EmptyAttribute), string.Empty, attribute));
    }

    [Fact]
    public async void UpdateAttribute1_NullData()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(nameof(UpdateAttribute1_NullData), nameof(UnitTestEntity.StringValue), null));
    }

    [Fact]
    public async void UpdateAttribute1_EntityNotFound()
    {
        var client = NewClient();
        var attribute = new AttributeData {Type = FiwareTypes.Text};

        var res = await client.UpdateAttribute(nameof(UpdateAttribute1_EntityNotFound), nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(nameof(UpdateAttribute1_EntityNotFound)));
    }

    [Fact]
    public async void UpdateAttribute1_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(UpdateAttribute1_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(UpdateAttribute1_MultipleEntitiesFound), Fixture.EntityType + "1");
        var attribute = new AttributeData {Value = "NewTestValue", Type = FiwareTypes.Text};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.UpdateAttribute(entity1.Id, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.Conflict, res.Code);
        var check = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check);
        Assert.True(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
        Assert.Equal("TestValue", check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void UpdateAttribute1_AttributeNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute1_AttributeNotFound));
        var attribute = new AttributeData {Type = FiwareTypes.Text};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, nameof(UnitTestEntity.StringValue) + "1", attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.Code);
    }

    [Fact]
    public async void UpdateAttribute1_NullDataValue()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute1_NullDataValue));
        var attribute = new AttributeData {Value = null, Type = FiwareTypes.Text};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Null(check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void UpdateAttribute1_NullDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute1_NullDataType));
        var attribute = new AttributeData {Value = entity.StringValue, Type = null};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(FiwareTypes.Text, check[nameof(UnitTestEntity.StringValue)]?["type"]?.ToString());
    }

    [Fact]
    public async void UpdateAttribute1_EmptyDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute1_EmptyDataType));
        var attribute = new AttributeData {Value = entity.StringValue, Type = string.Empty};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(FiwareTypes.Text, check[nameof(UnitTestEntity.StringValue)]?["type"]?.ToString());
    }

    [Fact]
    public async void UpdateAttribute1_MergeMetadata()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute1_MergeMetadata));
        entity.StringValueMetadata.Add("test", new MetadataItem("value", FiwareTypes.Text));
        var attribute = new AttributeData {Value = entity.StringValue, Type = FiwareTypes.Text};
        attribute.Metadata.Add("test1", new MetadataItem("value", FiwareTypes.Text));

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.NotNull(check[nameof(UnitTestEntity.StringValue)]?["metadata"]?["test"]);
        Assert.NotNull(check[nameof(UnitTestEntity.StringValue)]?["metadata"]?["test1"]);
    }

    [Fact]
    public async void UpdateAttribute1_OverrideMetadata()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute1_OverrideMetadata));
        entity.StringValueMetadata.Add("test", new MetadataItem("value", FiwareTypes.Text));
        var attribute = new AttributeData {Value = entity.StringValue, Type = FiwareTypes.Text};
        attribute.Metadata.Add("test", new MetadataItem("value1", FiwareTypes.TextUnrestricted));

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, nameof(UnitTestEntity.StringValue), attribute, true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal("value1", check[nameof(UnitTestEntity.StringValue)]?["metadata"]?["test"]?["value"]?.ToString());
        Assert.Equal(FiwareTypes.TextUnrestricted, check[nameof(UnitTestEntity.StringValue)]?["metadata"]?["test"]?["type"]?.ToString());
    }

    [Fact]
    public async void UpdateAttribute1_DecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(UpdateAttribute1_DecodeValue));
        var attribute = new AttributeData {Value = "<test>:=(value)", Type = FiwareTypes.Text};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, nameof(UnitTestEntity.StringValue), attribute, false, false);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(encoder.EncodeValue(attribute.Value.ToObject<string>()), check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToString());
    }

    [Fact]
    public async void UpdateAttribute1_SkipDecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(UpdateAttribute1_SkipDecodeValue));
        var attribute = new AttributeData {Value = encoder.EncodeValue("<test>:=(value)"), Type = FiwareTypes.Text};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, nameof(UnitTestEntity.StringValue), attribute, false, true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Equal(attribute.Value.ToObject<string>(), check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToString());
    }

    [Fact]
    public async void UpdateAttribute2_NullId()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(null, Fixture.EntityType, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void UpdateAttribute2_EmptyId()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(string.Empty, Fixture.EntityType, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void UpdateAttribute2_NullType()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(nameof(UpdateAttribute2_NullType), null, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void UpdateAttribute2_EmptyType()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(nameof(UpdateAttribute2_EmptyType), string.Empty, nameof(UnitTestEntity.StringValue), attribute));
    }

    [Fact]
    public async void UpdateAttribute2_NullAttribute()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(nameof(UpdateAttribute2_NullAttribute), Fixture.EntityType, null, attribute));
    }

    [Fact]
    public async void UpdateAttribute2_EmptyAttribute()
    {
        var client = NewClient();
        var attribute = new AttributeData();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(nameof(UpdateAttribute2_EmptyAttribute), Fixture.EntityType, string.Empty, attribute));
    }

    [Fact]
    public async void UpdateAttribute2_NullData()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttribute(nameof(UpdateAttribute2_NullData), Fixture.EntityType, nameof(UnitTestEntity.StringValue), null));
    }

    [Fact]
    public async void UpdateAttribute2_EntityNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute2_EntityNotFound), Fixture.EntityType + nameof(UpdateAttribute2_EntityNotFound));
        var attribute = new AttributeData {Type = FiwareTypes.Text};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(nameof(UpdateAttribute2_EntityNotFound), Fixture.EntityType, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(entity.Id, Fixture.EntityType));
        Assert.True(await Fixture.EntityExists(entity.Id, entity.Type));
    }

    [Fact]
    public async void UpdateAttribute2_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(UpdateAttribute2_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(UpdateAttribute2_MultipleEntitiesFound), Fixture.EntityType + "1");
        var attribute = new AttributeData {Value = "NewTestValue", Type = FiwareTypes.Text};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.UpdateAttribute(entity1.Id, entity1.Type, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check);
        Assert.Equal("NewTestValue", check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void UpdateAttribute2_AttributeNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute2_AttributeNotFound));
        var attribute = new AttributeData {Type = FiwareTypes.Text};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue) + "1", attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.Code);
    }

    [Fact]
    public async void UpdateAttribute2_NullDataValue()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute2_NullDataValue));
        var attribute = new AttributeData {Value = null, Type = FiwareTypes.Text};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.Null(check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToObject<string>());
    }

    [Fact]
    public async void UpdateAttribute2_NullDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute2_NullDataType));
        var attribute = new AttributeData {Value = entity.StringValue, Type = null};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal(FiwareTypes.Text, check[nameof(UnitTestEntity.StringValue)]?["type"]?.ToString());
    }

    [Fact]
    public async void UpdateAttribute2_EmptyDataType()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute2_EmptyDataType));
        var attribute = new AttributeData {Value = entity.StringValue, Type = string.Empty};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal(FiwareTypes.Text, check[nameof(UnitTestEntity.StringValue)]?["type"]?.ToString());
    }

    [Fact]
    public async void UpdateAttribute2_MergeMetadata()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute2_MergeMetadata));
        entity.StringValueMetadata.Add("test", new MetadataItem("value", FiwareTypes.Text));
        var attribute = new AttributeData {Value = entity.StringValue, Type = FiwareTypes.Text};
        attribute.Metadata.Add("test1", new MetadataItem("value", FiwareTypes.Text));

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue), attribute);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.NotNull(check[nameof(UnitTestEntity.StringValue)]?["metadata"]?["test"]);
        Assert.NotNull(check[nameof(UnitTestEntity.StringValue)]?["metadata"]?["test1"]);
    }

    [Fact]
    public async void UpdateAttribute2_OverrideMetadata()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(UpdateAttribute2_OverrideMetadata));
        entity.StringValueMetadata.Add("test", new MetadataItem("value", FiwareTypes.Text));
        var attribute = new AttributeData {Value = entity.StringValue, Type = FiwareTypes.Text};
        attribute.Metadata.Add("test", new MetadataItem("value1", FiwareTypes.TextUnrestricted));

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue), attribute, true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal("value1", check[nameof(UnitTestEntity.StringValue)]?["metadata"]?["test"]?["value"]?.ToString());
        Assert.Equal(FiwareTypes.TextUnrestricted, check[nameof(UnitTestEntity.StringValue)]?["metadata"]?["test"]?["type"]?.ToString());
    }

    [Fact]
    public async void UpdateAttribute2_DecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(UpdateAttribute2_DecodeValue));
        var attribute = new AttributeData {Value = "<test>:=(value)", Type = FiwareTypes.Text};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue), attribute, false, false);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal(encoder.EncodeValue(attribute.Value.ToObject<string>()), check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToString());
    }

    [Fact]
    public async void UpdateAttribute2_SkipDecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(UpdateAttribute2_SkipDecodeValue));
        var attribute = new AttributeData {Value = encoder.EncodeValue("<test>:=(value)"), Type = FiwareTypes.Text};

        await client.CreateEntity(entity);
        var res = await client.UpdateAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue), attribute, false, true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal(attribute.Value.ToObject<string>(), check[nameof(UnitTestEntity.StringValue)]?["value"]?.ToString());
    }
    #endregion

    #region UpdateAttributes
    [Fact]
    public async void UpdateAttributes1_NullId()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttributes(null, attributes));
    }

    [Fact]
    public async void UpdateAttributes1_EmptyId()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttributes(string.Empty, attributes));
    }

    [Fact]
    public async void UpdateAttributes1_NullAttributes()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttributes(nameof(UpdateAttributes1_NullAttributes), null));
    }

    [Fact]
    public async void UpdateAttributes1_EmptyAttributes()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        var res = await client.UpdateAttributes(nameof(UpdateAttributes1_EmptyAttributes), attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
    }

    [Fact]
    public async void UpdateAttributes1_NullData()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData> {{"TestAttribute", null!}};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttributes(nameof(UpdateAttributes1_NullData), attributes));
    }

    //todo


    [Fact]
    public async void UpdateAttributes2_NullId()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttributes(null, Fixture.EntityType, attributes));
    }

    [Fact]
    public async void UpdateAttributes2_EmptyId()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttributes(string.Empty, Fixture.EntityType, attributes));
    }

    [Fact]
    public async void UpdateAttributes2_NullType()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttributes(nameof(UpdateAttributes2_NullType), null, attributes));
    }

    [Fact]
    public async void UpdateAttributes2_EmptyType()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttributes(nameof(UpdateAttributes2_EmptyType), string.Empty, attributes));
    }

    [Fact]
    public async void UpdateAttributes2_NullAttributes()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttributes(nameof(UpdateAttributes2_NullAttributes), Fixture.EntityType, null));
    }

    [Fact]
    public async void UpdateAttributes2_EmptyAttributes()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData>();

        var res = await client.UpdateAttributes(nameof(UpdateAttributes2_EmptyAttributes), Fixture.EntityType, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, res.Code);
    }

    [Fact]
    public async void UpdateAttributes2_NullData()
    {
        var client = NewClient();
        var attributes = new Dictionary<string, AttributeData> {{"TestAttribute", null!}};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAttributes(nameof(UpdateAttributes2_NullData), Fixture.EntityType, attributes));
    }

    //todo
    #endregion

    #region UpdateAttributeValue
    //todo
    #endregion

    #region CreateOrUpdateAttribute
    //todo
    #endregion

    #region CreateOrUpdateAttributes
    //todo
    #endregion

    #region ReplaceAttributes
    //todo
    #endregion

    #region GetAttribute
    [Fact]
    public async void GetAttribute1_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttribute(null, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttribute1_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttribute(string.Empty, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttribute1_NullAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttribute(nameof(GetAttribute1_NullAttribute), null));
    }

    [Fact]
    public async void GetAttribute1_EmptyAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttribute(nameof(GetAttribute1_EmptyAttribute), string.Empty));
    }

    [Fact]
    public async void GetAttribute1_EntityNotFound()
    {
        var client = NewClient();

        var (res, attr) = await client.GetAttribute(nameof(GetAttribute1_EntityNotFound), nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.Null(attr);
        Assert.False(await Fixture.EntityExists(nameof(GetAttribute1_EntityNotFound)));
    }

    [Fact]
    public async void GetAttribute1_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetAttribute1_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(GetAttribute1_MultipleEntitiesFound), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, attr) = await client.GetAttribute(entity1.Id, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.Conflict, res.Code);
        Assert.Null(attr);
        var check = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check);
        Assert.True(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttribute1_AttributeNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetAttribute1_AttributeNotFound));

        await client.CreateEntity(entity);
        var (res, attr) = await client.GetAttribute(entity.Id, nameof(UnitTestEntity.StringValue) + "1");

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.Null(attr);
    }

    [Fact]
    public async void GetAttribute1_DecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(GetAttribute1_DecodeValue));
        entity.StringValue = "<test>:=(value)";

        await client.CreateEntity(entity);
        var (res, attr) = await client.GetAttribute(entity.Id, nameof(UnitTestEntity.StringValue), false);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(attr);
        Assert.Equal(entity.StringValue, attr.Value.ToObject<string>());
    }

    [Fact]
    public async void GetAttribute1_SkipDecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(GetAttribute1_SkipDecodeValue));
        entity.StringValue = "<test>:=(value)";

        await client.CreateEntity(entity);
        var (res, attr) = await client.GetAttribute(entity.Id, nameof(UnitTestEntity.StringValue), true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(attr);
        Assert.Equal(encoder.EncodeValue(entity.StringValue), attr.Value.ToObject<string>());
    }

    [Fact]
    public async void GetAttribute1_Metadata()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(GetAttribute1_Metadata));
        entity.StringValue = "<test>:=(value)";
        entity.StringValueMetadata = new MetadataCollection
        {
            ["test"] = new("TestText", FiwareTypes.Text)
        };

        await client.CreateEntity(entity);
        var (res, attr) = await client.GetAttribute(entity.Id, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(attr);
        Assert.Equal(entity.StringValue, attr.Value.ToObject<string>());
        Assert.Equal(entity.StringValueMetadata["test"].Value.ToObject<string>(), attr.Metadata["test"].Value.ToObject<string>());
    }

    [Fact]
    public async void GetAttribute2_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttribute(null, Fixture.EntityType, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttribute2_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttribute(string.Empty, Fixture.EntityType, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttribute2_NullType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttribute(nameof(GetAttribute2_NullType), null, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttribute2_EmptyType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttribute(nameof(GetAttribute2_EmptyType), string.Empty, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttribute2_NullAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttribute(nameof(GetAttribute2_NullAttribute), Fixture.EntityType, null));
    }

    [Fact]
    public async void GetAttribute2_EmptyAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttribute(nameof(GetAttribute2_EmptyAttribute), Fixture.EntityType, string.Empty));
    }

    [Fact]
    public async void GetAttribute2_EntityNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetAttribute2_EntityNotFound), Fixture.EntityType + "1");

        await client.CreateEntity(entity);
        var (res, attr) = await client.GetAttribute(entity.Id, Fixture.EntityType, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.Null(attr);
        Assert.False(await Fixture.EntityExists(entity.Id, Fixture.EntityType));
        Assert.True(await Fixture.EntityExists(entity.Id, entity.Type));
    }

    [Fact]
    public async void GetAttribute2_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(GetAttribute2_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(GetAttribute2_MultipleEntitiesFound), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, attr) = await client.GetAttribute(entity1.Id, entity1.Type, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(attr);
        Assert.Equal(entity1.StringValue, attr.Value.ToObject<string>());
    }

    [Fact]
    public async void GetAttribute2_AttributeNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetAttribute2_AttributeNotFound));

        await client.CreateEntity(entity);
        var (res, attr) = await client.GetAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue) + "1");

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.Null(attr);
    }

    [Fact]
    public async void GetAttribute2_DecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(GetAttribute2_DecodeValue));
        entity.StringValue = "<test>:=(value)";

        await client.CreateEntity(entity);
        var (res, attr) = await client.GetAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue), false);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(attr);
        Assert.Equal(entity.StringValue, attr.Value.ToObject<string>());
    }

    [Fact]
    public async void GetAttribute2_SkipDecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(GetAttribute2_SkipDecodeValue));
        entity.StringValue = "<test>:=(value)";

        await client.CreateEntity(entity);
        var (res, attr) = await client.GetAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue), true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(attr);
        Assert.Equal(encoder.EncodeValue(entity.StringValue), attr.Value.ToObject<string>());
    }

    [Fact]
    public async void GetAttribute2_Metadata()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(GetAttribute2_Metadata));
        entity.StringValue = "<test>:=(value)";
        entity.StringValueMetadata = new MetadataCollection
        {
            ["test"] = new("TestText", FiwareTypes.Text)
        };

        await client.CreateEntity(entity);
        var (res, attr) = await client.GetAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.NotNull(attr);
        Assert.Equal(entity.StringValue, attr.Value.ToObject<string>());
        Assert.Equal(entity.StringValueMetadata["test"].Value.ToObject<string>(), attr.Metadata["test"].Value.ToObject<string>());
    }
    #endregion

    #region GetAttributes
    //todo
    #endregion

    #region GetAttributeValue
    [Fact]
    public async void GetAttributeValue1_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttributeValue<string>(null, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttributeValue1_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttributeValue<string>(string.Empty, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttributeValue1_NullAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttributeValue<string>(nameof(GetAttributeValue1_NullAttribute), null));
    }

    [Fact]
    public async void GetAttributeValue1_EmptyAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttributeValue<string>(nameof(GetAttributeValue1_EmptyAttribute), string.Empty));
    }

    [Fact]
    public async void GetAttributeValue1_EntityNotFound()
    {
        var client = NewClient();

        var (res, value) = await client.GetAttributeValue<string>(nameof(GetAttributeValue1_EntityNotFound), nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.Null(value);
        Assert.False(await Fixture.EntityExists(nameof(GetAttributeValue1_EntityNotFound)));
    }

    [Fact]
    public async void GetAttributeValue1_AttributeNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetAttributeValue1_AttributeNotFound));

        await client.CreateEntity(entity);
        var (res, value) = await client.GetAttributeValue<string>(entity.Id, nameof(UnitTestEntity.StringValue) + "1");

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.Null(value);
    }

    [Fact]
    public async void GetAttributeValue1_DecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(GetAttributeValue1_DecodeValue));
        entity.StringValue = "<test>:=(value)";

        await client.CreateEntity(entity);
        var (res, value) = await client.GetAttributeValue<string>(entity.Id, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.Equal(entity.StringValue, value);
    }

    [Fact]
    public async void GetAttributeValue1_SkipDecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(GetAttributeValue1_SkipDecodeValue));
        entity.StringValue = "<test>:=(value)";

        await client.CreateEntity(entity);
        var (res, value) = await client.GetAttributeValue<string>(entity.Id, nameof(UnitTestEntity.StringValue), true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.Equal(encoder.EncodeValue(entity.StringValue), value);
    }

    [Fact]
    public async void GetAttributeValue2_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttributeValue<string>(null, Fixture.EntityType, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttributeValue2_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttributeValue<string>(string.Empty, Fixture.EntityType, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttributeValue2_NullType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttributeValue<string>(nameof(GetAttributeValue2_NullType), null, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttributeValue2_EmptyType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttributeValue<string>(nameof(GetAttributeValue2_EmptyType), string.Empty, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void GetAttributeValue2_NullAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttributeValue<string>(nameof(GetAttributeValue2_NullAttribute), Fixture.EntityType, null));
    }

    [Fact]
    public async void GetAttributeValue2_EmptyAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAttributeValue<string>(nameof(GetAttributeValue2_EmptyAttribute), Fixture.EntityType, string.Empty));
    }

    [Fact]
    public async void GetAttributeValue2_EntityNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetAttributeValue2_EntityNotFound), Fixture.EntityType + nameof(GetAttributeValue2_EntityNotFound));

        await client.CreateEntity(entity);
        var (res, value) = await client.GetAttributeValue<string>(entity.Id, Fixture.EntityType, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.Null(value);
        Assert.False(await Fixture.EntityExists(entity.Id, Fixture.EntityType));
        Assert.True(await Fixture.EntityExists(entity.Id, entity.Type));
    }

    [Fact]
    public async void GetAttributeValue2_AttributeNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(GetAttributeValue2_AttributeNotFound));

        await client.CreateEntity(entity);
        var (res, value) = await client.GetAttributeValue<string>(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue) + "1");

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.Null(value);
    }

    [Fact]
    public async void GetAttributeValue2_DecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(GetAttributeValue2_DecodeValue));
        entity.StringValue = "<test>:=(value)";

        await client.CreateEntity(entity);
        var (res, value) = await client.GetAttributeValue<string>(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.Equal(entity.StringValue, value);
    }

    [Fact]
    public async void GetAttributeValue2_SkipDecodeValue()
    {
        var encoder = new DollarStringEncoder();
        var client = NewClient(new FiwareSettings {StringValueEncoder = encoder});
        var entity = NewEntity(nameof(GetAttributeValue2_SkipDecodeValue));
        entity.StringValue = "<test>:=(value)";

        await client.CreateEntity(entity);
        var (res, value) = await client.GetAttributeValue<string>(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue), true);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.OK, res.Code);
        Assert.Equal(encoder.EncodeValue(entity.StringValue), value);
    }
    #endregion

    #region DeleteAttribute
    [Fact]
    public async void DeleteAttribute1_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttribute(null, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttribute1_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttribute(string.Empty, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttribute1_NullAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttribute(nameof(DeleteAttribute1_NullAttribute), null));
    }

    [Fact]
    public async void DeleteAttribute1_EmptyAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttribute(nameof(DeleteAttribute1_EmptyAttribute), string.Empty));
    }

    [Fact]
    public async void DeleteAttribute1_EntityNotFound()
    {
        var client = NewClient();

        var res = await client.DeleteAttribute(nameof(DeleteAttribute1_EntityNotFound), nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(nameof(DeleteAttribute1_EntityNotFound)));
    }

    [Fact]
    public async void DeleteAttribute1_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteAttribute1_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(DeleteAttribute1_MultipleEntitiesFound), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteAttribute(entity1.Id, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.Conflict, res.Code);
        var check = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check);
        Assert.True(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttribute1_AttributeNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttribute1_AttributeNotFound));

        await client.CreateEntity(entity);
        var res = await client.DeleteAttribute(entity.Id, nameof(UnitTestEntity.StringValue) + "1");

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
    }

    [Fact]
    public async void DeleteAttribute1_AttributeFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttribute1_AttributeFound));

        await client.CreateEntity(entity);
        var res = await client.DeleteAttribute(entity.Id, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttribute1_ProtectedAttribute()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttribute1_AttributeFound));

        await client.CreateEntity(entity);
        var res1 = await client.DeleteAttribute(entity.Id, "id");
        var res2 = await client.DeleteAttribute(entity.Id, "type");
        var res3 = await client.DeleteAttribute(entity.Id, "dateCreated");
        var res4 = await client.DeleteAttribute(entity.Id, "dateModified");

        Assert.True(res1.IsBad, res1.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res1.Code);
        Assert.True(res2.IsBad, res2.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res2.Code);
        Assert.True(res3.IsBad, res3.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res3.Code);
        Assert.True(res4.IsBad, res4.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res4.Code);
    }

    [Fact]
    public async void DeleteAttribute2_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttribute(null, Fixture.EntityType, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttribute2_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttribute(string.Empty, Fixture.EntityType, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttribute2_NullType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttribute(nameof(DeleteAttribute2_NullType), null, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttribute2_EmptyType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttribute(nameof(DeleteAttribute2_EmptyType), string.Empty, nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttribute2_NullAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttribute(nameof(DeleteAttribute2_NullAttribute), Fixture.EntityType, null));
    }

    [Fact]
    public async void DeleteAttribute2_EmptyAttribute()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttribute(nameof(DeleteAttribute2_EmptyAttribute), Fixture.EntityType, string.Empty));
    }

    [Fact]
    public async void DeleteAttribute2_EntityNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttribute2_EntityNotFound), Fixture.EntityType + nameof(DeleteAttribute2_EntityNotFound));

        await client.CreateEntity(entity);
        var res = await client.DeleteAttribute(entity.Id, Fixture.EntityType, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(entity.Id, Fixture.EntityType));
        Assert.True(await Fixture.EntityExists(entity.Id, entity.Type));
    }

    [Fact]
    public async void DeleteAttribute2_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteAttribute2_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(DeleteAttribute2_MultipleEntitiesFound), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteAttribute(entity1.Id, entity1.Type, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttribute2_AttributeNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttribute2_AttributeNotFound));

        await client.CreateEntity(entity);
        var res = await client.DeleteAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue) + "1");

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
    }

    [Fact]
    public async void DeleteAttribute2_AttributeFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttribute2_AttributeFound));

        await client.CreateEntity(entity);
        var res = await client.DeleteAttribute(entity.Id, entity.Type, nameof(UnitTestEntity.StringValue));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttribute2_ProtectedAttribute()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttribute2_ProtectedAttribute));

        await client.CreateEntity(entity);
        var res1 = await client.DeleteAttribute(entity.Id, entity.Type, "id");
        var res2 = await client.DeleteAttribute(entity.Id, entity.Type, "type");
        var res3 = await client.DeleteAttribute(entity.Id, entity.Type, "dateCreated");
        var res4 = await client.DeleteAttribute(entity.Id, entity.Type, "dateModified");

        Assert.True(res1.IsBad, res1.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res1.Code);
        Assert.True(res2.IsBad, res2.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res2.Code);
        Assert.True(res3.IsBad, res3.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res3.Code);
        Assert.True(res4.IsBad, res4.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res4.Code);
    }
    #endregion

    #region DeleteAttributes
    [Fact]
    public async void DeleteAttributes1_NullId()
    {
        var client = NewClient();
        var attributes = Array.Empty<string>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttributes(null, attributes));
    }

    [Fact]
    public async void DeleteAttributes1_EmptyId()
    {
        var client = NewClient();
        var attributes = Array.Empty<string>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttributes(string.Empty, attributes));
    }

    [Fact]
    public async void DeleteAttributes1_NullAttributes()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttributes(nameof(DeleteAttributes1_NullAttributes), null));
    }

    [Fact]
    public async void DeleteAttributes1_EmptyAttributes()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttributes1_EmptyAttributes));
        var attributes = Array.Empty<string>();

        await client.CreateEntity(entity);
        var res = await client.DeleteAttributes(entity.Id, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.True(await Fixture.EntityExists(entity.Id));
    }

    [Fact]
    public async void DeleteAttributes1_NullAttributesItem()
    {
        var client = NewClient();
        var attributes = new string[] {null!};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttributes(nameof(DeleteAttributes1_NullAttributesItem), attributes));
    }

    [Fact]
    public async void DeleteAttributes1_EntityNotFound()
    {
        var client = NewClient();
        var attributes = new[] {nameof(UnitTestEntity.StringValue)};

        var res = await client.DeleteAttributes(nameof(DeleteAttributes1_EntityNotFound), attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(nameof(DeleteAttribute1_EntityNotFound)));
    }

    [Fact]
    public async void DeleteAttributes1_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteAttributes1_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(DeleteAttributes1_MultipleEntitiesFound), Fixture.EntityType + "1");
        var attributes = new[] {nameof(UnitTestEntity.StringValue)};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteAttributes(entity1.Id, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.Conflict, res.Code);
        var check = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check);
        Assert.True(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttributes1_AttributesNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttributes1_AttributesNotFound));
        var attributes = new[] {nameof(UnitTestEntity.StringValue) + "1"};

        await client.CreateEntity(entity);
        var res = await client.DeleteAttributes(entity.Id, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.True(await Fixture.EntityExists(entity.Id));
    }

    [Fact]
    public async void DeleteAttributes1_SomeAttributesFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttributes1_SomeAttributesFound));
        var attributes = new[] {nameof(UnitTestEntity.StringValue), nameof(UnitTestEntity.StringValue) + "1"};

        await client.CreateEntity(entity);
        var res = await client.DeleteAttributes(entity.Id, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.True(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttributes1_AllAttributesFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttributes1_AllAttributesFound));
        var attributes = new[] {nameof(UnitTestEntity.StringValue)};

        await client.CreateEntity(entity);
        var res = await client.DeleteAttributes(entity.Id, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttributes1_ProtectedAttributes()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttributes1_ProtectedAttributes));
        var attributes = new[] {"id", "type", "dateCreated", "dateModified"};

        await client.CreateEntity(entity);
        var res = await client.DeleteAttributes(entity.Id, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.True(await Fixture.EntityExists(entity.Id));
    }

    [Fact]
    public async void DeleteAttributes2_NullId()
    {
        var client = NewClient();
        var attributes = Array.Empty<string>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttributes(null, Fixture.EntityType, attributes));
    }

    [Fact]
    public async void DeleteAttributes2_EmptyId()
    {
        var client = NewClient();
        var attributes = Array.Empty<string>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttributes(string.Empty, Fixture.EntityType, attributes));
    }

    [Fact]
    public async void DeleteAttributes2_NullType()
    {
        var client = NewClient();
        var attributes = Array.Empty<string>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttributes(nameof(DeleteAttributes2_NullType), null, attributes));
    }

    [Fact]
    public async void DeleteAttributes2_EmptyType()
    {
        var client = NewClient();
        var attributes = Array.Empty<string>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttributes(nameof(DeleteAttributes2_EmptyType), string.Empty, attributes));
    }

    [Fact]
    public async void DeleteAttributes2_NullAttributes()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttributes(nameof(DeleteAttributes2_NullAttributes), Fixture.EntityType, null));
    }

    [Fact]
    public async void DeleteAttributes2_EmptyAttributes()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttributes2_EmptyAttributes));
        var attributes = Array.Empty<string>();

        await client.CreateEntity(entity);
        var res = await client.DeleteAttributes(entity.Id, entity.Type, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        Assert.True(await Fixture.EntityExists(entity.Id, entity.Type));
    }

    [Fact]
    public async void DeleteAttributes2_NullAttributesItem()
    {
        var client = NewClient();
        var attributes = new string[] {null!};

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttributes(nameof(DeleteAttributes2_NullAttributesItem), Fixture.EntityType, attributes));
    }

    [Fact]
    public async void DeleteAttributes2_EntityNotFound()
    {
        var client = NewClient();
        var attributes = new[] {nameof(UnitTestEntity.StringValue)};

        var res = await client.DeleteAttributes(nameof(DeleteAttributes2_EntityNotFound), Fixture.EntityType, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(nameof(DeleteAttributes2_EntityNotFound), Fixture.EntityType));
    }

    [Fact]
    public async void DeleteAttributes2_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteAttributes2_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(DeleteAttributes2_MultipleEntitiesFound), Fixture.EntityType + "1");
        var attributes = new[] {nameof(UnitTestEntity.StringValue)};

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteAttributes(entity1.Id, entity1.Type, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttributes2_AttributesNotFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttributes2_AttributesNotFound));
        var attributes = new[] {nameof(UnitTestEntity.StringValue) + "1"};

        await client.CreateEntity(entity);
        var res = await client.DeleteAttributes(entity.Id, entity.Type, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.True(await Fixture.EntityExists(entity.Id, entity.Type));
    }

    [Fact]
    public async void DeleteAttributes2_SomeAttributesFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttributes2_SomeAttributesFound));
        var attributes = new[] {nameof(UnitTestEntity.StringValue), nameof(UnitTestEntity.StringValue) + "1"};

        await client.CreateEntity(entity);
        var res = await client.DeleteAttributes(entity.Id, entity.Type, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.True(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttributes2_AllAttributesFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttributes2_AllAttributesFound));
        var attributes = new[] {nameof(UnitTestEntity.StringValue)};

        await client.CreateEntity(entity);
        var res = await client.DeleteAttributes(entity.Id, entity.Type, attributes);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAttributes2_ProtectedAttributes()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAttributes2_ProtectedAttributes));
        var attributes = new[] {"id", "type", "dateCreated", "dateModified"};

        await client.CreateEntity(entity);
        var res = await client.DeleteAttributes(entity.Id, entity.Type, attributes);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.True(await Fixture.EntityExists(entity.Id, entity.Type));
    }
    #endregion

    #region DeleteAllAttributes
    [Fact]
    public async void DeleteAllAttributes1_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAllAttributes(null));
    }

    [Fact]
    public async void DeleteAllAttributes1_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAllAttributes(string.Empty));
    }

    [Fact]
    public async void DeleteAllAttributes1_EntityNotFound()
    {
        var client = NewClient();

        var res = await client.DeleteAllAttributes(nameof(DeleteAllAttributes1_EntityNotFound));

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(nameof(DeleteAllAttributes1_EntityNotFound)));
    }

    [Fact]
    public async void DeleteAllAttributes1_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAllAttributes1_EntityFound));

        await client.CreateEntity(entity);
        var res = await client.DeleteAllAttributes(entity.Id);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAllAttributes1_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteAllAttributes1_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(DeleteAllAttributes1_MultipleEntitiesFound), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteAllAttributes(entity1.Id);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.Conflict, res.Code);
    }

    [Fact]
    public async void DeleteAllAttributes1_EmptyEntity()
    {
        var client = NewClient();
        var entity = NewEmptyEntity(nameof(DeleteAllAttributes1_EmptyEntity));

        await client.CreateEntity(entity);
        var res = await client.DeleteAllAttributes(entity.Id);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id);
        Assert.NotNull(check);
    }

    [Fact]
    public async void DeleteAllAttributes2_NullId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAllAttributes(null, Fixture.EntityType));
    }

    [Fact]
    public async void DeleteAllAttributes2_EmptyId()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAllAttributes(string.Empty, Fixture.EntityType));
    }

    [Fact]
    public async void DeleteAllAttributes2_NullType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAttributes(nameof(DeleteAllAttributes2_NullType), null));
    }

    [Fact]
    public async void DeleteAllAttributes2_EmptyType()
    {
        var client = NewClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAllAttributes(nameof(DeleteAllAttributes2_EmptyType), string.Empty));
    }

    [Fact]
    public async void DeleteAllAttributes2_EntityNotFound()
    {
        var client = NewClient();

        var res = await client.DeleteAllAttributes(nameof(DeleteAllAttributes2_EntityNotFound), Fixture.EntityType);

        Assert.True(res.IsBad, res.Code.ToString());
        Assert.Equal(HttpStatusCode.NotFound, res.Code);
        Assert.False(await Fixture.EntityExists(nameof(DeleteAllAttributes2_EntityNotFound), Fixture.EntityType));
    }

    [Fact]
    public async void DeleteAllAttributes2_EntityFound()
    {
        var client = NewClient();
        var entity = NewEntity(nameof(DeleteAllAttributes2_EntityFound));

        await client.CreateEntity(entity);
        var res = await client.DeleteAllAttributes(entity.Id, entity.Type);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.False(check.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAllAttributes2_MultipleEntitiesFound()
    {
        var client = NewClient();
        var entity1 = NewEntity(nameof(DeleteAllAttributes2_MultipleEntitiesFound));
        var entity2 = NewEntity(nameof(DeleteAllAttributes2_MultipleEntitiesFound), Fixture.EntityType + "1");

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var res = await client.DeleteAllAttributes(entity1.Id, entity1.Type);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check1 = await Fixture.GetEntity(entity1.Id, entity1.Type);
        Assert.NotNull(check1);
        Assert.False(check1.ContainsKey(nameof(UnitTestEntity.StringValue)));
        var check2 = await Fixture.GetEntity(entity2.Id, entity2.Type);
        Assert.NotNull(check2);
        Assert.True(check2.ContainsKey(nameof(UnitTestEntity.StringValue)));
    }

    [Fact]
    public async void DeleteAllAttributes2_EmptyEntity()
    {
        var client = NewClient();
        var entity = NewEmptyEntity(nameof(DeleteAllAttributes2_EmptyEntity));

        await client.CreateEntity(entity);
        var res = await client.DeleteAllAttributes(entity.Id, entity.Type);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.NoContent, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
    }
    #endregion

    #region StartSubscriptionServer
    //todo
    #endregion

    #region StopSubscriptionServer
    //todo
    #endregion

    #region CreateSubscription
    //todo
    #endregion

    #region GetSubscription
    //todo
    #endregion

    #region UpdateSubscription
    //todo
    #endregion

    #region DeleteSubscription
    //todo
    #endregion

    #region utils
    private FiwareClient NewClient() => new(Fixture.ContextBrokerAddress);

    private FiwareClient NewClient(FiwareSettings settings) => new(Fixture.ContextBrokerAddress, settings);

    private UnitTestEntity NewEntity() => NewEntity(null, Fixture.EntityType);

    private UnitTestEntity NewEntity(string id) => NewEntity(id, Fixture.EntityType);

    private static UnitTestEntity NewEntity(string? id, string? type) => new()
    {
        Id = id,
        Type = type
    };

    private EmptyUnitTestEntity NewEmptyEntity() => NewEmptyEntity(null, Fixture.EntityType);

    private EmptyUnitTestEntity NewEmptyEntity(string id) => NewEmptyEntity(id, Fixture.EntityType);

    private EmptyUnitTestEntity NewEmptyEntity(string? id, string? type) => new()
    {
        Id = id,
        Type = type
    };
    #endregion

    #region test classes
    private class UnitTestEntity : EntityBase
    {
        public string? StringValue { get; set; } = "TestValue";

        [FiwareMetadata(nameof(StringValue))]
        public MetadataCollection StringValueMetadata { get; set; } = new();
    }

    private class EmptyUnitTestEntity : EntityBase
    { }

    private class InvalidEntity
    {
        public string? InvalidId { get; set; }

        public string? InvalidType { get; set; }
    }
    #endregion
}
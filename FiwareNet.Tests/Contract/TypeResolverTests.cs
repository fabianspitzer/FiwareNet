using System.Text.RegularExpressions;
using Xunit;

namespace FiwareNet.Tests;

public class TypeResolverTests : FiwareTestFixtureClass
{
    public TypeResolverTests(FiwareTestFixture fixture) : base(fixture) { }

    #region interface tests
    [Fact]
    public async void BaseTypeResolver_Interface()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress, new FiwareSettings
        {
            TypeResolvers = new List<TypeResolver> {new CustomTypeResolver(Fixture.EntityType)}
        });
        var entity1 = new TypeResolverTest1
        {
            Id = nameof(BaseTypeResolver_Interface) + 1,
            Type = Fixture.EntityType + nameof(TypeResolverTest1),
            StringValue = "TestString"
        };
        var entity2 = new TypeResolverTest2
        {
            Id = nameof(BaseTypeResolver_Interface) + 2,
            Type = Fixture.EntityType + nameof(TypeResolverTest2),
            IntValue = 123
        };
        var entity3 = new TypeResolverTest3
        {
            Id = nameof(BaseTypeResolver_Interface) + 3,
            Type = Fixture.EntityType + nameof(TypeResolverTest3),
            BoolArray = new[] {true, true, false, true, false, false}
        };

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await client.CreateEntity(entity3);
        var (res, entities) = await client.GetEntities<ITypeResolverClass>(new Regex(nameof(BaseTypeResolver_Interface)));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(3, entities.Count);
        Assert.IsType<TypeResolverTest1>(entities[0]);
        Assert.Equal(entity1.StringValue, ((TypeResolverTest1) entities[0]).StringValue);
        Assert.IsType<TypeResolverTest2>(entities[1]);
        Assert.Equal(entity2.IntValue, ((TypeResolverTest2) entities[1]).IntValue);
        Assert.IsType<TypeResolverTest3>(entities[2]);
        Assert.Equal(entity3.BoolArray, ((TypeResolverTest3) entities[2]).BoolArray);
    }

    [Fact]
    public async void BaseTypeResolver_Class()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress, new FiwareSettings
        {
            TypeResolvers = new List<TypeResolver> {new CustomTypeResolver3(Fixture.EntityType)}
        });
        var entity1 = new TypeResolverTest4
        {
            Id = nameof(BaseTypeResolver_Class) + 1,
            Type = Fixture.EntityType + nameof(TypeResolverTest4),
            DoubleValue = 123.45
        };
        var entity2 = new TypeResolverTest5
        {
            Id = nameof(BaseTypeResolver_Class) + 2,
            Type = Fixture.EntityType + nameof(TypeResolverTest5),
            DecimalValue = 123456789
        };

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<TypeResolverClass>(new Regex(nameof(BaseTypeResolver_Class)));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.IsType<TypeResolverTest4>(entities[0]);
        Assert.Equal(entity1.DoubleValue, ((TypeResolverTest4) entities[0]).DoubleValue);
        Assert.IsType<TypeResolverTest5>(entities[1]);
        Assert.Equal(entity2.DecimalValue, ((TypeResolverTest5) entities[1]).DecimalValue);
    }

    [Fact]
    public async void GenericTypeResolver_Interface()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress, new FiwareSettings
        {
            TypeResolvers = new List<TypeResolver> {new CustomTypeResolver2(Fixture.EntityType)}
        });
        var entity1 = new TypeResolverTest1
        {
            Id = nameof(GenericTypeResolver_Interface) + 1,
            Type = Fixture.EntityType + nameof(TypeResolverTest1),
            StringValue = "TestString"
        };
        var entity2 = new TypeResolverTest2
        {
            Id = nameof(GenericTypeResolver_Interface) + 2,
            Type = Fixture.EntityType + nameof(TypeResolverTest2),
            IntValue = 123
        };
        var entity3 = new TypeResolverTest3
        {
            Id = nameof(GenericTypeResolver_Interface) + 3,
            Type = Fixture.EntityType + nameof(TypeResolverTest3),
            BoolArray = new[] {true, true, false, true, false, false}
        };

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        await client.CreateEntity(entity3);
        var (res, entities) = await client.GetEntities<ITypeResolverClass>(new Regex(nameof(GenericTypeResolver_Interface)));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(3, entities.Count);
        Assert.IsType<TypeResolverTest1>(entities[0]);
        Assert.Equal(entity1.StringValue, ((TypeResolverTest1) entities[0]).StringValue);
        Assert.IsType<TypeResolverTest2>(entities[1]);
        Assert.Equal(entity2.IntValue, ((TypeResolverTest2) entities[1]).IntValue);
        Assert.IsType<TypeResolverTest3>(entities[2]);
        Assert.Equal(entity3.BoolArray, ((TypeResolverTest3) entities[2]).BoolArray);
    }

    [Fact]
    public async void GenericTypeResolver_Class()
    {
        var client = new FiwareClient(Fixture.ContextBrokerAddress, new FiwareSettings
        {
            TypeResolvers = new List<TypeResolver> {new CustomTypeResolver4(Fixture.EntityType)}
        });
        var entity1 = new TypeResolverTest4
        {
            Id = nameof(GenericTypeResolver_Class) + 1,
            Type = Fixture.EntityType + nameof(TypeResolverTest4),
            DoubleValue = 123.45
        };
        var entity2 = new TypeResolverTest5
        {
            Id = nameof(GenericTypeResolver_Class) + 2,
            Type = Fixture.EntityType + nameof(TypeResolverTest5),
            DecimalValue = 123456789
        };

        await client.CreateEntity(entity1);
        await client.CreateEntity(entity2);
        var (res, entities) = await client.GetEntities<TypeResolverClass>(new Regex(nameof(GenericTypeResolver_Class)));

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.NotNull(entities);
        Assert.Equal(2, entities.Count);
        Assert.IsType<TypeResolverTest4>(entities[0]);
        Assert.Equal(entity1.DoubleValue, ((TypeResolverTest4) entities[0]).DoubleValue);
        Assert.IsType<TypeResolverTest5>(entities[1]);
        Assert.Equal(entity2.DecimalValue, ((TypeResolverTest5) entities[1]).DecimalValue);
    }
    #endregion

    #region test classes
    private class CustomTypeResolver : TypeResolver
    {
        private readonly int _offset;

        public CustomTypeResolver(string entityType) => _offset = entityType.Length;

        public override bool CanResolve(Type type) => typeof(ITypeResolverClass).IsAssignableFrom(type);

        public override Type Resolve(string entityId, string entityType) => entityType[_offset..] switch
        {
            nameof(TypeResolverTest1) => typeof(TypeResolverTest1),
            nameof(TypeResolverTest2) => typeof(TypeResolverTest2),
            nameof(TypeResolverTest3) => typeof(TypeResolverTest3),
            _ => throw new NotImplementedException()
        };
    }

    private class CustomTypeResolver2 : TypeResolver<ITypeResolverClass>
    {
        private readonly int _offset;

        public CustomTypeResolver2(string entityType) => _offset = entityType.Length;

        public override Type Resolve(string entityId, string entityType) => entityType[_offset..] switch
        {
            nameof(TypeResolverTest1) => typeof(TypeResolverTest1),
            nameof(TypeResolverTest2) => typeof(TypeResolverTest2),
            nameof(TypeResolverTest3) => typeof(TypeResolverTest3),
            _ => throw new NotImplementedException()
        };
    }

    private class CustomTypeResolver3 : TypeResolver
    {
        private readonly int _offset;

        public CustomTypeResolver3(string entityType) => _offset = entityType.Length;

        public override bool CanResolve(Type type) => typeof(TypeResolverClass).IsAssignableFrom(type);

        public override Type Resolve(string entityId, string entityType) => entityType[_offset..] switch
        {
            nameof(TypeResolverTest4) => typeof(TypeResolverTest4),
            nameof(TypeResolverTest5) => typeof(TypeResolverTest5),
            _ => throw new NotImplementedException()
        };
    }

    private class CustomTypeResolver4 : TypeResolver<TypeResolverClass>
    {
        private readonly int _offset;

        public CustomTypeResolver4(string entityType) => _offset = entityType.Length;

        public override Type Resolve(string entityId, string entityType) => entityType[_offset..] switch
        {
            nameof(TypeResolverTest4) => typeof(TypeResolverTest4),
            nameof(TypeResolverTest5) => typeof(TypeResolverTest5),
            _ => throw new NotImplementedException()
        };
    }

    private interface ITypeResolverClass
    { }

    private class TypeResolverTest1 : EntityBase, ITypeResolverClass
    {
        public string StringValue { get; init; } = null!;
    }

    private class TypeResolverTest2 : EntityBase, ITypeResolverClass
    {
        public int IntValue { get; init; }
    }

    private class TypeResolverTest3 : EntityBase, ITypeResolverClass
    {
        public bool[] BoolArray { get; init; } = null!;
    }

    private abstract class TypeResolverClass : EntityBase
    { }

    private class TypeResolverTest4 : TypeResolverClass
    {
        public double DoubleValue { get; init; }
    }

    private class TypeResolverTest5 : TypeResolverClass
    {
        public decimal DecimalValue { get; init; }
    }
    #endregion
}
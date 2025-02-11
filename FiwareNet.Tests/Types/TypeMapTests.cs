using System.Collections;
using System.Net;
using FiwareNet.Ngsi;
using Xunit;

namespace FiwareNet.Tests;

public class TypeMapTests : FiwareTestFixtureClass
{
    public TypeMapTests(FiwareTestFixture fixture) : base(fixture) { }

    #region constructors
    [Fact]
    public void Ctor_Empty()
    {
        var map = new TypeMap();

        Assert.Empty(map);
    }

    [Fact]
    public void Ctor_Copy_NullMap()
    {
        Assert.Throws<ArgumentNullException>(() => new TypeMap(null));
    }

    [Fact]
    public void Ctor_Copy()
    {
        var map = new TypeMap
        {
            {typeof(int), FiwareTypes.Number},
            {typeof(string), FiwareTypes.Text}
        };

        var copy = new TypeMap(map);
        map.Add(typeof(bool), FiwareTypes.Boolean);
        map.Remove(typeof(string));

        Assert.NotEmpty(copy);
        Assert.True(copy.ContainsType(typeof(int)));
        Assert.True(copy.ContainsType(typeof(string)));
        Assert.False(copy.ContainsType(typeof(bool)));
    }
    #endregion

    #region Count
    [Fact]
    public void Count()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.Equal(3, map.Count);
    }
    #endregion

    #region ExactMatch
    [Fact]
    public void ExactMatch_BaseType()
    {
        var map = new TypeMap {{typeof(BaseType), nameof(BaseType)}};
        var exactMap = new TypeMap {{typeof(BaseType), nameof(BaseType)}};
        exactMap.ExactMatch = true;

        Assert.NotNull(map.FindBestMatch(typeof(DerivedType)));
        Assert.Null(exactMap.FindBestMatch(typeof(DerivedType)));
    }

    [Fact]
    public void ExactMatch_Interface()
    {
        var map = new TypeMap {{typeof(IInterfaceType), nameof(IInterfaceType)}};
        var exactMap = new TypeMap {{typeof(IInterfaceType), nameof(IInterfaceType)}};
        exactMap.ExactMatch = true;

        Assert.NotNull(map.FindBestMatch(typeof(DerivedType)));
        Assert.Null(exactMap.FindBestMatch(typeof(DerivedType)));
    }

    [Fact]
    public void ExactMatch_Object()
    {
        var map = new TypeMap {{typeof(object), nameof(BaseType)}};
        var exactMap = new TypeMap {{typeof(object), nameof(BaseType)}};
        exactMap.ExactMatch = true;

        Assert.NotNull(map.FindBestMatch(typeof(DerivedType)));
        Assert.Null(exactMap.FindBestMatch(typeof(DerivedType)));
    }
    #endregion

    #region Types
    [Fact]
    public void Types()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.Equal(3, map.Types.Count);
    }

    [Fact]
    public void Types_Order()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        map.Remove(typeof(string));
        map.Add(typeof(string), FiwareTypes.Text);

        Assert.Equal(typeof(int), map.Types.ElementAt(0));
        Assert.Equal(typeof(string), map.Types.ElementAt(2));
    }
    #endregion

    #region TypeNames
    [Fact]
    public void TypeNames()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.Equal(3, map.TypeNames.Count);
    }
    #endregion

    #region Indexer
    [Fact]
    public void Indexer_Get_NullType()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.Throws<ArgumentNullException>(() => map[null]);
    }

    [Fact]
    public void Indexer_Get_NotFound()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.Throws<KeyNotFoundException>(() => map[typeof(double)]);
        Assert.False(map.ContainsType(typeof(double)));
    }

    [Fact]
    public void Indexer_Get_Found()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        var res = map[typeof(string)];

        Assert.Equal(FiwareTypes.Text, res);
    }

    [Fact]
    public void Indexer_Set_NullType()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.Throws<ArgumentNullException>(() => map[null] = FiwareTypes.Number);
    }

    [Fact]
    public void Indexer_Set_NullTypeName()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.Throws<ArgumentNullException>(() => map[typeof(double)] = null);
    }

    [Fact]
    public void Indexer_Set_EmptyTypeName()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.Throws<ArgumentNullException>(() => map[typeof(double)] = string.Empty);
    }

    [Fact]
    public void Indexer_Set_NotFound()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        map[typeof(double)] = FiwareTypes.Number;

        Assert.True(map.ContainsType(typeof(double)));
        Assert.Equal(FiwareTypes.Number, map[typeof(double)]);
    }

    [Fact]
    public void Indexer_Set_Found()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        map[typeof(int)] = FiwareTypes.Integer;

        Assert.True(map.ContainsType(typeof(int)));
        Assert.Equal(FiwareTypes.Integer, map[typeof(int)]);
    }
    #endregion

    #region Add
    [Fact]
    public void Add_NullType()
    {
        var map = new TypeMap();

        Assert.Throws<ArgumentNullException>(() => map.Add(null, FiwareTypes.Number));
    }

    [Fact]
    public void Add_NullTypeName()
    {
        var map = new TypeMap();

        Assert.Throws<ArgumentNullException>(() => map.Add(typeof(int), null));
    }

    [Fact]
    public void Add_EmptyTypeName()
    {
        var map = new TypeMap();

        Assert.Throws<ArgumentNullException>(() => map.Add(typeof(int), string.Empty));
    }

    [Fact]
    public void Add_AlreadyExists()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number}
        };

        Assert.Throws<ArgumentException>(() => map.Add(typeof(int), FiwareTypes.Number));
    }

    [Fact]
    public void Add()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number}
        };

        map.Add(typeof(bool), FiwareTypes.Boolean);

        Assert.Equal(3, map.Count);
        Assert.True(map.ContainsType(typeof(bool)));
    }
    #endregion

    #region InsertBefore
    [Fact]
    public void InsertBefore_NullType()
    {
        var map = new TypeMap();

        Assert.Throws<ArgumentNullException>(() => map.InsertBefore(typeof(string), null, FiwareTypes.Number));
    }

    [Fact]
    public void InsertBefore_NullTypeName()
    {
        var map = new TypeMap();

        Assert.Throws<ArgumentNullException>(() => map.InsertBefore(typeof(string), typeof(int), null));
    }

    [Fact]
    public void InsertBefore_EmptyTypeName()
    {
        var map = new TypeMap();

        Assert.Throws<ArgumentNullException>(() => map.InsertBefore(typeof(string), typeof(int), string.Empty));
    }

    [Fact]
    public void InsertBefore_AlreadyExists()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number}
        };

        Assert.Throws<ArgumentException>(() => map.InsertBefore(typeof(string), typeof(int), FiwareTypes.Number));
    }

    [Fact]
    public void InsertBefore_NotFound()
    {
        var map = new TypeMap();

        Assert.Throws<ArgumentOutOfRangeException>(() => map.InsertBefore(typeof(string), typeof(int), FiwareTypes.Number));
    }

    [Fact]
    public void InsertBefore_AtStart()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number}
        };

        map.InsertBefore(typeof(string), typeof(bool), FiwareTypes.Boolean);

        Assert.True(map.ContainsType(typeof(bool)));
        Assert.Equal(typeof(bool), map.Types.ElementAt(0));
    }

    [Fact]
    public void InsertBefore_AtEnd()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number}
        };

        map.InsertBefore(typeof(int), typeof(bool), FiwareTypes.Boolean);

        Assert.True(map.ContainsType(typeof(bool)));
        Assert.Equal(typeof(bool), map.Types.ElementAt(1));
    }
    #endregion

    #region InsertAfter
    [Fact]
    public void InsertAfter_NullType()
    {
        var map = new TypeMap();

        Assert.Throws<ArgumentNullException>(() => map.InsertAfter(typeof(string), null, FiwareTypes.Number));
    }

    [Fact]
    public void InsertAfter_NullTypeName()
    {
        var map = new TypeMap();

        Assert.Throws<ArgumentNullException>(() => map.InsertAfter(typeof(string), typeof(int), null));
    }

    [Fact]
    public void InsertAfter_EmptyTypeName()
    {
        var map = new TypeMap();

        Assert.Throws<ArgumentNullException>(() => map.InsertAfter(typeof(string), typeof(int), string.Empty));
    }

    [Fact]
    public void InsertAfter_AlreadyExists()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number}
        };

        Assert.Throws<ArgumentException>(() => map.InsertAfter(typeof(string), typeof(int), FiwareTypes.Number));
    }

    [Fact]
    public void InsertAfter_NotFound()
    {
        var map = new TypeMap();

        Assert.Throws<ArgumentOutOfRangeException>(() => map.InsertAfter(typeof(string), typeof(int), FiwareTypes.Number));
    }

    [Fact]
    public void InsertAfter_AtStart()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number}
        };

        map.InsertAfter(typeof(string), typeof(bool), FiwareTypes.Boolean);

        Assert.True(map.ContainsType(typeof(bool)));
        Assert.Equal(typeof(bool), map.Types.ElementAt(1));
    }

    [Fact]
    public void InsertAfter_AtEnd()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number}
        };

        map.InsertAfter(typeof(int), typeof(bool), FiwareTypes.Boolean);

        Assert.True(map.ContainsType(typeof(bool)));
        Assert.Equal(typeof(bool), map.Types.ElementAt(2));
    }
    #endregion

    #region Remove
    [Fact]
    public void Remove_NullType()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.Throws<ArgumentNullException>(() => map.Remove(null));
    }

    [Fact]
    public void Remove_NotFound()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.False(map.Remove(typeof(object)));
        Assert.Equal(3, map.Count);
    }

    [Fact]
    public void Remove_Found()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.True(map.Remove(typeof(int)));
        Assert.False(map.ContainsType(typeof(int)));
        Assert.Equal(2, map.Count);
    }
    #endregion

    #region Clear
    [Fact]
    public void Clear()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        map.Clear();

        Assert.Empty(map);
        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void Clear_EmptyMap()
    {
        var map = new TypeMap();

        map.Clear();

        Assert.Empty(map);
        Assert.Equal(0, map.Count);
    }
    #endregion

    #region ContainsType
    [Fact]
    public void ContainsType_Exact()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.True(map.ContainsType(typeof(string)));
    }

    [Fact]
    public void ContainsType_NotExact()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.False(map.ContainsType(typeof(object)));
    }

    [Fact]
    public void ContainsType_NotFound()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        Assert.False(map.ContainsType(typeof(double)));
    }
    #endregion

    #region TryGetTypeName
    [Fact]
    public void TryGetTypeName_Exact()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        var res = map.TryGetTypeName(typeof(string), out var typeName);

        Assert.True(res);
        Assert.Equal(FiwareTypes.Text, typeName);
    }

    [Fact]
    public void TryGetTypeName_NotExact()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        var res = map.TryGetTypeName(typeof(object), out var typeName);

        Assert.False(res);
        Assert.Null(typeName);
    }

    [Fact]
    public void TryGetTypeName_NotFound()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        var res = map.TryGetTypeName(typeof(double), out var typeName);

        Assert.False(res);
        Assert.Null(typeName);
    }
    #endregion

    #region FindBestMatch
    [Fact]
    public void FindBestMatch_Null()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number}
        };

        var res = map.FindBestMatch(null);

        Assert.Null(res);
    }

    [Fact]
    public void FindBestMatch_Nullable()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number}
        };

        var res1 = map.FindBestMatch(typeof(int));
        var res2 = map.FindBestMatch(typeof(int?));

        Assert.Equal(FiwareTypes.Number, res1);
        Assert.Equal(FiwareTypes.Number, res2);
    }

    [Fact]
    public void FindBestMatch_ExactMatch()
    {
        var map = new TypeMap
        {
            {typeof(BaseType), nameof(BaseType)},
            {typeof(IInterfaceType), nameof(IInterfaceType)},
            {typeof(DerivedType), nameof(DerivedType)}
        };

        var res = map.FindBestMatch(typeof(DerivedType));

        Assert.Equal(nameof(DerivedType), res);
    }

    [Fact]
    public void FindBestMatch_BaseType()
    {
        var map = new TypeMap
        {
            {typeof(BaseType), nameof(BaseType)},
            {typeof(object), FiwareTypes.StructuredValue}
        };

        var res = map.FindBestMatch(typeof(DerivedType));

        Assert.Equal(nameof(BaseType), res);
    }

    [Fact]
    public void FindBestMatch_InterfaceType()
    {
        var map = new TypeMap
        {
            {typeof(IInterfaceType), nameof(IInterfaceType)},
            {typeof(object), FiwareTypes.StructuredValue}
        };

        var res = map.FindBestMatch(typeof(DerivedType));

        Assert.Equal(nameof(IInterfaceType), res);
    }

    [Fact]
    public void FindBestMatch_NoMatch()
    {
        var map = new TypeMap
        {
            {typeof(int), FiwareTypes.Number},
            {typeof(string), FiwareTypes.Text}
        };

        var res = map.FindBestMatch(typeof(bool));

        Assert.Null(res);
    }
    #endregion

    #region GetJsonMap
    [Fact]
    public void GetJsonMap()
    {
        var map = TypeMap.GetJsonMap();

        Assert.Equal(FiwareTypes.Boolean, map.FindBestMatch(typeof(bool)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(sbyte)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(byte)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(short)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(ushort)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(int)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(uint)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(long)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(ulong)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(decimal)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(float)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(double)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(Enum)));
        Assert.Equal(FiwareTypes.Text, map.FindBestMatch(typeof(string)));
        Assert.Equal(FiwareTypes.Text, map.FindBestMatch(typeof(Uri)));
        Assert.Equal(FiwareTypes.DateTime, map.FindBestMatch(typeof(DateTime)));
        Assert.Equal(FiwareTypes.StructuredValue, map.FindBestMatch(typeof(object)));
    }
    #endregion

    #region GetExpandedMap
    [Fact]
    public void GetExpandedMap()
    {
        var map = TypeMap.GetExpandedMap();

        Assert.Equal(FiwareTypes.Boolean, map.FindBestMatch(typeof(bool)));
        Assert.Equal(FiwareTypes.SByte, map.FindBestMatch(typeof(sbyte)));
        Assert.Equal(FiwareTypes.Byte, map.FindBestMatch(typeof(byte)));
        Assert.Equal(FiwareTypes.Int16, map.FindBestMatch(typeof(short)));
        Assert.Equal(FiwareTypes.UInt16, map.FindBestMatch(typeof(ushort)));
        Assert.Equal(FiwareTypes.Int32, map.FindBestMatch(typeof(int)));
        Assert.Equal(FiwareTypes.UInt32, map.FindBestMatch(typeof(uint)));
        Assert.Equal(FiwareTypes.Int64, map.FindBestMatch(typeof(long)));
        Assert.Equal(FiwareTypes.UInt64, map.FindBestMatch(typeof(ulong)));
        Assert.Equal(FiwareTypes.Decimal, map.FindBestMatch(typeof(decimal)));
        Assert.Equal(FiwareTypes.Single, map.FindBestMatch(typeof(float)));
        Assert.Equal(FiwareTypes.Double, map.FindBestMatch(typeof(double)));
        Assert.Equal(FiwareTypes.Number, map.FindBestMatch(typeof(Enum)));
        Assert.Equal(FiwareTypes.Text, map.FindBestMatch(typeof(string)));
        Assert.Equal(FiwareTypes.Text, map.FindBestMatch(typeof(Uri)));
        Assert.Equal(FiwareTypes.DateTime, map.FindBestMatch(typeof(DateTime)));
        Assert.Equal(FiwareTypes.TimeSpan, map.FindBestMatch(typeof(TimeSpan)));
        Assert.Equal(FiwareTypes.Guid, map.FindBestMatch(typeof(Guid)));
        Assert.Equal(FiwareTypes.GeoPoint, map.FindBestMatch(typeof(GeoPoint)));
        Assert.Equal(FiwareTypes.GeoLine, map.FindBestMatch(typeof(GeoLine)));
        Assert.Equal(FiwareTypes.GeoPolygon, map.FindBestMatch(typeof(GeoPolygon)));
        Assert.Equal(FiwareTypes.GeoBox, map.FindBestMatch(typeof(GeoBox)));
        Assert.Equal(FiwareTypes.Array, map.FindBestMatch(typeof(IEnumerable)));
        Assert.Equal(FiwareTypes.StructuredValue, map.FindBestMatch(typeof(IDictionary)));
        Assert.Equal(FiwareTypes.StructuredValue, map.FindBestMatch(typeof(object)));
    }
    #endregion

    #region Clone
    [Fact]
    public void Clone()
    {
        var map = new TypeMap
        {
            {typeof(int), FiwareTypes.Number},
            {typeof(string), FiwareTypes.Text}
        };

        var clone = map.Clone();
        map.Add(typeof(bool), FiwareTypes.Boolean);
        map.Remove(typeof(string));

        Assert.NotEmpty(clone);
        Assert.True(clone.ContainsType(typeof(int)));
        Assert.True(clone.ContainsType(typeof(string)));
        Assert.False(clone.ContainsType(typeof(bool)));
    }
    #endregion

    #region GetEnumerator
    [Fact]
    public void GetEnumerator()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number},
            {typeof(bool), FiwareTypes.Boolean}
        };

        map.Remove(typeof(string));
        map.Add(typeof(string), FiwareTypes.Text);

        var index = 0;
        foreach (var (key, _) in map)
        {
            if (key == typeof(string)) break;
            index++;
        }
        Assert.Equal(2, index);
    }
    #endregion

    #region FiwareClient tests
    [Fact]
    public async void FiwareClient_MissingType()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text}
        };
        var client = new FiwareClient(Fixture.ContextBrokerAddress, new FiwareSettings {TypeMap = map});
        var entity = new UnitTestEntity
        {
            Id = nameof(FiwareClient_MissingType),
            Type = Fixture.EntityType
        };

        await Assert.ThrowsAsync<FiwareTypeException>(() => client.CreateEntity(entity));
    }

    [Fact]
    public async void FiwareClient_AttributeOverride()
    {
        var map = new TypeMap
        {
            {typeof(string), FiwareTypes.Text},
            {typeof(int), FiwareTypes.Number}
        };
        var client = new FiwareClient(Fixture.ContextBrokerAddress, new FiwareSettings {TypeMap = map});
        var entity = new UnitTestEntity2
        {
            Id = nameof(FiwareClient_AttributeOverride),
            Type = Fixture.EntityType
        };

        var res = await client.CreateEntity(entity);

        Assert.True(res.IsGood, res.ErrorDescription);
        Assert.Equal(HttpStatusCode.Created, res.Code);
        var check = await Fixture.GetEntity(entity.Id, entity.Type);
        Assert.NotNull(check);
        Assert.Equal(FiwareTypes.Text, check[nameof(UnitTestEntity2.StringValue)]?["type"]?.ToString());
        Assert.Equal(FiwareTypes.Int32, check[nameof(UnitTestEntity2.IntValue)]?["type"]?.ToString());
    }
    #endregion

    #region test classes
    public class BaseType { }

    public interface IInterfaceType {}

    public class DerivedType : BaseType, IInterfaceType { }

    public class UnitTestEntity : EntityBase
    {
        public string StringValue { get; set; } = "TestValue";

        public int IntValue { get; set; } = 123;
    }

    public class UnitTestEntity2 : EntityBase
    {
        public string StringValue { get; set; } = "TestValue";

        [FiwareType(FiwareTypes.Int32)]
        public int IntValue { get; set; } = 123;
    }
    #endregion
}
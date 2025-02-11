using Xunit;

namespace FiwareNet.Tests;

[CollectionDefinition("FiwareClientTests")]
public class FiwareTestFixtureCollection : ICollectionFixture<FiwareTestFixture>
{ }
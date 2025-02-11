using Xunit;

namespace FiwareNet.Tests;

[Collection("FiwareClientTests")]
public abstract class FiwareTestFixtureClass
{
    protected readonly FiwareTestFixture Fixture;

    protected FiwareTestFixtureClass(FiwareTestFixture fixture) => Fixture = fixture;
}
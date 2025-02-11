using FiwareNet.Encoders;

namespace FiwareNet.Tests;

public class UrlStringEncoderTests : HexEscapeEncoderTests
{
    public UrlStringEncoderTests() : base(new UrlStringEncoder(), new UrlStringEncoder(true))
    { }
}
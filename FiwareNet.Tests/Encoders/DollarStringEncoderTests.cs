using FiwareNet.Encoders;

namespace FiwareNet.Tests;

public class DollarStringEncoderTests : HexEscapeEncoderTests
{
    public DollarStringEncoderTests() : base(new DollarStringEncoder(), new DollarStringEncoder(true))
    { }
}
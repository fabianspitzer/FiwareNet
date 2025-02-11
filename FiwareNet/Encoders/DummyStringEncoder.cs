namespace FiwareNet.Encoders;

internal class DummyStringEncoder : IStringEncoder
{
    /// <inheritdoc/>
    public string EncodeField(string str) => str;

    /// <inheritdoc/>
    public string DecodeField(string str) => str;

    /// <inheritdoc/>
    public string EncodeValue(string str) => str;

    /// <inheritdoc/>
    public string DecodeValue(string str) => str;
}
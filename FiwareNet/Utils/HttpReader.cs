using System;
using System.Collections.Generic;
using System.Text;

namespace FiwareNet.Utils;

internal class HttpReader
{
    #region private members
    private const string ContentLengthHeader = "Content-Length";

    private string _headerBuffer = string.Empty;
    private int _contentLength;
    private int _currentBodyLength;
    private readonly StringBuilder _body = new();
    #endregion

    #region public properties
    public bool HeaderCompleted { get; private set; }

    public bool BodyCompleted { get; private set; }

    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

    public string Body => _body.ToString();
    #endregion

    #region public methods
    public void Read(byte[] data) => Read(data, 0, data.Length);

    public void Read(byte[] data, int index) => Read(data, index, data.Length - index);

    public void Read(byte[] data, int index, int count)
    {
        if (count <= 0 || BodyCompleted) return;

        var str = Encoding.UTF8.GetString(data, index, count);

        if (HeaderCompleted) AppendBody(str, count);
        else
        {
            _headerBuffer += str;

            var lineStart = 0;
            while (lineStart < _headerBuffer.Length)
            {
                var lineEnd = _headerBuffer.IndexOf('\n', lineStart);
                if (lineEnd < 0) //no end of line found -> incomplete header
                {
                    _headerBuffer = str.Substring(lineStart);
                    return;
                }

                var splitIndex = _headerBuffer.IndexOf(':', lineStart, lineEnd - lineStart);
                if (splitIndex == -1) //first line of HTTP or newline after header
                {
                    if (lineStart > 0 || Headers.Count > 0) //newline after header
                    {
                        HeaderCompleted = true;
                        AppendBody(_headerBuffer.Substring(lineEnd + 1), count - lineEnd - 1);
                        break;
                    }
                }
                else //header content
                {
                    var headerName = _headerBuffer.Substring(lineStart, splitIndex - lineStart).Trim();
                    var headerValue = _headerBuffer.Substring(splitIndex + 1, lineEnd - splitIndex - 1).Trim();
                    Headers.Add(headerValue, headerName);

                    //check if content length was found
                    if (headerName.Equals(ContentLengthHeader, StringComparison.OrdinalIgnoreCase)) _contentLength = int.Parse(headerValue);
                }

                lineStart = lineEnd + 1;
            }

            _headerBuffer = string.Empty;
        }
    }

    public void Clear()
    {
        _headerBuffer = string.Empty;
        _contentLength = 0;
        _currentBodyLength = 0;
        _body.Clear();

        HeaderCompleted = false;
        BodyCompleted = false;
        Headers.Clear();
    }
    #endregion

    #region private methods
    private void AppendBody(string body, int bodyLength)
    {
        _currentBodyLength += bodyLength;
        _body.Append(body);
        if (_currentBodyLength == _contentLength) BodyCompleted = true;
    }
    #endregion
}
using System;
using System.Text;
using RestSharp;

namespace FiwareNet;

/// <summary>
/// A class proving an HTTP Basic access authentication header for HTTP requests.
/// </summary>
/// <remarks>
/// Creates a new instance of the <see cref="BasicAuthenticationHandler"/> class.
/// </remarks>
/// <param name="username">The username to use.</param>
/// <param name="password">The password to use.</param>
public class BasicAuthenticationHandler(string username, string password) : IRequestHandler
{
    #region private members
    private readonly string _authString = "Basic " + Base64Encode($"{username}:{password}");
    #endregion

    #region private methods
    private static string Base64Encode(string input)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(plainTextBytes);
    }
    #endregion

    #region IRequestHandler interface
    /// <inheritdoc/>
    public void OnRequest(FiwareClient client, RestRequest request) => request.AddHeader("authorization", _authString);
    #endregion
}
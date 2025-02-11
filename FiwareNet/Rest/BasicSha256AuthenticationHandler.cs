using System;
using System.Security.Cryptography;
using System.Text;

namespace FiwareNet;

/// <summary>
/// A class proving an HTTP Basic access authentication header for HTTP requests.
/// The password is hashed using SHA256.
/// </summary>
/// <remarks>
/// Creates a new instance of the <see cref="BasicSha256AuthenticationHandler"/> class.
/// </remarks>
/// <param name="username">The username to use.</param>
/// <param name="password">The password to use. The password is hashed using SHA256.</param>
public class BasicSha256AuthenticationHandler(string username, string password) : BasicAuthenticationHandler(username, ComputeSha256(password))
{
    #region private methods
    private static string ComputeSha256(string input)
    {
        using var sha256 = new SHA256Managed();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }
    #endregion
}
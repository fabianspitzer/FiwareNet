using System.Net;
using Newtonsoft.Json;

namespace FiwareNet;

/// <summary>
/// A class containing information about a FIWARE REST response.
/// </summary>
public class ActionResponse
{
    #region public properties
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public HttpStatusCode Code { get; set; }

    /// <summary>
    /// Gets a value indicating whether the response is good (HTTP Status 200 - 299).
    /// </summary>
    [JsonIgnore]
    public bool IsGood => (int) Code >= 200 && (int) Code <= 299;

    /// <summary>
    /// Gets a value indicating whether the response is bad (HTTP Status other than 200 - 299).
    /// </summary>
    [JsonIgnore]
    public bool IsBad => !IsGood;

    /// <summary>
    /// Gets or sets the error type.
    /// </summary>
    [JsonProperty("error")]
    public string Error { get; set; }

    /// <summary>
    /// Gets or sets the error description.
    /// </summary>
    [JsonProperty("description")]
    public string ErrorDescription { get; set; }
    #endregion

    #region public methods
    /// <inheritdoc/>
    public override string ToString()
    {
        if (Error is null) return Code.ToString();
        return ErrorDescription is null ? Error : $"{Error}: {ErrorDescription}";
    }
    #endregion
}
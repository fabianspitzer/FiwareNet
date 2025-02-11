using RestSharp;

namespace FiwareNet;

/// <summary>
/// An interface for REST request handlers.
/// </summary>
public interface IRequestHandler
{
    /// <summary>
    /// Called when a new REST request is created and allows the request to be manipulated (e.g. adding HTTP headers).
    /// </summary>
    /// <param name="client">The <see cref="FiwareClient"/> instance processing the REST request.</param>
    /// <param name="request">The REST request data.</param>
    public void OnRequest(FiwareClient client, RestRequest request);
}
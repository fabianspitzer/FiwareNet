using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FiwareNet.Tests;

public sealed class FiwareTestFixture : IDisposable
{
    #region constructor
    public FiwareTestFixture()
    {
        ContextBrokerAddress = "http://localhost:1026/v2/";
        EntityType = "UnitTestEntity";

        Cleanup(EntityType).Wait();
    }
    #endregion

    #region public properties
    public string ContextBrokerAddress { get; }

    public string EntityType { get; }
    #endregion

    #region public methods
    public async Task<bool> Ping()
    {
        using var client = new HttpClient();
        var res = await client.GetAsync(ContextBrokerAddress);
        return res.IsSuccessStatusCode;
    }

    public async Task<HttpStatusCode> Post(string path, string content)
    {
        using var client = new HttpClient();
        var json = new StringContent(content, Encoding.UTF8, "application/json");
        var res = await client.PostAsync(ContextBrokerAddress + path.TrimStart('/'), json);
        return res.StatusCode;
    }

    public async Task<HttpStatusCode> Put(string path, string content)
    {
        using var client = new HttpClient();
        var json = new StringContent(content, Encoding.UTF8, "application/json");
        var res = await client.PutAsync(ContextBrokerAddress + path.TrimStart('/'), json);
        return res.StatusCode;
    }

    public async Task<(HttpStatusCode, string)> Get(string path)
    {
        using var client = new HttpClient();
        var res = await client.GetAsync(ContextBrokerAddress + path.TrimStart('/'));
        var content = await res.Content.ReadAsStringAsync();
        return (res.StatusCode, content);
    }

    public async Task<HttpStatusCode> Delete(string path)
    {
        using var client = new HttpClient();
        var res = await client.DeleteAsync(ContextBrokerAddress + path.TrimStart('/'));
        return res.StatusCode;
    }

    public async Task<bool> EntityExists(string id)
    {
        var res = await Get("entities/" + id);
        return res.Item1 == HttpStatusCode.OK;
    }

    public async Task<bool> EntityExists(string id, string type)
    {
        var res = await Get("entities/" + id + "?type=" + type);
        return res.Item1 == HttpStatusCode.OK;
    }

    public async Task<JObject?> GetEntity(string id)
    {
        var res = await Get("entities/" + id);
        return res.Item1 == HttpStatusCode.OK ? JsonConvert.DeserializeObject<JObject>(res.Item2) : null;
    }

    public async Task<JObject?> GetEntity(string id, string type)
    {
        var res = await Get("entities/" + id + "?type=" + type);
        return res.Item1 == HttpStatusCode.OK ? JsonConvert.DeserializeObject<JObject>(res.Item2) : null;
    }

    public async Task<bool> DeleteEntity(string id)
    {
        var res = await Delete("entities/" + id);
        return res == HttpStatusCode.NoContent;
    }
    #endregion

    #region private methods
    private async Task Cleanup(string type)
    {
        var (getCode, getContent) = await Get("entities?typePattern=(" + type + "|Thing)&limit=1000");
        Assert.Equal(HttpStatusCode.OK, getCode);

        var entities = JsonConvert.DeserializeObject<IList<JObject>>(getContent) ?? Array.Empty<JObject>();
        if (entities.Count == 0) return;

        var ids = new JArray();
        foreach (var entity in entities)
        {
            if (!entity.ContainsKey("id") || !entity.ContainsKey("type")) continue;
            ids.Add(new JObject
            {
                ["id"] = entity["id"],
                ["type"] = entity["type"]
            });
        }
        var batch = new JObject
        {
            ["actionType"] = new JValue("delete"),
            ["entities"] = ids
        };

        var deleteCode = await Post("op/update", JsonConvert.SerializeObject(batch));
        Assert.Equal(HttpStatusCode.NoContent, deleteCode);
    }
    #endregion

    #region IDisposable interface
    public void Dispose() => Cleanup(EntityType).Wait();
    #endregion
}
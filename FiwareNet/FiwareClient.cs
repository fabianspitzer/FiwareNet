using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using FiwareNet.Converters;
using FiwareNet.Encoders;

namespace FiwareNet;

/// <summary>
/// A client for the Orion Context Broker using the FIWARE protocol.
/// </summary>
public sealed class FiwareClient : IDisposable
{
    #region private members
    private readonly ContractStore _contractStore;
    private readonly TypeResolver[] _typeResolvers;
    private readonly IDictionary<Type, TypeResolver> _typeResolverCache = new Dictionary<Type, TypeResolver>();
    private readonly IStringEncoder _fieldEncoder;
    private readonly IStringEncoder _valueEncoder;
    private readonly JsonSerializer _jsonSerializer;

    private readonly RestClient _client;
    private readonly IRequestHandler[] _requestHandlers = [];
    private readonly int _entitiesPerRequest;

    private readonly ISubscriptionServer _server;
    private readonly HttpNotificationConfig _notificationConfig;
    private readonly IDictionary<string, Subscription> _subscriptions = new Dictionary<string, Subscription>();

    private const string DefaultAttrs = "dateCreated,dateModified,dateExpires,*";
    #endregion

    #region constructors
    /// <summary>
    /// Creates a new <see cref="FiwareClient"/> instance.
    /// </summary>
    /// <param name="endpoint">The endpoint of the context broker.</param>
    public FiwareClient(string endpoint) : this(endpoint, new FiwareSettings())
    { }

    /// <summary>
    /// Creates a new <see cref="FiwareClient"/> instance.
    /// </summary>
    /// <param name="endpoint">The endpoint of the context broker.</param>
    /// <param name="settings">Additional FIWARE settings.</param>
    public FiwareClient(string endpoint, FiwareSettings settings)
    {
        if (string.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));
        if (settings is null) throw new ArgumentNullException(nameof(settings));

        BrokerEndpoint = CleanupEndpoint(endpoint);

        if (settings.EntitiesPerRequest < 1) throw new IndexOutOfRangeException(nameof(settings.EntitiesPerRequest));
        _entitiesPerRequest = settings.EntitiesPerRequest;

        //encoders
        _fieldEncoder = settings.FieldEncoder ?? new DummyStringEncoder();
        _valueEncoder = settings.StringValueEncoder ?? new DummyStringEncoder();

        //JSON settings/converter
        var jsonSettings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Include};
        if (settings.TypeConverters != null)
        {
            foreach (var converter in settings.TypeConverters) jsonSettings.Converters.Add(converter);
        }
        jsonSettings.Converters.Add(new DateTimeConverter());
        jsonSettings.Converters.Add(new NullableDateTimeConverter());
        jsonSettings.Converters.Add(new RegexConverter());
        jsonSettings.Converters.Add(new EntityFilterConverter(_fieldEncoder));
        _jsonSerializer = JsonSerializer.Create(jsonSettings);

        //type name resolving
        var typeMap = settings.TypeMap ?? TypeMap.GetJsonMap();
        _contractStore = new ContractStore(typeMap, _jsonSerializer);
        _typeResolvers = settings.TypeResolvers?.ToArray() ?? [];

        //REST client
        var restOptions = new RestClientOptions(BrokerEndpoint)
        {
            ClientCertificates = settings.ClientCertificates,
            UserAgent = settings.UserAgent ?? $"FiwareClient/{Assembly.GetExecutingAssembly().GetName().Version}",
            MaxTimeout = settings.Timeout
        };
        _client = new RestClient(restOptions, configureSerialization: s => s.UseNewtonsoftJson(jsonSettings));
        _client.AddDefaultParameter("Accept-Language", settings.AcceptLanguage, ParameterType.HttpHeader);
        if (settings.RequestHandlers != null) _requestHandlers = settings.RequestHandlers as IRequestHandler[] ?? [..settings.RequestHandlers];

        //subscription server
        var address = settings.SubscriptionAddress ?? GetLocalIpAddress();
        var port = settings.SubscriptionPort > 0 ? settings.SubscriptionPort : GetAvailablePort();
        var bindPort = settings.SubscriptionBindPort > 0 ? settings.SubscriptionBindPort : port;
        if (settings.UseMqtt)
        {
            SubscriptionEndpoint = $"mqtt://{address}:{port}";
            _server = new MqttSubscriptionServer(bindPort, settings.MqttTopic);
            _notificationConfig = new MqttNotificationConfig
            {
                Url = SubscriptionEndpoint,
                Topic = settings.MqttTopic,
                QualityOfService = 1
            };
        }
        else
        {
            SubscriptionEndpoint = $"http://{address}:{port}";
            _server = new TcpSubscriptionServer(bindPort);
            _notificationConfig = new HttpNotificationConfig {Url = SubscriptionEndpoint};
        }
        _server.OnNotification += OnNotification;
    }
    #endregion

    #region public properties
    /// <summary>
    /// Gets the used context broker endpoint.
    /// </summary>
    public string BrokerEndpoint { get; }

    /// <summary>
    /// Gets the used local subscription endpoint.
    /// </summary>
    public string SubscriptionEndpoint { get; }

    /// <summary>
    /// Gets a value indicating whether the subscription server has been started.
    /// </summary>
    public bool SubscriptionServerStarted => _server.IsStarted;
    #endregion

    #region public methods
    #region info
    /// <summary>
    /// Gets current information about the Orion context broker.
    /// </summary>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the parsed information.</returns>
    public async Task<(ActionResponse, OrionInfo)> GetInfo(CancellationToken token = default)
    {
        var request = new RestRequest("../version");
        return await SendReceive<OrionInfo>(request, token).ConfigureAwait(false);
    }
    #endregion

    #region entities
    #region create
    /// <summary>
    /// Creates the given entity.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to create.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateEntity<T>(T entity, CancellationToken token = default) where T : class
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        var request = new RestRequest("entities", Method.Post).AddJsonBody(SerializeEntity(entity));
        return Send(request, token);
    }

    /// <summary>
    /// Creates a collection entities.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">A collection of entities to create.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateEntities<T>(IEnumerable<T> entities, CancellationToken token = default) where T : class
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));

        var batchAction = new BatchActionObject {ActionType = "appendStrict", Entities = SerializeEntities(entities)};
        var request = new RestRequest("op/update", Method.Post).AddJsonBody(batchAction);
        return Send(request, token);
    }
    #endregion

    #region update
    /// <summary>
    /// Updates all attributes of a given entity.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateEntity<T>(T entity, CancellationToken token = default) where T : class
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        var contract = _contractStore.GetOrCreate(entity.GetType());
        var id = _fieldEncoder.EncodeField(contract.GetEntityId(entity));
        var type = _fieldEncoder.EncodeField(contract.GetEntityType(entity));
        var jsonEntity = SerializeEntity(entity);
        jsonEntity.Remove("id");
        jsonEntity.Remove("type");

        var request = new RestRequest($"entities/{id}/attrs?type={type}", Method.Patch).AddJsonBody(jsonEntity);
        return Send(request, token);
    }

    /// <summary>
    /// Updates all attributes of all given entities.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">A collection of entities to update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateEntities<T>(IEnumerable<T> entities, CancellationToken token = default) where T : class
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));

        var batchAction = new BatchActionObject {ActionType = "update", Entities = SerializeEntities(entities)};
        var request = new RestRequest("op/update", Method.Post).AddJsonBody(batchAction);
        return Send(request, token);
    }
    #endregion

    #region create or update
    /// <summary>
    /// Creates the given entity or updates it if it already exists.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to create.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateEntity<T>(T entity, CancellationToken token = default) where T : class
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        return CreateOrUpdateEntities(new[] {entity}, token);
    }

    /// <summary>
    /// Creates a collection entities or updates them if they already exist.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">A collection of entities to create.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateEntities<T>(IEnumerable<T> entities, CancellationToken token = default) where T : class
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));

        var batchAction = new BatchActionObject {ActionType = "append", Entities = SerializeEntities(entities)};
        var request = new RestRequest("op/update", Method.Post).AddJsonBody(batchAction);
        return Send(request, token);
    }
    #endregion

    #region get
    /// <summary>
    /// Gets the entity matching the given ID.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the parsed entity.</returns>
    public Task<(ActionResponse, T)> GetEntity<T>(string id, CancellationToken token = default) where T : class
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

        return InternalGetEntity<T>(id, null, token);
    }

    /// <summary>
    /// Gets the entity matching the given ID and type.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type name of the entity.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the parsed entity.</returns>
    public Task<(ActionResponse, T)> GetEntity<T>(string id, string type, CancellationToken token = default) where T : class
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));

        return InternalGetEntity<T>(id, type, token);
    }

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<DynamicEntity>)> GetEntities(CancellationToken token = default) => GetEntities<DynamicEntity>(token);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntities<T>(CancellationToken token = default) where T : class
    {
        return InternalGetEntities<T>(null, null, null, null, null, token);
    }

    /// <summary>
    /// Gets all entities matching the given ID.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="id">The ID of the entities.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntities<T>(string id, CancellationToken token = default) where T : class
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

        return InternalGetEntities<T>(id, null, null, null, null, token);
    }

    /// <summary>
    /// Gets all entities matching the given ID pattern.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="idPattern">The ID pattern to query the entities.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntities<T>(Regex idPattern, CancellationToken token = default) where T : class
    {
        if (idPattern is null) throw new ArgumentNullException(nameof(idPattern));

        return InternalGetEntities<T>(null, null, idPattern, null, null, token);
    }

    /// <summary>
    /// Gets all entities matching the given ID pattern and type.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="idPattern">The ID pattern to query the entities.</param>
    /// <param name="type">The type name of the entities.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntities<T>(Regex idPattern, string type, CancellationToken token = default) where T : class
    {
        if (idPattern is null) throw new ArgumentNullException(nameof(idPattern));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));

        return InternalGetEntities<T>(null, type, idPattern, null, null, token);
    }

    /// <summary>
    /// Gets all entities matching the given ID and type pattern.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="id">The ID of the entities.</param>
    /// <param name="typePattern">The type pattern to query the entities.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntities<T>(string id, Regex typePattern, CancellationToken token = default) where T : class
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (typePattern is null) throw new ArgumentNullException(nameof(typePattern));

        return InternalGetEntities<T>(id, null, null, typePattern, null, token);
    }

    /// <summary>
    /// Gets all entities matching the given ID pattern and type pattern.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="idPattern">The ID pattern to query the entities.</param>
    /// <param name="typePattern">The type pattern to query the entities.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntities<T>(Regex idPattern, Regex typePattern, CancellationToken token = default) where T : class
    {
        if (idPattern is null) throw new ArgumentNullException(nameof(idPattern));
        if (typePattern is null) throw new ArgumentNullException(nameof(typePattern));

        return InternalGetEntities<T>(null, null, idPattern, typePattern, null, token);
    }

    /// <summary>
    /// Gets all entities that match the given <see cref="EntityFilter"/> object.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="filter">The filter for the entities to get.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntities<T>(EntityFilter filter, CancellationToken token = default) where T : class
    {
        if (filter is null) throw new ArgumentNullException(nameof(filter));

        return InternalGetEntities<T>(filter.Id, filter.Type, filter.IdPattern, filter.TypePattern, null, token);
    }

    /// <summary>
    /// Gets all entities that match the given collection of <see cref="EntityFilter"/> objects.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="filters">The collection of filters for the entities to get.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntities<T>(IEnumerable<EntityFilter> filters, CancellationToken token = default) where T : class
    {
        if (filters is null) throw new ArgumentNullException(nameof(filters));

        return InternalGetEntities<T>(filters, null, token);
    }

    /// <summary>
    /// Gets all entities matching the given type.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="type">The type name of the entities.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntitiesByType<T>(string type, CancellationToken token = default) where T : class
    {
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));

        return InternalGetEntities<T>(null, type, null, null, null, token);
    }

    /// <summary>
    /// Gets all entities matching the given type pattern.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="typePattern">The type pattern to query the entities.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntitiesByType<T>(Regex typePattern, CancellationToken token = default) where T : class
    {
        if (typePattern is null) throw new ArgumentNullException(nameof(typePattern));

        return InternalGetEntities<T>(null, null, null, typePattern, null, token);
    }

    /// <summary>
    /// Gets all entities matching the given query.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The query to use.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntitiesByQuery<T>(string query, CancellationToken token = default) where T : class
    {
        if (string.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

        return InternalGetEntities<T>(null, null, null, null, query, token);
    }

    /// <summary>
    /// Gets all entities matching the given type and query.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="type">The type name of the entities.</param>
    /// <param name="query">The query to use.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntitiesByQuery<T>(string type, string query, CancellationToken token = default) where T : class
    {
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

        return InternalGetEntities<T>(null, type, null, null, query, token);
    }

    /// <summary>
    /// Gets all entities matching the given type pattern and query.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="typePattern">The type pattern to query the entities.</param>
    /// <param name="query">The query to use.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntitiesByQuery<T>(Regex typePattern, string query, CancellationToken token = default) where T : class
    {
        if (typePattern is null) throw new ArgumentNullException(nameof(typePattern));
        if (string.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

        return InternalGetEntities<T>(null, null, null, typePattern, query, token);
    }

    /// <summary>
    /// Gets all entities that match the given <see cref="EntityFilter"/> object and query.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="filter">The entity filter to use for the query.</param>
    /// <param name="query">The query to use.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntitiesByQuery<T>(EntityFilter filter, string query, CancellationToken token = default) where T : class
    {
        if (filter is null) throw new ArgumentNullException(nameof(filter));

        return GetEntitiesByQuery<T>(new[] {filter}, query, token);
    }

    /// <summary>
    /// Gets all entities that match the given collection of <see cref="EntityFilter"/> objects and query.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="filters">The entity filter to use for the query.</param>
    /// <param name="query">The query to use.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of parsed entities.</returns>
    public Task<(ActionResponse, IList<T>)> GetEntitiesByQuery<T>(IEnumerable<EntityFilter> filters, string query, CancellationToken token = default) where T : class
    {
        if (filters is null) throw new ArgumentNullException(nameof(filters));
        if (string.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

        return InternalGetEntities<T>(filters, query, token);
    }
    #endregion

    #region delete
    /// <summary>
    /// Deletes the given entity.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteEntity<T>(T entity, CancellationToken token = default) where T : class
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        var contract = _contractStore.GetOrCreate(entity.GetType());
        return DeleteEntity(contract.GetEntityId(entity), contract.GetEntityType(entity), token);
    }

    /// <summary>
    /// Deletes the entity matching the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteEntity(string id, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}", Method.Delete);
        return Send(request, token);
    }

    /// <summary>
    /// Deletes the entity matching the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity to delete.</param>
    /// <param name="type">The type name of the entity.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteEntity(string id, string type, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));

        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}?type={_fieldEncoder.EncodeField(type)}", Method.Delete);
        return Send(request, token);
    }

    /// <summary>
    /// Deletes all given entities.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteEntities<T>(IEnumerable<T> entities, CancellationToken token = default) where T : class
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));

        var items = new List<EntityFilter>();
        foreach (var entity in entities)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entities), "FIWARE entity cannot be null.");

            var contract = _contractStore.GetOrCreate(entity.GetType());
            items.Add(new EntityFilter(contract.GetEntityId(entity), contract.GetEntityType(entity)));
        }

        var batchAction = new BatchActionObject {ActionType = "delete", Entities = items};
        var request = new RestRequest("op/update", Method.Post).AddJsonBody(batchAction);
        return Send(request, token);
    }

    /// <summary>
    /// Deletes all entities matching the given IDs.
    /// </summary>
    /// <param name="ids">The IDs of the entities to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteEntities(IEnumerable<string> ids, CancellationToken token = default)
    {
        if (ids is null) throw new ArgumentNullException(nameof(ids));

        var items = new List<EntityFilter>();
        foreach (var id in ids)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(ids), "FIWARE ID cannot be empty.");
            items.Add(new EntityFilter(id));
        }

        var batchAction = new BatchActionObject {ActionType = "delete", Entities = items};
        var request = new RestRequest("op/update", Method.Post).AddJsonBody(batchAction);
        return Send(request, token);
    }

    /// <summary>
    /// Deletes all entities that match the given <see cref="EntityFilter"/> object.
    /// </summary>
    /// <param name="filter">The entity to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public async Task<ActionResponse> DeleteEntities(EntityFilter filter, CancellationToken token = default)
    {
        if (filter is null) throw new ArgumentNullException(nameof(filter));

        var (res1, entities) = await GetEntities<MinimalEntity>(filter, token).ConfigureAwait(false);
        if (res1.IsBad) return res1;
        if (entities.Count == 0) return new ActionResponse {Code = HttpStatusCode.NotFound};

        var items = new List<EntityFilter>();
        foreach (var entity in entities) items.Add(entity);

        var batchAction = new BatchActionObject {ActionType = "delete", Entities = items};
        var request = new RestRequest("op/update", Method.Post).AddJsonBody(batchAction);
        return await Send(request, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes all entities that match the given collection of <see cref="EntityFilter"/> objects.
    /// </summary>
    /// <param name="filters">The entities to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public async Task<ActionResponse> DeleteEntities(IEnumerable<EntityFilter> filters, CancellationToken token = default)
    {
        if (filters is null) throw new ArgumentNullException(nameof(filters));
        var filterArr = filters.ToArray();
        if (filterArr.Length == 0) throw new ArgumentException("Filter collection cannot be empty", nameof(filters)); //would return/delete ALL entities

        var (res1, entities) = await GetEntities<MinimalEntity>(filterArr, token).ConfigureAwait(false);
        if (res1.IsBad) return res1;
        if (entities.Count == 0) return new ActionResponse {Code = HttpStatusCode.NotFound};

        var items = new List<EntityFilter>();
        foreach (var entity in entities) items.Add(entity);

        var batchAction = new BatchActionObject {ActionType = "delete", Entities = items};
        var request = new RestRequest("op/update", Method.Post).AddJsonBody(batchAction);
        return await Send(request, token).ConfigureAwait(false);
    }
    #endregion
    #endregion

    #region attributes
    #region create
    /// <summary>
    /// Creates the given attribute for the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to create.</param>
    /// <param name="data">The attribute data to create.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateAttribute(string id, string attribute, AttributeData data, CancellationToken token = default) => CreateAttribute(id, attribute, data, false, token);

    /// <summary>
    /// Creates the given attribute for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to create.</param>
    /// <param name="data">The attribute data to create.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateAttribute(string id, string type, string attribute, AttributeData data, CancellationToken token = default) => CreateAttribute(id, type, attribute, data, false, token);

    /// <summary>
    /// Creates the given attribute for the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to create.</param>
    /// <param name="data">The attribute data to create.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateAttribute(string id, string attribute, AttributeData data, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalCreateAttributes(id, null, new Dictionary<string, AttributeData> {{attribute, data}}, skipEncode, token);
    }

    /// <summary>
    /// Creates the given attribute for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to create.</param>
    /// <param name="data">The attribute data to create.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateAttribute(string id, string type, string attribute, AttributeData data, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalCreateAttributes(id, type, new Dictionary<string, AttributeData> {{attribute, data}}, skipEncode, token);
    }

    /// <summary>
    /// Creates the given attribute for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="data">The attribute data to create.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateAttributes(string id, IDictionary<string, AttributeData> data, CancellationToken token = default) => CreateAttributes(id, data, false, token);

    /// <summary>
    /// Creates the given attributes for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="data">The attribute data to create.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateAttributes(string id, string type, IDictionary<string, AttributeData> data, CancellationToken token = default) => CreateAttributes(id, type, data, false, token);

    /// <summary>
    /// Creates the given attributes for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="data">The attribute data to create.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateAttributes(string id, IDictionary<string, AttributeData> data, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalCreateAttributes(id, null, data, skipEncode, token);
    }

    /// <summary>
    /// Creates the given attributes for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="data">The attribute data to create.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateAttributes(string id, string type, IDictionary<string, AttributeData> data, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalCreateAttributes(id, type, data, skipEncode, token);
    }
    #endregion

    #region update
    /// <summary>
    /// Updates the attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to update.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttribute(string id, string attribute, AttributeData data, CancellationToken token = default) => UpdateAttribute(id, attribute, data, false, false, token);

    /// <summary>
    /// Updates the attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to update.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttribute(string id, string type, string attribute, AttributeData data, CancellationToken token = default) => UpdateAttribute(id, type, attribute, data, false, false, token);

    /// <summary>
    /// Updates the attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to update.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttribute(string id, string attribute, AttributeData data, bool overrideMetadata, CancellationToken token = default) => UpdateAttribute(id, attribute, data, overrideMetadata, false, token);

    /// <summary>
    /// Updates the attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to update.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttribute(string id, string type, string attribute, AttributeData data, bool overrideMetadata, CancellationToken token = default) => UpdateAttribute(id, type, attribute, data, overrideMetadata, false, token);

    /// <summary>
    /// Updates the attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to update.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttribute(string id, string attribute, AttributeData data, bool overrideMetadata, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalUpdateAttribute(id, null, attribute, data, overrideMetadata, skipEncode, token);
    }

    /// <summary>
    /// Updates the attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to update.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttribute(string id, string type, string attribute, AttributeData data, bool overrideMetadata, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalUpdateAttribute(id, type, attribute, data, overrideMetadata, skipEncode, token);
    }

    /// <summary>
    /// Updates all given attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttributes(string id, IDictionary<string, AttributeData> data, CancellationToken token = default) => UpdateAttributes(id, data, false, false, token);

    /// <summary>
    /// Updates all given attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttributes(string id, string type, IDictionary<string, AttributeData> data, CancellationToken token = default) => UpdateAttributes(id, type, data, false, false, token);

    /// <summary>
    /// Updates all given attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttributes(string id, IDictionary<string, AttributeData> data, bool overrideMetadata, CancellationToken token = default) => UpdateAttributes(id, data, overrideMetadata, false, token);

    /// <summary>
    /// Updates all given attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttributes(string id, string type, IDictionary<string, AttributeData> data, bool overrideMetadata, CancellationToken token = default) => UpdateAttributes(id, type, data, overrideMetadata, false, token);

    /// <summary>
    /// Updates all given attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttributes(string id, IDictionary<string, AttributeData> data, bool overrideMetadata, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalUpdateAttributes(id, null, data, overrideMetadata, skipEncode, token);
    }

    /// <summary>
    /// Updates all given attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttributes(string id, string type, IDictionary<string, AttributeData> data, bool overrideMetadata, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalUpdateAttributes(id, type, data, overrideMetadata, skipEncode, token);
    }

    /// <summary>
    /// Updates the value of an attribute from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to update.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttributeValue(string id, string attribute, object value, CancellationToken token = default) => UpdateAttributeValue(id, attribute, value, false, token);

    /// <summary>
    /// Updates the value of an attribute from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to update.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttributeValue(string id, string type, string attribute, object value, CancellationToken token = default) => UpdateAttributeValue(id, type, attribute, value, false, token);

    /// <summary>
    /// Updates the value of an attribute from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to update.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttributeValue(string id, string attribute, object value, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));

        return InternalUpdateAttributeValue(id, null, attribute, value, skipEncode, token);
    }

    /// <summary>
    /// Updates the value of an attribute from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to update.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateAttributeValue(string id, string type, string attribute, object value, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));

        return InternalUpdateAttributeValue(id, type, attribute, value, skipEncode, token);
    }
    #endregion

    #region update or create
    /// <summary>
    /// Creates or updates the given attribute for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to create or update.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttribute(string id, string attribute, AttributeData data, CancellationToken token = default) => CreateOrUpdateAttribute(id, attribute, data, false, false, token);

    /// <summary>
    /// Creates or updates the given attribute for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to create or update.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttribute(string id, string type, string attribute, AttributeData data, CancellationToken token = default) => CreateOrUpdateAttribute(id, type, attribute, data, false, false, token);

    /// <summary>
    /// Creates or updates the given attribute for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to create or update.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttribute(string id, string attribute, AttributeData data, bool overrideMetadata, CancellationToken token = default) => CreateOrUpdateAttribute(id, attribute, data, overrideMetadata, false, token);

    /// <summary>
    /// Creates or updates the given attribute for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to create or update.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttribute(string id, string type, string attribute, AttributeData data, bool overrideMetadata, CancellationToken token = default) => CreateOrUpdateAttribute(id, type, attribute, data, overrideMetadata, false, token);

    /// <summary>
    /// Creates or updates the given attribute for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to create or update.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttribute(string id, string attribute, AttributeData data, bool overrideMetadata, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalCreateOrUpdateAttributes(id, null, new Dictionary<string, AttributeData> {{attribute, data}}, overrideMetadata, skipEncode, token);
    }

    /// <summary>
    /// Creates or updates the given attribute for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to create or update.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttribute(string id, string type, string attribute, AttributeData data, bool overrideMetadata, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalCreateOrUpdateAttributes(id, type, new Dictionary<string, AttributeData> {{attribute, data}}, overrideMetadata, skipEncode, token);
    }

    /// <summary>
    /// Creates or updates the given attributes for the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttributes(string id, IDictionary<string, AttributeData> data, CancellationToken token = default) => CreateOrUpdateAttributes(id, data, false, false, token);

    /// <summary>
    /// Creates or updates the given attributes for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttributes(string id, string type, IDictionary<string, AttributeData> data, CancellationToken token = default) => CreateOrUpdateAttributes(id, type, data, false, false, token);

    /// <summary>
    /// Creates or updates the given attributes for the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttributes(string id, IDictionary<string, AttributeData> data, bool overrideMetadata, CancellationToken token = default) => CreateOrUpdateAttributes(id, data, overrideMetadata, false, token);

    /// <summary>
    /// Creates or updates the given attributes for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttributes(string id, string type, IDictionary<string, AttributeData> data, bool overrideMetadata, CancellationToken token = default) => CreateOrUpdateAttributes(id, type, data, overrideMetadata, false, token);

    /// <summary>
    /// Creates or updates the given attributes for the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttributes(string id, IDictionary<string, AttributeData> data, bool overrideMetadata, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalCreateOrUpdateAttributes(id, null, data, overrideMetadata, skipEncode, token);
    }

    /// <summary>
    /// Creates or updates the given attributes for the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="data">The attribute data to create or update.</param>
    /// <param name="overrideMetadata">Whether to override existing metadata.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> CreateOrUpdateAttributes(string id, string type, IDictionary<string, AttributeData> data, bool overrideMetadata, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalCreateOrUpdateAttributes(id, type, data, overrideMetadata, skipEncode, token);
    }
    #endregion

    #region replace
    /// <summary>
    /// Replaces all attributes of the entity with the given ID with the given attributes.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="data">The attribute data to replace.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> ReplaceAttributes(string id, IDictionary<string, AttributeData> data, CancellationToken token = default) => ReplaceAttributes(id, data, false, token);

    /// <summary>
    /// Replaces all attributes of the entity with the given ID and type with the given attributes.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> ReplaceAttributes(string id, string type, IDictionary<string, AttributeData> data, CancellationToken token = default) => ReplaceAttributes(id, type, data, false, token);

    /// <summary>
    /// Replaces all attributes of the entity with the given ID with the given attributes.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> ReplaceAttributes(string id, IDictionary<string, AttributeData> data, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalReplaceAttributes(id, null, data, skipEncode, token);
    }

    /// <summary>
    /// Replaces all attributes of the entity with the given ID and type with the given attributes.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="data">The attribute data to update.</param>
    /// <param name="skipEncode">Whether to skip value encoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> ReplaceAttributes(string id, string type, IDictionary<string, AttributeData> data, bool skipEncode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (data is null) throw new ArgumentNullException(nameof(data));

        return InternalReplaceAttributes(id, type, data, skipEncode, token);
    }
    #endregion

    #region get
    /// <summary>
    /// Gets the attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to get.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the data of the attribute.</returns>
    public Task<(ActionResponse, AttributeData)> GetAttribute(string id, string attribute, CancellationToken token = default) => GetAttribute(id, attribute, false, token);

    /// <summary>
    /// Gets the attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>s
    /// <param name="attribute">The name of the attribute to get.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the data of the attribute.</returns>
    public Task<(ActionResponse, AttributeData)> GetAttribute(string id, string type, string attribute, CancellationToken token = default) => GetAttribute(id, type, attribute, false, token);

    /// <summary>
    /// Gets the attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to get.</param>
    /// <param name="skipDecode">Whether to skip value decoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the data of the attribute.</returns>
    public Task<(ActionResponse, AttributeData)> GetAttribute(string id, string attribute, bool skipDecode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));

        return InternalGetAttribute(id, null, attribute, skipDecode, token);
    }

    /// <summary>
    /// Gets the attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to get.</param>
    /// <param name="skipDecode">Whether to skip value decoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the data of the attribute.</returns>
    public Task<(ActionResponse, AttributeData)> GetAttribute(string id, string type, string attribute, bool skipDecode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));

        return InternalGetAttribute(id, type, attribute, skipDecode, token);
    }

    /// <summary>
    /// Gets a list of all attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the list of <see cref="AttributeData"/> objects.</returns>
    public Task<(ActionResponse, IDictionary<string, AttributeData>)> GetAttributes(string id, CancellationToken token = default) => GetAttributes(id, false, token);

    /// <summary>
    /// Gets a list of all attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the list of <see cref="AttributeData"/> objects.</returns>
    public Task<(ActionResponse, IDictionary<string, AttributeData>)> GetAttributes(string id, string type, CancellationToken token = default) => GetAttributes(id, type, false, token);

    /// <summary>
    /// Gets a list of all attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="skipDecode">Whether to skip value decoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the list of <see cref="AttributeData"/> objects.</returns>
    public Task<(ActionResponse, IDictionary<string, AttributeData>)> GetAttributes(string id, bool skipDecode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

        return InternalGetAttributes(id, null, skipDecode, token);
    }

    /// <summary>
    /// Gets a list of all attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="skipDecode">Whether to skip value decoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the list of <see cref="AttributeData"/> objects.</returns>
    public Task<(ActionResponse, IDictionary<string, AttributeData>)> GetAttributes(string id, string type, bool skipDecode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));

        return InternalGetAttributes(id, type, skipDecode, token);
    }

    /// <summary>
    /// Gets a list of attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attributes">A list of attribute names to get.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the list of <see cref="AttributeData"/> objects in the same order as the requested attributes.</returns>
    public Task<(ActionResponse, IList<AttributeData>)> GetAttributes(string id, IEnumerable<string> attributes, CancellationToken token = default) => GetAttributes(id, attributes, false, token);

    /// <summary>
    /// Gets a list of attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attributes">A list of attribute names to get.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the list of <see cref="AttributeData"/> objects in the same order as the requested attributes.</returns>
    public Task<(ActionResponse, IList<AttributeData>)> GetAttributes(string id, string type, IEnumerable<string> attributes, CancellationToken token = default) => GetAttributes(id, type, attributes, false, token);

    /// <summary>
    /// Gets a list of attribute data from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attributes">A list of attribute names to get.</param>
    /// <param name="skipDecode">Whether to skip value decoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the list of <see cref="AttributeData"/> objects in the same order as the requested attributes.</returns>
    public Task<(ActionResponse, IList<AttributeData>)> GetAttributes(string id, IEnumerable<string> attributes, bool skipDecode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (attributes is null) throw new ArgumentNullException(nameof(attributes));

        return InternalGetAttributes(id, null, attributes, skipDecode, token);
    }

    /// <summary>
    /// Gets a list of attribute data from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attributes">A list of attribute names to get.</param>
    /// <param name="skipDecode">Whether to skip value decoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the list of <see cref="AttributeData"/> objects in the same order as the requested attributes.</returns>
    public Task<(ActionResponse, IList<AttributeData>)> GetAttributes(string id, string type, IEnumerable<string> attributes, bool skipDecode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (attributes is null) throw new ArgumentNullException(nameof(attributes));

        return InternalGetAttributes(id, type, attributes, skipDecode, token);
    }

    /// <summary>
    /// Gets the value of an attribute from the entity with the given ID.
    /// </summary>
    /// <typeparam name="T">The type of the attribute value.</typeparam>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to get.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the value of the attribute.</returns>
    public Task<(ActionResponse, T)> GetAttributeValue<T>(string id, string attribute, CancellationToken token = default) => GetAttributeValue<T>(id, attribute, false, token);

    /// <summary>
    /// Gets the value of an attribute from the entity with the given ID and type.
    /// </summary>
    /// <typeparam name="T">The type of the attribute value.</typeparam>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to get.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the value of the attribute.</returns>
    public Task<(ActionResponse, T)> GetAttributeValue<T>(string id, string type, string attribute, CancellationToken token = default) => GetAttributeValue<T>(id, type, attribute, false, token);

    /// <summary>
    /// Gets the value of an attribute from the entity with the given ID.
    /// </summary>
    /// <typeparam name="T">The type of the attribute value.</typeparam>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to get.</param>
    /// <param name="skipDecode">Whether to skip value decoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the value of the attribute.</returns>
    public Task<(ActionResponse, T)> GetAttributeValue<T>(string id, string attribute, bool skipDecode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));

        return InternalGetAttributeValue<T>(id, null, attribute, skipDecode, token);
    }

    /// <summary>
    /// Gets the value of an attribute from the entity with the given ID and type.
    /// </summary>
    /// <typeparam name="T">The type of the attribute value.</typeparam>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to get.</param>
    /// <param name="skipDecode">Whether to skip value decoding.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the value of the attribute.</returns>
    public Task<(ActionResponse, T)> GetAttributeValue<T>(string id, string type, string attribute, bool skipDecode, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));

        return InternalGetAttributeValue<T>(id, type, attribute, skipDecode, token);
    }
    #endregion

    #region delete
    /// <summary>
    /// Deletes an attribute from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attribute">The name of the attribute to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteAttribute(string id, string attribute, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));

        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs/{_fieldEncoder.EncodeField(attribute)}", Method.Delete);
        return Send(request, token);
    }

    /// <summary>
    /// Deletes an attribute from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attribute">The name of the attribute to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteAttribute(string id, string type, string attribute, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attribute));

        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs/{_fieldEncoder.EncodeField(attribute)}?type={_fieldEncoder.EncodeField(type)}", Method.Delete);
        return Send(request, token);
    }

    /// <summary>
    /// Deletes all given attributes from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="attributes">The names of the attributes to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteAttributes(string id, IEnumerable<string> attributes, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (attributes is null) throw new ArgumentNullException(nameof(attributes));

        return InternalDeleteAttributes(id, null, attributes, token);
    }

    /// <summary>
    /// Deletes all given attributes from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="attributes">The names of the attributes to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteAttributes(string id, string type, IEnumerable<string> attributes, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
        if (attributes is null) throw new ArgumentNullException(nameof(attributes));

        return InternalDeleteAttributes(id, type, attributes, token);
    }

    /// <summary>
    /// Deletes all attributes from the entity with the given ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteAllAttributes(string id, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

        return InternalDeleteAllAttributes(id, null, token);
    }

    /// <summary>
    /// Deletes all attributes from the entity with the given ID and type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteAllAttributes(string id, string type, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));

        return InternalDeleteAllAttributes(id, type, token);
    }
    #endregion
    #endregion

    #region subscriptions
    /// <summary>
    /// Starts the subscription server to listen for incoming subscription notifications.
    /// </summary>
    public Task StartSubscriptionServer() => _server.IsStarted ? Task.CompletedTask : _server.Start();

    /// <summary>
    /// Stops the subscription server.
    /// </summary>
    /// <param name="deleteSubscriptions">Whether to delete all current subscriptions from the context broker.</param>
    public async Task StopSubscriptionServer(bool deleteSubscriptions = true)
    {
        if (_server.IsStarted)
        {
            await _server.Stop().ConfigureAwait(false);

            if (deleteSubscriptions)
            {
                var tasks = new List<Task>();
                foreach (var id in _subscriptions.Keys) tasks.Add(DeleteSubscription(id));
                await Task.WhenAll([..tasks]).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Creates a new subscription for a given entity.
    /// The subscription will only report changed attributes on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">A <see cref="EntityBase"/> object to monitor.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public Task<(ActionResponse, Subscription<T>)> CreateSubscription<T>(T entity, CancellationToken token = default) where T : class
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        return CreateSubscription<T>(new[] {entity}, null, token);
    }

    /// <summary>
    /// Creates a new subscription for a given entity.
    /// The subscription will only report changed attributes on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">A <see cref="EntityBase"/> object to monitor.</param>
    /// <param name="description">A description for the subscription.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public Task<(ActionResponse, Subscription<T>)> CreateSubscription<T>(T entity, string description, CancellationToken token = default) where T : class
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        return CreateSubscription<T>(new[] {entity}, description, token);
    }

    /// <summary>
    /// Creates a new subscription for a given collection of entities.
    /// The subscription will only report changed attributes on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">A collection of <see cref="EntityBase"/> objects to monitor.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public Task<(ActionResponse, Subscription<T>)> CreateSubscription<T>(IEnumerable<T> entities, CancellationToken token = default) where T : class
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));

        return CreateSubscription(entities, null, token);
    }

    /// <summary>
    /// Creates a new subscription for a given collection of entities.
    /// The subscription will only report changed attributes on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">A collection of <see cref="EntityBase"/> objects to monitor.</param>
    /// <param name="description">A description for the subscription.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public async Task<(ActionResponse, Subscription<T>)> CreateSubscription<T>(IEnumerable<T> entities, string description, CancellationToken token = default) where T : class
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));

        var entityDict = new Dictionary<string, T>();
        var items = new List<EntityFilter>();
        foreach (var entity in entities)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entities), "FIWARE entity cannot be null.");

            var contract = _contractStore.GetOrCreate(entity.GetType());
            items.Add(new EntityFilter(contract.GetEntityId(entity), contract.GetEntityType(entity)));
            entityDict.Add(contract.GetEntityId(entity), entity);
        }

        var (res, subscriptionId) = await InternalCreateSubscription<T>(items, description, true, token).ConfigureAwait(false);
        if (res.IsBad) return (res, null);

        var subscription = new Subscription<T>(subscriptionId, description, entityDict);
        _subscriptions.Add(subscriptionId, subscription);
        return (res, subscription);
    }

    /// <summary>
    /// Creates a new subscription for a given <see cref="EntityFilter"/> object.
    /// The subscription will only report changed attributes on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">A <see cref="EntityFilter"/> object to monitor.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public Task<(ActionResponse, Subscription<T>)> CreateSubscription<T>(EntityFilter entity, CancellationToken token = default) where T : class
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        return CreateSubscription<T>(new[] {entity}, null, token);
    }

    /// <summary>
    /// Creates a new subscription for a given <see cref="EntityFilter"/> object.
    /// The subscription will only report changed attributes on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">A <see cref="EntityFilter"/> object to monitor.</param>
    /// <param name="description">A description for the subscription.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public Task<(ActionResponse, Subscription<T>)> CreateSubscription<T>(EntityFilter entity, string description, CancellationToken token = default) where T : class
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        return CreateSubscription<T>(new[] {entity}, description, token);
    }

    /// <summary>
    /// Creates a new subscription for a given collection of <see cref="EntityFilter"/> objects.
    /// The subscription will only report changed attributes on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">A collection of <see cref="EntityFilter"/> objects to monitor.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public Task<(ActionResponse, Subscription<T>)> CreateSubscription<T>(IEnumerable<EntityFilter> entities, CancellationToken token = default) where T : class
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));

        return CreateSubscription<T>(entities, null, token);
    }

    /// <summary>
    /// Creates a new subscription for a given collection of <see cref="EntityFilter"/> objects.
    /// The subscription will only report changed attributes on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">A collection of <see cref="EntityFilter"/> objects to monitor.</param>
    /// <param name="description">A description for the subscription.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public async Task<(ActionResponse, Subscription<T>)> CreateSubscription<T>(IEnumerable<EntityFilter> entities, string description, CancellationToken token = default) where T : class
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));

        var (res, subscriptionId) = await InternalCreateSubscription<T>(entities, description, true, token).ConfigureAwait(false);
        if (res.IsBad) return (res, null);

        var subscription = new Subscription<T>(subscriptionId, description, false);
        _subscriptions.Add(subscriptionId, subscription);
        return (res, subscription);
    }

    /// <summary>
    /// Creates a new subscription for a given <see cref="EntityFilter"/> object.
    /// The subscription will always report the entire entity on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">A <see cref="EntityFilter"/> object to monitor.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public Task<(ActionResponse, Subscription<T>)> CreateEntitySubscription<T>(EntityFilter entity, CancellationToken token = default) where T : class
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        return CreateEntitySubscription<T>(new[] {entity}, null, token);
    }

    /// <summary>
    /// Creates a new subscription for a given <see cref="EntityFilter"/> object.
    /// The subscription will always report the entire entity on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">A <see cref="EntityFilter"/> object to monitor.</param>
    /// <param name="description">A description for the subscription.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public Task<(ActionResponse, Subscription<T>)> CreateEntitySubscription<T>(EntityFilter entity, string description, CancellationToken token = default) where T : class
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        return CreateEntitySubscription<T>(new[] {entity}, description, token);
    }

    /// <summary>
    /// Creates a new subscription for a given collection of <see cref="EntityFilter"/> objects.
    /// The subscription will always report the entire entity on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">A collection of <see cref="EntityFilter"/> objects to monitor.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public Task<(ActionResponse, Subscription<T>)> CreateEntitySubscription<T>(IEnumerable<EntityFilter> entities, CancellationToken token = default) where T : class
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));

        return CreateEntitySubscription<T>(entities, null, token);
    }

    /// <summary>
    /// Creates a new subscription for a given collection of <see cref="EntityFilter"/> objects.
    /// The subscription will always report the entire entity on notification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">A collection of <see cref="EntityFilter"/> objects to monitor.</param>
    /// <param name="description">A description for the subscription.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and the newly created subscription.</returns>
    public async Task<(ActionResponse, Subscription<T>)> CreateEntitySubscription<T>(IEnumerable<EntityFilter> entities, string description, CancellationToken token = default) where T : class
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));

        var (res, subscriptionId) = await InternalCreateSubscription<T>(entities, description, false, token).ConfigureAwait(false);
        if (res.IsBad) return (res, null);

        var subscription = new Subscription<T>(subscriptionId, description, true);
        _subscriptions.Add(subscriptionId, subscription);
        return (res, subscription);
    }

    /// <summary>
    /// Gets details about a subscription.
    /// </summary>
    /// <param name="id">The ID of the subscription.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and information about the subscription.</returns>
    public Task<(ActionResponse, SubscriptionInfo)> GetSubscription(string id, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

        var request = new RestRequest($"subscriptions/{id}");
        return SendReceive<SubscriptionInfo>(request, token);
    }

    /// <summary>
    /// Gets details about all subscriptions currently on the broker.
    /// </summary>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information, and a collection of information about all subscriptions.</returns>
    public Task<(ActionResponse, IList<SubscriptionInfo>)> GetSubscriptions(CancellationToken token = default)
    {
        var request = new RestRequest("subscriptions");
        return SendReceivePaginated<SubscriptionInfo>(request, token);
    }

    /// <summary>
    /// Updates details of a subscription.
    /// </summary>
    /// <param name="subscriptionInfo">The subscription details to update.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> UpdateSubscription(SubscriptionInfo subscriptionInfo, CancellationToken token = default)
    {
        if (subscriptionInfo is null) throw new ArgumentNullException(nameof(subscriptionInfo));
        if (subscriptionInfo.Id is null) throw new ArgumentNullException(nameof(subscriptionInfo.Id), "Missing subscription ID.");

        var jsonObject = JObject.FromObject(subscriptionInfo, _jsonSerializer);
        jsonObject.Remove("id");
        if (jsonObject["notification"] is JObject notification)
        {
            notification.Remove("timesSent");
            notification.Remove("lastNotification");
            notification.Remove("lastSuccess");
            notification.Remove("lastSuccessCode");
            notification.Remove("lastFailure");
            notification.Remove("lastFailureReason");
            notification.Remove("failsCounter");
        }

        var request = new RestRequest($"subscriptions/{subscriptionInfo.Id}", Method.Patch).AddJsonBody(jsonObject);
        return Send(request, token);
    }

    /// <summary>
    /// Deletes a subscription from the broker.
    /// </summary>
    /// <param name="subscription">The subscription to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteSubscription(Subscription subscription, CancellationToken token = default)
    {
        if (subscription is null) throw new ArgumentNullException(nameof(subscription));

        return DeleteSubscription(subscription.Id, token);
    }

    /// <summary>
    /// Deletes a subscription from the broker.
    /// </summary>
    /// <param name="subscription">The subscription to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public Task<ActionResponse> DeleteSubscription(SubscriptionInfo subscription, CancellationToken token = default)
    {
        if (subscription is null) throw new ArgumentNullException(nameof(subscription));

        return DeleteSubscription(subscription.Id, token);
    }

    /// <summary>
    /// Deletes a subscription with the given ID from the broker.
    /// </summary>
    /// <param name="id">The ID of the subscription to delete.</param>
    /// <param name="token">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ActionResponse"/> object containing additional information.</returns>
    public async Task<ActionResponse> DeleteSubscription(string id, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

        var request = new RestRequest($"subscriptions/{id}", Method.Delete);

        var res = await Send(request, token).ConfigureAwait(false);
        if (res.IsGood) _subscriptions.Remove(id);
        return res;
    }
    #endregion
    #endregion

    #region private methods
    private string CleanupEndpoint(string endpoint)
    {
        //fix protocol if missing
        if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = "http://" + endpoint;
        }

        //ensure trailing slash
        if (endpoint[endpoint.Length - 1] != '/') endpoint += '/';

        //validate URL structure
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _)) throw new ArgumentException("The provided endpoint is not a valid URL.", nameof(endpoint));

        //check API version
        var match = Regex.Match(endpoint, @"\/[vV](\d+)\/$", RegexOptions.RightToLeft | RegexOptions.IgnoreCase);
        if (match.Success)
        {
            if (match.Groups[1].Value != "2") throw new ArgumentException("Only the version 2 endpoint (/v2/) is supported.", nameof(endpoint));
        }
        else //fix missing API version
        {
            endpoint += "v2/";
        }

        return endpoint;
    }

    #region entities
    private async Task<(ActionResponse, T)> InternalGetEntity<T>(string id, string type, CancellationToken token)
    {
        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}?attrs={DefaultAttrs}");
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));

        var (res, json) = await SendReceive<JObject>(request, token).ConfigureAwait(false);
        if (res.IsBad) return (res, default);

        var entity = DeserializeEntity<T>(json);
        return (res, entity);
    }

    private async Task<(ActionResponse, IList<T>)> InternalGetEntities<T>(string id, string type, Regex idPattern, Regex typePattern, string query, CancellationToken token)
    {
        var request = new RestRequest($"entities?attrs={DefaultAttrs}");
        if (id is not null) request.AddQueryParameter("id", _fieldEncoder.EncodeField(id));
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));
        if (idPattern is not null) request.AddQueryParameter("idPattern", idPattern.ToString());
        if (typePattern is not null) request.AddQueryParameter("typePattern", typePattern.ToString());
        if (query is not null) request.AddQueryParameter("q", query);

        var (res, json) = await SendReceivePaginated<JToken>(request, token).ConfigureAwait(false);
        if (res.IsBad) return (res, default);

        var entities = DeserializeEntities<T>(json);
        return (res, entities);
    }

    private async Task<(ActionResponse, IList<T>)> InternalGetEntities<T>(IEnumerable<EntityFilter> filters, string query, CancellationToken token)
    {
        var entityQuery = new EntityQuery
        {
            Entities = filters,
            Attributes = DefaultAttrs.Split(',')
        };
        if (query is not null) entityQuery.Expression = new Dictionary<string, string> {{"q", query}};

        var request = new RestRequest("op/query", Method.Post).AddJsonBody(entityQuery);
        var (res, json) = await SendReceivePaginated<JToken>(request, token).ConfigureAwait(false);
        if (res.IsBad) return (res, default);

        var entities = DeserializeEntities<T>(json);
        return (res, entities);
    }
    #endregion

    #region attributes
    private Task<ActionResponse> InternalCreateAttributes(string id, string type, IDictionary<string, AttributeData> data, bool skipEncode, CancellationToken token)
    {
        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs?options=append", Method.Post);
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));

        var json = new JObject();
        foreach (var item in data)
        {
            if (item.Value is null) throw new ArgumentNullException(nameof(data), "Attribute data cannot be null.");
            json.Add(_fieldEncoder.EncodeField(item.Key), SerializeAttribute(item.Value, skipEncode));
        }
        request.AddJsonBody(json);

        return Send(request, token);
    }

    private Task<ActionResponse> InternalUpdateAttribute(string id, string type, string attribute, AttributeData data, bool overrideMetadata, bool skipEncode, CancellationToken token)
    {
        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs/{_fieldEncoder.EncodeField(attribute)}", Method.Put);
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));
        if (overrideMetadata) request.AddQueryParameter("options", "overrideMetadata");
        request.AddJsonBody(SerializeAttribute(data, skipEncode));

        return Send(request, token);
    }

    private Task<ActionResponse> InternalUpdateAttributes(string id, string type, IDictionary<string, AttributeData> data, bool overrideMetadata, bool skipEncode, CancellationToken token)
    {
        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs", Method.Patch);
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));
        if (overrideMetadata) request.AddQueryParameter("options", "overrideMetadata");

        var json = new JObject();
        foreach (var item in data)
        {
            if (item.Value is null) throw new ArgumentNullException(nameof(data), "Attribute data cannot be null.");
            json.Add(_fieldEncoder.EncodeField(item.Key), SerializeAttribute(item.Value, skipEncode));
        }
        request.AddJsonBody(json);

        return Send(request, token);
    }

    private Task<ActionResponse> InternalUpdateAttributeValue(string id, string type, string attribute, object value, bool skipEncode, CancellationToken token)
    {
        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs/{_fieldEncoder.EncodeField(attribute)}/value", Method.Put);
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));

        var json = JToken.FromObject(value, _jsonSerializer);
        request.AddJsonBody(skipEncode ? json : EncodeValue(json));

        return Send(request, token);
    }

    private Task<ActionResponse> InternalCreateOrUpdateAttributes(string id, string type, IDictionary<string, AttributeData> data, bool overrideMetadata, bool skipEncode, CancellationToken token)
    {
        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs", Method.Post);
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));
        if (overrideMetadata) request.AddQueryParameter("options", "overrideMetadata");

        var json = new JObject();
        foreach (var item in data)
        {
            json.Add(_fieldEncoder.EncodeField(item.Key), SerializeAttribute(item.Value, skipEncode));
        }
        request.AddJsonBody(json);

        return Send(request, token);
    }

    private Task<ActionResponse> InternalReplaceAttributes(string id, string type, IDictionary<string, AttributeData> data, bool skipEncode, CancellationToken token)
    {
        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs", Method.Put);
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));

        var json = new JObject();
        foreach (var item in data)
        {
            if (item.Value is null) throw new ArgumentNullException(nameof(data), "Attribute data cannot be null.");
            json.Add(_fieldEncoder.EncodeField(item.Key), SerializeAttribute(item.Value, skipEncode));
        }
        request.AddJsonBody(json);

        return Send(request, token);
    }

    private async Task<(ActionResponse, AttributeData)> InternalGetAttribute(string id, string type, string attribute, bool skipDecode, CancellationToken token)
    {
        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs/{_fieldEncoder.EncodeField(attribute)}");
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));

        var (res, json) = await SendReceive<JObject>(request, token).ConfigureAwait(false);
        if (res.IsBad) return (res, null);

        var data = DeserializeAttribute(json, skipDecode);
        return (res, data);
    }

    private async Task<(ActionResponse, IDictionary<string, AttributeData>)> InternalGetAttributes(string id, string type, bool skipDecode, CancellationToken token)
    {
        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs");
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));

        var (res, json) = await SendReceive<JObject>(request, token).ConfigureAwait(false);
        if (res.IsBad) return (res, null);

        var data = new Dictionary<string, AttributeData>();
        foreach (var prop in json)
        {
            if (prop.Value is not JObject obj) continue;
            data.Add(_fieldEncoder.DecodeField(prop.Key), DeserializeAttribute(obj, skipDecode));
        }
        return (res, data);
    }

    private async Task<(ActionResponse, IList<AttributeData>)> InternalGetAttributes(string id, string type, IEnumerable<string> attributes, bool skipDecode, CancellationToken token)
    {
        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs");
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));

        var encodedAttributes = new List<string>();
        foreach (var attribute in attributes)
        {
            if (!string.IsNullOrEmpty(attribute)) encodedAttributes.Add(_fieldEncoder.EncodeField(attribute));
        }
        if (encodedAttributes.Count == 0) throw new ArgumentException("FIWARE attribute list cannot be empty.", nameof(attributes));

        var (res, json) = await SendReceive<JObject>(request, token).ConfigureAwait(false);
        if (res.IsBad) return (res, null);

        var data = new List<AttributeData>();
        foreach (var attribute in encodedAttributes)
        {
            if (json[attribute] is JObject obj) data.Add(DeserializeAttribute(obj, skipDecode));
            else data.Add(null);
        }
        return (res, data);
    }

    private async Task<(ActionResponse, T)> InternalGetAttributeValue<T>(string id, string type, string attribute, bool skipDecode, CancellationToken token)
    {
        var request = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}/attrs/{_fieldEncoder.EncodeField(attribute)}/value").AddHeader("Accept", "*/*");
        if (type is not null) request.AddQueryParameter("type", _fieldEncoder.EncodeField(type));

        var (res, json) = await SendReceive<JToken>(request, token).ConfigureAwait(false);
        if (res.IsBad) return (res, default);

        if (!skipDecode) json = DecodeValue(json);
        var value = json.ToObject<T>(_jsonSerializer);
        return (res, value);
    }

    private async Task<ActionResponse> InternalDeleteAttributes(string id, string type, IEnumerable<string> attributes, CancellationToken token)
    {
        var data = new Dictionary<string, string> {{"id", _fieldEncoder.EncodeField(id)}};
        if (type is not null) data.Add("type", _fieldEncoder.EncodeField(type));

        var count = 0;
        foreach (var attribute in attributes)
        {
            if (string.IsNullOrEmpty(attribute)) throw new ArgumentNullException(nameof(attributes), "Attribute name cannot be null or empty.");
            if (attribute.Equals("id") || attribute.Equals("type")) continue;

            var encoded = _fieldEncoder.EncodeField(attribute);
            if (data.ContainsKey(encoded)) continue;

            data.Add(encoded, string.Empty);
            ++count;
        }
        if (count == 0) return new ActionResponse {Code = HttpStatusCode.NoContent}; //would delete entity if sent

        var batchAction = new BatchActionObject {ActionType = "delete", Entities = new[] {data}};
        var request2 = new RestRequest("op/update?options=keyValues", Method.Post).AddJsonBody(batchAction);
        return await Send(request2, token).ConfigureAwait(false);
    }

    private async Task<ActionResponse> InternalDeleteAllAttributes(string id, string type, CancellationToken token)
    {
        var request1 = new RestRequest($"entities/{_fieldEncoder.EncodeField(id)}?options=keyValues");
        if (type is not null) request1.AddQueryParameter("type", _fieldEncoder.EncodeField(type));

        var (res1, data) = await SendReceive<JObject>(request1, token).ConfigureAwait(false);
        if (res1.IsBad) return res1;
        if (data.Count == 2) return new ActionResponse {Code = HttpStatusCode.NoContent}; //only contains id and type; would delete entity if sent

        var batchAction = new BatchActionObject {ActionType = "delete", Entities = new[] {data}};
        var request2 = new RestRequest("op/update?options=keyValues", Method.Post).AddJsonBody(batchAction);
        return await Send(request2, token).ConfigureAwait(false);
    }
    #endregion

    private async Task<(ActionResponse, string)> InternalCreateSubscription<T>(IEnumerable<EntityFilter> entities, string description, bool onlyChangedAttrs, CancellationToken token)
    {
        //build subscription
        var contract = _contractStore.GetOrCreate<T>();
        var info = new SubscriptionInfo
        {
            Description = description,
            Subject = new SubscriptionSubject
            {
                Entities = entities.ToList(),
                Condition = new SubscriptionCondition
                {
                    Attributes = contract.Attributes.Select(attribute => _fieldEncoder.EncodeField(attribute.FiwareName)).ToList()
                }
            },
            Notification = new NotificationInfo
            {
                Attributes = DefaultAttrs.Split(','),
                OnlyChangedAttributes = onlyChangedAttrs
            }
        };
        if (_notificationConfig is MqttNotificationConfig mqttConfig) info.Notification.Mqtt = mqttConfig;
        else info.Notification.Http = _notificationConfig;

        var jsonObject = JObject.FromObject(info, _jsonSerializer);
        if (jsonObject["notification"] is JObject notification)
        {
            notification.Remove("timesSent");
            notification.Remove("failsCounter");
        }

        //publish subscription
        var request = new RestRequest("subscriptions", Method.Post).AddJsonBody(jsonObject);
        var (res, headers) = await SendHeader(request, token).ConfigureAwait(false);
        if (res.Code != HttpStatusCode.Created) return (res, null);

        //get subscription ID
        string subscriptionId = null;
        foreach (var header in headers)
        {
            if (header.Name != "Location") continue;

            var path = header.Value?.ToString();
            subscriptionId = path?.Substring(path.LastIndexOf('/') + 1);
            break;
        }
        return subscriptionId is null ? (new ActionResponse {Code = HttpStatusCode.BadRequest, Error = "Missing Location header in subscription create response."}, null) : (res, subscriptionId);
    }

    #region serialization
    private JObject SerializeEntity(object entity)
    {
        //get contract and serialized JSON object
        var targetType = entity.GetType();
        var contract = _contractStore.GetOrCreate(targetType);
        var jsonObject = JObject.FromObject(entity, _jsonSerializer);

        //encode main fields
        jsonObject["id"] = _fieldEncoder.EncodeField(contract.GetEntityId(entity));
        jsonObject["type"] = _fieldEncoder.EncodeField(contract.GetEntityType(entity));

        //serialize attributes
        foreach (var attribute in contract.Attributes)
        {
            var value = jsonObject[attribute.FiwareName];
            jsonObject.Remove(attribute.FiwareName);
            if (value is null || attribute.ReadOnly) continue;

            if (attribute.RawData)
            {
                if (!attribute.SkipEncode) value["value"] = EncodeValue(value["value"]);
            }
            else
            {
                var val = attribute.SkipEncode ? value : EncodeValue(value);

                value = new JObject
                {
                    ["value"] = val,
                    ["type"] = _fieldEncoder.EncodeField(attribute.FiwareType)
                };
            }

            jsonObject[_fieldEncoder.EncodeField(attribute.FiwareName)] = value;
        }

        //serialize attribute metadata
        foreach (var metadata in contract.MetadataAttributes)
        {
            var value = jsonObject[metadata.JsonPropertyName];
            jsonObject.Remove(metadata.JsonPropertyName);
            if (value is not JObject metadataValue) continue;

            if (jsonObject[_fieldEncoder.EncodeField(metadata.FiwareName)] is JObject attribute) //check if merging with FIWARE property is possible
            {
                attribute["metadata"] = metadataValue; //metadata fields are not encoded
            }
        }

        //handle dynamic entity
        if (targetType == typeof(DynamicEntity) && jsonObject[nameof(DynamicEntity.Attributes)] is JObject attributes)
        {
            jsonObject.Remove(nameof(DynamicEntity.Attributes));

            foreach (var prop in attributes)
            {
                if ( prop.Value is not JObject value) continue;

                jsonObject[_fieldEncoder.EncodeField(prop.Key)] = new JObject
                {
                    ["value"] = EncodeValue(value["value"]),
                    ["type"] = _fieldEncoder.EncodeField(value.GetValue("type")?.ToString()),
                    ["metadata"] = value["metadata"]
                };
            }
        }

        return jsonObject;
    }

    private JArray SerializeEntities(IEnumerable entities)
    {
        var arr = new JArray();
        foreach (var entity in entities)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entities), "FIWARE entity cannot be null.");
            arr.Add(SerializeEntity(entity));
        }
        return arr;
    }

    private T DeserializeEntity<T>(JObject jsonObject) => (T) DeserializeEntity(typeof(T), jsonObject);

    private object DeserializeEntity(Type type, JObject jsonObject)
    {
        //resolve type
        var entityId = _fieldEncoder.DecodeField(jsonObject.GetValue("id")?.ToString());
        var entityType = _fieldEncoder.DecodeField(jsonObject.GetValue("type")?.ToString());
        Type targetType = null;
        if (_typeResolverCache.TryGetValue(type, out var resolver)) //check if there is already a resolver for that base-type in the cache
        {
            if (resolver.CanResolve(type)) targetType = resolver.Resolve(entityId, entityType);
        }
        else
        {
            //check if base-type has a resolver attribute
            foreach (var attribute in type.GetCustomAttributes(false))
            {
                if (attribute is not FiwareTypeResolverAttribute fcr) continue;
                var typeResolver = (TypeResolver) Activator.CreateInstance(fcr.Resolver);
                _typeResolverCache.Add(type, typeResolver);
                if (typeResolver.CanResolve(type)) targetType = typeResolver.Resolve(entityId, entityType);
                break;
            }

            //fallback to list of resolvers
            if (targetType is null)
            {
                foreach (var typeResolver in _typeResolvers)
                {
                    if (!typeResolver.CanResolve(type)) continue;
                    targetType = typeResolver.Resolve(entityId, entityType);
                    break;
                }
            }
        }
        targetType ??= type;

        //get contract
        var contract = _contractStore.GetOrCreate(targetType);

        //decode main fields
        jsonObject["id"] = entityId;
        jsonObject["type"] = entityType;

        //unwrap/flatten attribute metadata
        foreach (var metadata in contract.MetadataAttributes)
        {
            if (jsonObject[metadata.FiwareName] is JObject attribute && attribute.TryGetValue("metadata", out var value))
            {
                jsonObject[metadata.JsonPropertyName] = value;
            }
        }

        //unwrap/flatten attributes
        foreach (var attribute in contract.Attributes)
        {
            var encodedName = _fieldEncoder.EncodeField(attribute.FiwareName);
            var value = jsonObject[encodedName];
            jsonObject.Remove(encodedName);
            if (value is not JObject obj) continue;

            if (attribute.RawData)
            {
                if (!attribute.SkipEncode) obj["value"] = DecodeValue(obj["value"]);
            }
            else
            {
                value = attribute.SkipEncode ? obj["value"] : DecodeValue(obj["value"]);
            }

            jsonObject[attribute.FiwareName] = value;
        }

        //handle dynamic entity
        if (targetType == typeof(DynamicEntity))
        {
            var attributes = new JObject();

            foreach (var prop in jsonObject)
            {
                if (prop.Key is "id" or "type" || prop.Value is not JObject value) continue;

                attributes[_fieldEncoder.DecodeField(prop.Key)] = new JObject
                {
                    ["value"] = DecodeValue(value["value"]),
                    ["type"] = _fieldEncoder.DecodeField(value.GetValue("type")?.ToString()),
                    ["metadata"] = value["metadata"] ?? new JObject()
                };
            }

            jsonObject[nameof(DynamicEntity.Attributes)] = attributes;
        }

        return jsonObject.ToObject(targetType, _jsonSerializer);
    }

    private IList<T> DeserializeEntities<T>(IEnumerable<JToken> jsonArray)
    {
        var entities = new List<T>();
        foreach (var entity in jsonArray)
        {
            if (entity is not JObject jsonObject) continue;
            entities.Add(DeserializeEntity<T>(jsonObject));
        }
        return entities;
    }

    private JObject SerializeAttribute(AttributeData attribute, bool skipEncode)
    {
        var json = JObject.FromObject(attribute, _jsonSerializer);

        if (!skipEncode && attribute.Type != FiwareTypes.TextUnrestricted) json["value"] = EncodeValue(json["value"]);
        if (!string.IsNullOrEmpty(attribute.Type)) json["type"] = _fieldEncoder.EncodeField(json.Value<string>("type"));

        return json;
    }

    private AttributeData DeserializeAttribute(JObject json, bool skipEncode)
    {
        var type = _fieldEncoder.DecodeField(json.Value<string>("type"));

        if (!skipEncode && type != FiwareTypes.TextUnrestricted) json["value"] = DecodeValue(json["value"]);
        json["type"] = _fieldEncoder.DecodeField(type);

        return json.ToObject<AttributeData>(_jsonSerializer);
    }

    private JToken EncodeValue(JToken token)
    {
        switch (token)
        {
            case JValue value:
                if (value.Type is JTokenType.String or JTokenType.Uri) value.Value = _valueEncoder.EncodeValue(value.Value?.ToString());
                break;
            case JArray array:
                foreach (var item in array) EncodeValue(item);
                break;
            case JObject obj:
                foreach (var prop in obj) EncodeValue(prop.Value); //properties are not encoded
                break;
        }

        return token;
    }

    private JToken DecodeValue(JToken token)
    {
        switch (token)
        {
            case JValue value:
                if (value.Type is JTokenType.String or JTokenType.Uri) value.Value = _valueEncoder.DecodeValue(value.Value?.ToString());
                break;
            case JArray array:
                foreach (var item in array) DecodeValue(item);
                break;
            case JObject obj:
                foreach (var prop in obj) DecodeValue(prop.Value); //properties are not encoded
                break;
        }

        return token;
    }
    #endregion

    #region send data
    private async Task<ActionResponse> Send(RestRequest request, CancellationToken token)
    {
        var (res, _, _) = await SendReceiveHeader<JObject>(request, token).ConfigureAwait(false);
        return res;
    }

    private async Task<(ActionResponse, IEnumerable<Parameter>)> SendHeader(RestRequest request, CancellationToken token)
    {
        var (res, _, headers) = await SendReceiveHeader<JObject>(request, token).ConfigureAwait(false);
        return (res, headers);
    }

    private async Task<(ActionResponse, T)> SendReceive<T>(RestRequest request, CancellationToken token)
    {
        var (res, data, _) = await SendReceiveHeader<T>(request, token).ConfigureAwait(false);
        return (res, data);
    }

    private async Task<(ActionResponse, T, IEnumerable<Parameter>)> SendReceiveHeader<T>(RestRequest request, CancellationToken token)
    {
        foreach (var handler in _requestHandlers) handler.OnRequest(this, request);

        RestResponse<T> result;
        try
        {
            result = await _client.ExecuteAsync<T>(request, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return (new ActionResponse {Code = HttpStatusCode.BadRequest, Error = ex.GetType().Name, ErrorDescription = ex.Message}, default, default);
        }

        if (result.ResponseStatus == ResponseStatus.TimedOut) return (new ActionResponse {Code = HttpStatusCode.RequestTimeout, Error = result.ErrorMessage}, default, default);

        switch (result.StatusCode)
        {
            case HttpStatusCode.OK:
            case HttpStatusCode.Created:
            case HttpStatusCode.Accepted:
            case HttpStatusCode.NoContent:
                return result.ErrorException is null
                    ? (new ActionResponse {Code = result.StatusCode}, result.Data, result.Headers)
                    : (new ActionResponse {Code = HttpStatusCode.BadRequest, Error = result.ErrorException.GetType().Name, ErrorDescription = result.ErrorException.Message}, default, result.Headers);
            default:
                var ar = result.Content is null ? new ActionResponse() : JsonConvert.DeserializeObject<ActionResponse>(result.Content);
                if (ar is null) return (new ActionResponse {Code = HttpStatusCode.InternalServerError,  Error = "UnknownFormat", ErrorDescription = "Unknown payload format in response."}, default, result.Headers);
                ar.Code = result.StatusCode;
                return (ar, default, result.Headers);
        }
    }

    private async Task<(ActionResponse, IList<T>)> SendReceivePaginated<T>(RestRequest request, CancellationToken token)
    {
        request.AddQueryParameter("limit", _entitiesPerRequest);

        var result = new List<T>();
        var offset = 0;
        ActionResponse res;
        do
        {
            request.Parameters.RemoveParameter("offset");
            request.AddQueryParameter("offset", offset);

            (res, var data) = await SendReceive<IList<T>>(request, token).ConfigureAwait(false);
            if (res.IsBad) return (res, null);

            result.AddRange(data);
            offset += _entitiesPerRequest;
        } while (result.Count == offset);

        return (res, result);
    }
    #endregion

    #region network
    private static IPAddress GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) return ip;
        }

        return IPAddress.Loopback;
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint) listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
    #endregion
    #endregion

    #region private events
    private void OnNotification(ISubscriptionServer server, string rawNotification)
    {
        try
        {
            var json = _jsonSerializer.Deserialize<JObject>(new JsonTextReader(new StringReader(rawNotification)));

            //get active subscription
            var subscriptionId = json?["subscriptionId"]?.Value<string>();
            if (subscriptionId is null || !_subscriptions.ContainsKey(subscriptionId)) return;
            var subscription = _subscriptions[subscriptionId];

            //parse data section
            if (json["data"] is not JArray data) return;
            foreach (var token in data)
            {
                if (token is not JObject entry) continue;

                //find changed attribute
                var changedAttributes = new HashSet<string>();
                foreach (var prop in entry)
                {
                    if (prop.Key is "id" or "type") continue;
                    changedAttributes.Add(_fieldEncoder.DecodeField(prop.Key));
                }
                if (changedAttributes.Count == 0) continue;

                //deserialize entity
                if (subscription.Type == typeof(DynamicEntity))
                {
                    var entity = DeserializeEntity<DynamicEntity>(entry);
                    if (entity is null) continue;

                    //send notification
                    if (subscription.FullEntity) subscription.Notify(entity.Id, entity);
                    else
                    {
                        //get changed attributes
                        var attributes = new Dictionary<string, object>();
                        foreach (var attribute in entity.Attributes)
                        {
                            attributes.Add(attribute.Key, attribute.Value.Value);
                        }
                        subscription.Notify(entity.Id, attributes);
                    }
                }
                else
                {
                    var contract = _contractStore.GetOrCreate(subscription.Type);
                    var entity = DeserializeEntity(subscription.Type, entry);
                    if (contract is null || entity is null) continue;

                    //send notification
                    if (subscription.FullEntity) subscription.Notify(contract.GetEntityId(entity), entity);
                    else
                    {
                        //get changed attributes
                        var attributes = new Dictionary<string, object>();
                        foreach (var changedAttribute in changedAttributes)
                        {
                            var attribute = contract.GetFiwareAttribute(changedAttribute);
                            if (attribute is null) continue;

                            attributes.Add(attribute.PropertyName, attribute.Property.GetValue(entity));
                        }
                        subscription.Notify(contract.GetEntityId(entity), attributes);
                    }
                }
            }
        }
        catch
        {
            //ignore
        }
    }
    #endregion

    #region IDisposable interface
    /// <inheritdoc/>
    public void Dispose()
    {
        StopSubscriptionServer().Wait();
        _server?.Dispose();
    }
    #endregion
}
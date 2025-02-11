using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;

namespace FiwareNet;

internal class MqttSubscriptionServer : ISubscriptionServer
{
    #region private members
    private readonly string _subscriptionTopic;
    private readonly MqttServer _server;
    #endregion

    #region constructor
    public MqttSubscriptionServer(int port, string topic)
    {
        _subscriptionTopic = topic;

        var serverOptions = new MqttServerOptionsBuilder().WithDefaultEndpointPort(port).Build();
        _server = new MqttFactory().CreateMqttServer(serverOptions);
        _server.InterceptingPublishAsync += MqttMessageReceived;
    }
    #endregion

    #region private events
    private Task MqttMessageReceived(InterceptingPublishEventArgs args)
    {
        //check MQTT topic
        if (!args.ApplicationMessage.Topic.Equals(_subscriptionTopic)) return Task.CompletedTask;

        return Task.Run(() => OnNotification?.Invoke(this, Encoding.UTF8.GetString([..args.ApplicationMessage.PayloadSegment])));
    }
    #endregion

    #region ISubscriptionServer interface
    public bool IsStarted => _server.IsStarted;

    public Task Start() => _server.StartAsync();

    public Task Stop() => _server.StopAsync();

    public event ISubscriptionServer.NotificationEvent OnNotification;

    public void Dispose() => _server?.Dispose();
    #endregion
}
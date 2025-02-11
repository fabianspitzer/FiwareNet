using System;
using System.Threading.Tasks;

namespace FiwareNet;

internal interface ISubscriptionServer : IDisposable
{
    public bool IsStarted { get; }

    public Task Start();

    public Task Stop();

    public event NotificationEvent OnNotification;

    public delegate void NotificationEvent(ISubscriptionServer server, string notification);
}
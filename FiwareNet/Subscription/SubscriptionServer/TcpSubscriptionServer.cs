using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using FiwareNet.Utils;

namespace FiwareNet;

internal class TcpSubscriptionServer(int port) : ISubscriptionServer
{
    #region private members
    private const int BufferSize = 1024;
    private static readonly byte[] Response = "HTTP/1.1 200 OK\r\n"u8.ToArray();

    private readonly TcpListener _listener = TcpListener.Create(port);
    private bool _isStopping;
    #endregion

    #region ISubscriptionServer interface
    public bool IsStarted { get; private set; }

    public Task Start()
    {
        if (IsStarted) throw new InvalidOperationException("Server is already running.");

        _isStopping = false;
        _listener.Start(100);
        IsStarted = true;

        Task.Run(() =>
        {
            //wait for new connection
            while (!_isStopping)
            {
                var socket = _listener.AcceptSocket();
                HandleConnection(socket);
            }
        }).OnError(ex =>
        {
            if (_isStopping) return;
            _listener.Stop();
            IsStarted = false;
            throw ex;
        });

        return Task.CompletedTask;
    }

    public Task Stop()
    {
        if (!IsStarted) return Task.CompletedTask;

        return Task.Run(() =>
        {
            _isStopping = true;
            _listener.Stop();
            IsStarted = false;
        });
    }

    public event ISubscriptionServer.NotificationEvent OnNotification;

    public void Dispose() => Stop();
    #endregion

    #region private methods
    private void HandleConnection(Socket socket) => Task.Run(() =>
    {
        var buffer = new byte[BufferSize];
        var reader = new HttpReader();

        //read data
        while (!reader.BodyCompleted)
        {
            var bytesRead = socket.Receive(buffer, 0, BufferSize, SocketFlags.None);
            reader.Read(buffer, 0, bytesRead);
        }

        //respond with OK
        socket.Send(Response, 0, Response.Length, SocketFlags.None);

        //close connection
        socket.Shutdown(SocketShutdown.Both);
        socket.Close();

        //notify client
        OnNotification?.Invoke(this, reader.Body);
    });
    #endregion
}
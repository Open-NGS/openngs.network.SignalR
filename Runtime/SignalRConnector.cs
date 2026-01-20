using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

public class SignalRConnector : IConnector
{
    SignalR signalR;


    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnError;
    public float Latency { get; private set; } = 200;

    private Action<HttpConnectionOptions> configureHttpConnection;

    public SignalRConnector(IRetryPolicy retryPolicy, Action<HttpConnectionOptions> configure)
    {
        IsConnected = false;
        this.retryPolicy = retryPolicy;
        this.configureHttpConnection = configure;
    }

    private IRetryPolicy retryPolicy { get; set; }

    public bool IsConnected { get; private set; }
    public string AccessToken { get => accessToken; set { SetToken(value); } }

    private string accessToken;

    public void SetToken(string token)
    {
        this.accessToken = token;
        if (signalR != null)
            signalR.accessToken = token;
    }
    public void Init(string serverAddress)
    {
        signalR = new SignalR();
        signalR.accessToken = this.accessToken;
        signalR.ConnectionStarted += (object sender, ConnectionEventArgs e) =>
        {
            OnConnected?.Invoke();
            IsConnected = true;
        };
        signalR.ConnectionClosed += (object sender, ConnectionEventArgs e) =>
        {
            IsConnected = false;
            OnDisconnected?.Invoke();
        };

        signalR.Reconnecting += (object sender, ConnectionEventArgs e) =>
        {
            IsConnected = false;
            OnDisconnected?.Invoke();
        };
        signalR.Reconnected += (object sender, ConnectionEventArgs e) =>
        {
            IsConnected = true;
            OnConnected?.Invoke();
        };

        try
        {
            signalR.Init(serverAddress, retryPolicy, this.configureHttpConnection);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex.Message);
        }

        signalR.Remove("ReceiveMessage");
        signalR.On<string>("ReceiveMessage", message => OnMessageReceived?.Invoke(message));

    }
    public async Task Connect() => await signalR?.Connect();


    public void Disonnect()
    {
        IsConnected = false;
        signalR?.Stop();
    }

    public async void Invoke(string methodName, object arg1) => await signalR.Invoke(methodName, arg1);
    public async void Invoke(string methodName, object arg1, object arg2) => await signalR.Invoke(methodName, arg1, arg2);
    public async void Invoke(string methodName, object arg1, object arg2, object arg3) => await signalR.Invoke(methodName, arg1, arg2, arg3);


    public async Task InvokeAsync(string methodName) =>
        await signalR.InvokeAsync(methodName);

    public async Task<T> InvokeAsync<T>(string methodName) =>
        await signalR.InvokeAsync<T>(methodName);

    public async Task<T> InvokeAsync<T>(string methodName, object args1) =>
        await signalR.InvokeAsync<T>(methodName, args1);

    public async Task<T> InvokeAsync<T>(string methodName, object args1, object args2)
    {
        return await signalR.InvokeAsync<T>(methodName, args1, args2);
    }

    public async Task<T> InvokeAsync<T>(string methodName, object args1, object args2, object args3)
    {
        return await signalR.InvokeAsync<T>(methodName, args1, args2, args3);
    }

    public void Remove(string methodName) => signalR.Remove(methodName);
    public void On<T1>(string methodName, Action<T1> handler)
    {
        signalR.On(methodName, handler);
    }

    public async void SendCaller(string message)
    {
        var json = new JsonPayload
        {
            message = message
        };
       await signalR.Invoke("SendCaller", JsonUtility.ToJson(json));
    }

    public async void SendMessage(string message)
    {
        var json = new JsonPayload
        {
            message = message
        };
        await signalR.Invoke("SendMessage", JsonUtility.ToJson(json));
    }

    public void SendMessageToAll(string message)
    {
        throw new NotImplementedException();
    }

    public void SendToAll(string message)
    {
        throw new NotImplementedException();
    }


    private readonly Stopwatch _stopwatch = new Stopwatch();
    public void ProfStart()
    {
        _stopwatch.Restart();
    }

    public void ProfStop()
    {
        _stopwatch.Stop();
        this.Latency = _stopwatch.ElapsedMilliseconds;
    }

    [Serializable]
    public class JsonPayload
    {
        public string message;
    }
}

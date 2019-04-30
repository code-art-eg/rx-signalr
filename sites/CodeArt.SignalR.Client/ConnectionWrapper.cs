using Microsoft.AspNet.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// A wrapper around <see cref="HubConnection"/>
  /// </summary>
  internal class ConnectionWrapper : RefCountedObject<ConnectionOptions>, IConnectionState
  {
    /// <summary>
    /// Underlying hub connection
    /// </summary>
    private HubConnection _hubConnection;
    private readonly string _hubName;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="key">connection options</param>
    public ConnectionWrapper(ConnectionOptions key, string hubName) : base(key)
    {
      if (string.IsNullOrWhiteSpace(hubName))
      {
        throw new ArgumentException("Hub name cannot be empty.", nameof(hubName));
      }
      _hubName = hubName;
    }

    /// <summary>
    /// Event for connection state change
    /// </summary>
    public event Action<StateChange> StateChanged;

    /// <summary>
    /// Event for connection state change
    /// </summary>
    public event EventHandler ConnectedChanged;

    /// <summary>
    /// Connection Id
    /// </summary>
    public string Id => _hubConnection?.ConnectionId;

    private bool _connected = false;
    /// <summary>
    /// Whether the connection is in "Connected" state
    /// </summary>
    public bool Connected
    {
      get
      {
        return _connected;
      }
      private set
      {
        if (value != _connected)
        {
          _connected = value;
          ConnectedChanged?.Invoke(this, EventArgs.Empty);
        }
      }
    }

    /// <summary>
    /// Current connection state
    /// </summary>
    public ConnectionState State => _hubConnection == null ? ConnectionState.Disconnected : _hubConnection.State;

    /// <summary>
    /// Hub proxy instance
    /// </summary>
    public IHubProxy HubProxy { get; private set; }

    /// <summary>
    /// start the connection
    /// </summary>
    protected override void OnStart()
    {
      // create the connection
      _hubConnection = new HubConnection(Key.Url, Key.QueryString, Key.UseDefaultPath);
      HubProxy = _hubConnection.CreateHubProxy(_hubName);
      // listen for state changes
      _hubConnection.StateChanged += HandleStateChange;
      // start
      _hubConnection.Start();
    }

    /// <summary>
    /// Handle stop
    /// </summary>
    /// <returns></returns>
    protected override Task OnStop()
    {
      if (_hubConnection == null)
      {
        return CompletedTask;
      }
      // Stop the connection
      if (Exception == null)
      {
        _hubConnection.Stop(TimeSpan.FromSeconds(2));
      }
      // stop listening for state changes
      _hubConnection.StateChanged -= HandleStateChange;
      // dispose
      // _hubConnection.Dispose();
      // clear
      _hubConnection = null;
      Connected = false;
      return CompletedTask;
    }

    /// <summary>
    /// Handle state change
    /// </summary>
    /// <param name="change"></param>
    private void HandleStateChange(StateChange change)
    {
      // notify subscribers
      StateChanged?.Invoke(change);
      Connected = change.NewState == ConnectionState.Connected;

      if (change.NewState == ConnectionState.Disconnected)
      {
        if (!Completed)
        {
          // If a disconnect occurs without stop being called, trigger error state
          var whatHappened = change.OldState == ConnectionState.Connecting ? "failed" : "was lost";
          OnError(new ApplicationException($"Connection to {Key.Url} {whatHappened}."));
        }
      }
    }
  }
}

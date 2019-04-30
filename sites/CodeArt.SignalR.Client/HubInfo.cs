using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// Hub wrapper
  /// </summary>
  internal class HubInfo : RefCountedObject<HubOptions>, IConnectionState, IHubWrapper
  {
    /// <summary>
    /// Object that unsubscribe an event subscribed by <see cref="HubInfo.On{T}(string, Action{T})"/> when disposed
    /// </summary>
    private class OnUnsubscriber: IDisposable
    {
      /// <summary>
      /// underlying hub
      /// </summary>
      private readonly HubInfo _info;

      /// <summary>
      /// id of event to unsubscribe
      /// </summary>
      public readonly int _id;

      /// <summary>
      /// constructor
      /// </summary>
      /// <param name="info">hub info</param>
      /// <param name="id">id of event to unsubscribe</param>
      public OnUnsubscriber(HubInfo info, int id)
      {
        _info = info;
        _id = id;
      }

      /// <summary>
      /// Call Of to unsubscribe event
      /// </summary>
      public void Dispose() => _info.Off(_id);
    }

    /// <summary>
    /// An object to leavegroups when dispose is called
    /// </summary>
    private class GroupsUnsubscriber : IDisposable
    {
      /// <summary>
      /// underlying hub
      /// </summary>
      private readonly HubInfo _info;
      private readonly IEnumerable<string> _groups;

      /// <summary>
      /// constructor
      /// </summary>
      /// <param name="info">hub</param>
      /// <param name="groups">groups to leave</param>
      public GroupsUnsubscriber(HubInfo info, IEnumerable<string> groups)
      {
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _groups = groups;
      }

      /// <summary>
      /// leave groups
      /// </summary>
      public async void Dispose()
      {
        if (_groups == null)
        {
          return;
        }
        foreach (var group in _groups)
        {
          await _info._groups.StopItemByKey(group).ConfigureAwait(false);
        }
      }
    }

    /// <summary>
    /// Global collection of hubs
    /// </summary>
    private static readonly RefCountedCollection<HubOptions, HubInfo> _hubs = new RefCountedCollection<HubOptions, HubInfo>(
      (key) => new HubInfo(key)
    );

    /// <summary>
    /// Get hub by options
    /// </summary>
    /// <param name="options">hub options</param>
    /// <returns>Hub info</returns>
    public static HubInfo GetByKey(HubOptions options)
    {
      return _hubs.GetByKey(options);
    }

    /// <summary>
    /// underlying connection wrapper
    /// </summary>
    private ConnectionWrapper _connectionWrapper;
    
    /// <summary>
    /// A collection of joined groups
    /// </summary>
    private readonly RefCountedCollection<string, GroupInfo> _groups;

    /// <summary>
    /// Callbacks
    /// </summary>
    private readonly Dictionary<int, HubCallbackInfo> _callbacks = new Dictionary<int, HubCallbackInfo>();

    /// <summary>
    /// next unique id for callback
    /// </summary>
    private int _nextId = 0;

    /// <summary>
    /// Reconnect cancellation (used to cancel reconnect if stop is called)
    /// </summary>
    private CancellationTokenSource reconnectCancellationSource;

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="key">hub options</param>
    private HubInfo(HubOptions key) : base(key)
    {
      _groups = new RefCountedCollection<string, GroupInfo>((name) => new GroupInfo(this, name));

    }

    /// <summary>
    /// Whether the hub is in connected state
    /// </summary>
    public bool Connected => _connectionWrapper?.Connected ?? false;

    /// <summary>
    /// Event to notify subscribers of connection state changes
    /// </summary>
    public event EventHandler ConnectedChanged;

    /// <summary>
    /// Connection id
    /// </summary>
    public string Id => _connectionWrapper?.Id;

    /// <summary>
    /// Invokes a method on hub (will fail if disconnected)
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="methodName">method name</param>
    /// <param name="args">args list</param>
    /// <returns>data returned from server</returns>
    public Task<T> InvokeAsync<T>(string methodName, params object[] args)
    {
      return _connectionWrapper.HubProxy.Invoke<T>(methodName, args);
    }

    /// <summary>
    /// Invokes a method on hub (will fail if disconnected)
    /// </summary>
    /// <param name="methodName">method name</param>
    /// <param name="args">args list</param>
    public Task InvokeAsync(string methodName, params object[] args)
    {
      return _connectionWrapper.HubProxy.Invoke(methodName, args);
    }

    /// <summary>
    /// Listens for an event on the hub
    /// </summary>
    /// <typeparam name="T">event data type</typeparam>
    /// <param name="eventName">event name</param>
    /// <param name="action">action to call when event is triggered</param>
    /// <returns>id used to unsubribe by calling off method</returns>
    public IDisposable On<T>(string eventName, Action<T> action)
    {
      var callback = new HubCallbackInfo
      {
        EventName = eventName,
        Id = Interlocked.Increment(ref _nextId),
        Subscriber = () => _connectionWrapper.HubProxy.On(eventName, action),
      };
      if (_connectionWrapper.HubProxy != null)
      {
        callback.Unsubscriber = callback.Subscriber();
      }
      lock (_callbacks)
      {
        _callbacks.Add(callback.Id, callback);
      }
      return new OnUnsubscriber(this, callback.Id);
    }

    

    public IDisposable JoinGroups(IEnumerable<string> groups)
    {
      if (groups != null)
      {
        foreach (var group in groups)
        {
          _groups.GetByKey(group);
        }
      }
      return new GroupsUnsubscriber(this, groups);
    }

    protected override void OnStart()
    {
      StartConnection();
    }

    protected override async Task OnStop()
    {
      ReleaseConnection(true);
      lock (_callbacks)
      {
        _callbacks.Clear();
        if (reconnectCancellationSource != null)
        {
          reconnectCancellationSource.Cancel();
        }
      }
      await _groups.StopAll();
      await _connectionWrapper?.Stop();
    }

    /// <summary>
    /// unsubscribe an event
    /// </summary>
    /// <param name="id">event id</param>
    private void Off(int id)
    {
      lock (_callbacks)
      {
        if (_callbacks.TryGetValue(id, out var callback))
        {
          callback.Unsubscriber?.Dispose();
          callback.Unsubscriber = null;
          _callbacks.Remove(id);
        }
      }
    }

    /// <summary>
    /// Release a connection
    /// </summary>
    /// <param name="isStopping">whether releasing because stop is called or because an error occcured causing connection to be errored</param>
    private void ReleaseConnection(bool isStopping)
    {
      lock (_callbacks)
      {
        // unsubscribe all call backs
        foreach (var item in _callbacks)
        {
          if (item.Value.Unsubscriber != null)
          {
            item.Value.Unsubscriber.Dispose();
            item.Value.Unsubscriber = null;
          }
        }
        if (isStopping)
        {
          _callbacks.Clear();
        }
        else
        {
          if (_connectionWrapper != null)
          {
            _connectionWrapper.ConnectedChanged -= HandleConnectionChange;
          }
        }
      }
    }

    /// <summary>
    /// Handle connection state changes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    private void HandleConnectionChange(object sender, EventArgs eventArgs)
    {
      ConnectedChanged?.Invoke(sender, eventArgs);
    }

    /// <summary>
    /// Start connection
    /// </summary>
    private void StartConnection()
    {
      // release old connection if any
      ReleaseConnection(false);
      lock (_callbacks)
      {
        // if cancellation was requested return
        var cancellationRequested = false;
        if (reconnectCancellationSource != null)
        {
          cancellationRequested = reconnectCancellationSource.IsCancellationRequested;
          reconnectCancellationSource.Dispose();
          reconnectCancellationSource = null;
        }
        if (cancellationRequested)
        {
          return;
        }

        // create a new connection
        _connectionWrapper = new ConnectionWrapper(Key.ConnectionOptions, Key.Name);
        _connectionWrapper.ConnectedChanged += HandleConnectionChange;
        _connectionWrapper.AddRef();

        // resubscribe any events that were subscribed on old hub and connection
        foreach (var item in _callbacks)
        {
          item.Value.Unsubscriber = item.Value.Subscriber();
        }

        // register for reconnection if connection is lost
        _connectionWrapper.CompletedTask.ContinueWith(async (t) =>
        {
          // if completed no need to reconnect because hub was stopped
          if (Completed)
          {
            return;
          }
          // wait then attempt reconnect
          reconnectCancellationSource = new CancellationTokenSource();
          await Task.Delay(5000);
          StartConnection();
        }, TaskScheduler.Default);
      }
    }
  }
}

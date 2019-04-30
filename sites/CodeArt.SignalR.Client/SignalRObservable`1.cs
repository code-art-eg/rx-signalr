using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArt.SignalR.Client
{

  /// <summary>
  /// An observable object that wraps SignalR hub connection
  /// </summary>
  /// <typeparam name="T">Type of events emitted</typeparam>
  public class SignalRObservable<T> : IObservable<T>, IConnectionState, IHubWrapper
  {
    /// <summary>
    /// Object that handles a single subscriptions
    /// </summary>
    private class SignalRObservableUnsubscriber : IDisposable
    {
      /// <summary>
      /// subscribed Observer
      /// </summary>
      private readonly IObserver<T> _observer;
      /// <summary>
      /// Parent observable
      /// </summary>
      private readonly SignalRObservable<T> _observable;

      /// <summary>
      /// Hub info
      /// </summary>
      private readonly HubInfo _hub;

      /// <summary>
      /// Cancellation to cancel waiting for hub to complete
      /// </summary>
      private readonly CancellationTokenSource _hubStopCancellationSource = new CancellationTokenSource();

      /// <summary>
      /// Contination task that handles hub completion
      /// </summary>
      private readonly Task _continuationTask;

      /// <summary>
      /// Unsubscriber for groups
      /// </summary>
      private readonly IDisposable _groupDisposable;

      /// <summary>
      /// Unsubscriber for event
      /// </summary>
      private readonly IDisposable _eventDisposable;

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="observer">observer</param>
      /// <param name="observable">observable</param>
      public SignalRObservableUnsubscriber(IObserver<T> observer, SignalRObservable<T> observable)
      {
        _observer = observer;
        _observable = observable;
        lock (observable._lock)
        {
          _hub = _observable.Hub;
          if (_hub == null)
          {
            _hub = _observable.Hub = HubInfo.GetByKey(_observable._hubOptions);
          }
          else
          {
            _hub.AddRef();
          }
        }

        _continuationTask = _hub.CompletedTask.ContinueWith((t) =>
        {
          if (!t.IsCanceled)
          {
            if (_hub.Exception != null)
            {
              _observer.OnError(_hub.Exception);
            }
            else
            {
              _observer.OnCompleted();
            }
          }
          t.Dispose();
        }, _hubStopCancellationSource.Token);
        _groupDisposable = _hub.JoinGroups(_observable.Options.Groups);
        _eventDisposable = _hub.On<T>(_observable.Options.EventName, (e) =>
        {
          _observer.OnNext(e);
        });
      }

      /// <summary>
      /// Unsubscribe tear down logic
      /// </summary>
      public void Dispose()
      {
        _eventDisposable.Dispose();
        _groupDisposable.Dispose();
        _hubStopCancellationSource.Dispose();
        _hub.Stop();
        _observer.OnCompleted();
      }
    }

    /// <summary>
    /// Lock
    /// </summary>
    private readonly object _lock = new object();

    /// <summary>
    /// Hub info
    /// </summary>
    private HubInfo _hubInfo;

    /// <summary>
    /// Hub options
    /// </summary>
    private readonly HubOptions _hubOptions;


    /// <summary>
    /// Create a new instance of SignalRObservable that emits server side events occuring on a SignalR hub.
    /// </summary>
    /// <param name="options">Observable initializations options</param>
    /// <remarks>
    /// This will create a connection and connect to server only when there are subscribers
    /// and will automatically disconnect when last subscriber for all observers using same connection options unsubscribers.
    /// </remarks>
    public SignalRObservable(SignalRObservableOptions options)
    {
      Options = options ?? throw new ArgumentNullException(nameof(options));
      _hubOptions = new HubOptions(options.HubName, options.ConnectionOptions);
    }

    /// <summary>
    /// creates a new instance of SignalRObservable that emits server side events occuring on a SignalR hub.
    /// </summary>
    /// <param name="url">server url</param>
    /// <param name="hubName">hub name</param>
    /// <param name="eventName">event name</param>
    /// <param name="groupName">group name</param>
    public SignalRObservable(string url, string hubName, string eventName, string groupName = null) :
      this(
        new SignalRObservableOptions(
          new ConnectionOptions(url),
          hubName,
          eventName,
          string.IsNullOrEmpty(groupName) ? SignalREvents<T>._emptyGroups : new string[] { groupName }
        )
      )
    {

    }

    /// <summary>
    /// Options 
    /// </summary>
    public SignalRObservableOptions Options { get; }

    /// <summary>
    /// Creates a new subscription. A connection 
    /// </summary>
    /// <param name="observer">observer</param>
    /// <returns>Disposable to unsubscribe and release all resources by the subscription</returns>
    public IDisposable Subscribe(IObserver<T> observer)
    {
      return new SignalRObservableUnsubscriber(observer, this);
    }

    /// <summary>
    /// Hub
    /// </summary>
    private HubInfo Hub
    {
      get
      {
        return _hubInfo;
      }
      set
      {
        if (_hubInfo != null)
        {
          _hubInfo.ConnectedChanged -= HandleConnectionChange;
        }
        _hubInfo = value;
        if (value != null)
        {
          value.ConnectedChanged += HandleConnectionChange;
          value.CompletedTask.ContinueWith((t) =>
          {
            lock (_lock)
            {
              Hub = null;
            }
          }, TaskScheduler.Default);
        }
      }
    }

    /// <summary>
    /// Connection state
    /// </summary>
    public bool Connected => _hubInfo?.Connected ?? false;

    /// <summary>
    /// An event that notifies subscribers of connection status.
    /// </summary>
    public event EventHandler ConnectedChanged;

    /// <summary>
    /// Connection Id
    /// </summary>
    public string Id => _hubInfo?.Id;

    /// <summary>
    /// Invokes a method on hub (will fail if disconnected)
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="methodName">method name</param>
    /// <param name="args">args list</param>
    /// <returns>data returned from server</returns>
    public async Task<TResult> InvokeAsync<TResult>(string methodName, params object[] args)
    {
      if (Hub == null)
      {
        throw new InvalidOperationException("Cannot invoke operation while there are no subscriptions active on observable, therefore no connections.");
      }
      await this.WaitForConnected();
      return await Hub.InvokeAsync<TResult>(methodName, args);
    }

    /// <summary>
    /// Invokes a method on hub (will fail if disconnected)
    /// </summary>
    /// <param name="methodName">method name</param>
    /// <param name="args">args list</param>
    public async Task InvokeAsync(string methodName, params object[] args)
    {
      if (Hub == null)
      {
        throw new InvalidOperationException("Cannot invoke operation while there are no subscriptions active on observable, therefore no connections.");
      }
      await this.WaitForConnected();
      await Hub.InvokeAsync(methodName, args);
    }

    /// <summary>
    /// Handle connection state changes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HandleConnectionChange(object sender, EventArgs e)
    {
      ConnectedChanged?.Invoke(this, e);
    }
  }
}

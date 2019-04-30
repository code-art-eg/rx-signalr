using System;
using System.Threading.Tasks;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// A wrapper around SignalRObservable that uses events instead of using the Observer pattern
  /// </summary>
  /// <typeparam name="T">Event types</typeparam>
  public class SignalREvents<T> : IConnectionState, IHubWrapper, IDisposable
  {
    internal static readonly string[] _emptyGroups = { };

    /// <summary>
    /// Internal observer
    /// </summary>
    private class InternalObserver : IObserver<T>
    {
      private readonly SignalREvents<T> _signalREvents;

      public InternalObserver(SignalREvents<T> signalRObserver)
      {
        _signalREvents = signalRObserver;
      }

      // complete action
      public void OnCompleted() { }

      /// <summary>
      /// Error handler
      /// </summary>
      /// <param name="error"></param>
      public void OnError(Exception error) { }

      /// <summary>
      /// Next action
      /// </summary>
      /// <param name="value"></param>
      public void OnNext(T value)
      {
        _signalREvents._dataReceived?.Invoke(_signalREvents, new SignalREventArgs<T>(value));
      }
    }

    /// <summary>
    /// internal observerable
    /// </summary>
    private readonly SignalRObservable<T> _observable;

    /// <summary>
    /// Event handler chain
    /// </summary>
    private EventHandler<SignalREventArgs<T>> _dataReceived;

    /// <summary>
    /// internal observer (initialized only when first listener subscribes)
    /// </summary>
    private InternalObserver _observer;

    /// <summary>
    /// unsubscriber for the internal observer
    /// </summary>
    private IDisposable _unsubscriber;
        

    /// <summary>
    /// Constructor. Initialises new instance but will not start connection until the event is subscribed to
    /// </summary>
    /// <param name="options"></param>
    public SignalREvents(SignalRObservableOptions options)
    {
      _observable = new SignalRObservable<T>(options);
    }

    public SignalREvents(string url, string hubName, string eventName, string groupName = null):
      this(
        new SignalRObservableOptions(
          new ConnectionOptions(url),
          hubName,
          eventName,
          string.IsNullOrEmpty(groupName) ? _emptyGroups : new string[] { groupName }
        )
      )
    {

    }
    /// <summary>
    /// Emits events when data is received from server
    /// </summary>
    public event EventHandler<SignalREventArgs<T>> DataReceived
    {
      add
      {
        lock(_observable)
        {
          if (_observer == null)
          {
            _observer = new InternalObserver(this);
            _unsubscriber = _observable.Subscribe(_observer);
          }
          _dataReceived += value;
        }
      }
      remove
      {
        lock (_observable)
        {
          _dataReceived -= value;
          if (_dataReceived == null)
          {
            _unsubscriber?.Dispose();
            _unsubscriber = null;
            _observer = null;
          }
        }
      }
    }

    /// <summary>
    /// Emit notification to subscribers about connection status
    /// </summary>
    public event EventHandler ConnectedChanged
    {
      add
      {
        lock(_observable)
        {
          _observable.ConnectedChanged += value;
        }
      }
      remove
      {
        lock (_observable)
        {
          _observable.ConnectedChanged -= value;
        }
      }
    }

    /// <summary>
    /// Whether connection is active
    /// </summary>
    public bool Connected => _observable.Connected;

    /// <summary>
    /// Connection id
    /// </summary>
    public string Id => _observable.Id;

    /// <summary>
    /// Invokes a method on hub (will fail if disconnected)
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="methodName">method name</param>
    /// <param name="args">args list</param>
    /// <returns>data returned from server</returns>
    public Task<TResult> InvokeAsync<TResult>(string methodName, params object[] args)
    {
      return _observable.InvokeAsync<TResult>(methodName, args);
    }

    /// <summary>
    /// Invokes a method on hub (will fail if disconnected)
    /// </summary>
    /// <param name="methodName">method name</param>
    /// <param name="args">args list</param>
    public async Task InvokeAsync(string methodName, params object[] args)
    {
      await _observable.InvokeAsync(methodName, args);
    }

    /// <summary>
    /// Unsub all subscribers
    /// </summary>
    public void Dispose()
    {
      lock (_observable)
      {
        _dataReceived = null;
        _unsubscriber?.Dispose();
        _unsubscriber = null;
        _observer = null;
      }
    }
  }
}

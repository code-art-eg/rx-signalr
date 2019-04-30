using System;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// Hub callback info
  /// </summary>
  internal class HubCallbackInfo
  {
    /// <summary>
    /// Event name
    /// </summary>
    public string EventName { get; set; }

    /// <summary>
    /// Subscriber (calls proxy.on(action) method). It captured the generic type of the event from the caller
    /// </summary>
    public Func<IDisposable> Subscriber { get; set; }

    /// <summary>
    /// Id (used to unsubscribe)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// An object used to unscriber from proxy)
    /// </summary>
    public IDisposable Unsubscriber { get; set; }
  }
}

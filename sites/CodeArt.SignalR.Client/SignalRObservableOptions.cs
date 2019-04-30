using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// SignalR obeservable options
  /// </summary>
  public class SignalRObservableOptions
  {
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="hubName">hub name</param>
    /// <param name="eventName">event name</param>
    /// <param name="groups">groups </param>
    public SignalRObservableOptions(ConnectionOptions connectionOptions, string hubName, string eventName, IEnumerable<string> groups = null)
    {
      if (string.IsNullOrWhiteSpace(hubName))
      {
        throw new ArgumentException("Hub name cannot be empty", nameof(hubName));
      }

      if (string.IsNullOrWhiteSpace(eventName))
      {
        throw new ArgumentException("Event name cannot be empty", nameof(eventName));
      }

      ConnectionOptions = connectionOptions ?? throw new ArgumentNullException(nameof(connectionOptions));
      HubName = hubName;
      EventName = eventName;
      Groups = new ReadOnlyCollection<string>(groups?.ToList() ?? new List<string>());
    }

    public ConnectionOptions ConnectionOptions { get; }

    /// <summary>
    /// hub name
    /// </summary>
    public string HubName { get; }

    /// <summary>
    ///  event name
    /// </summary>
    public string EventName { get; }

    /// <summary>
    /// groups
    /// </summary>
    public ReadOnlyCollection<string> Groups { get; }
  }
}

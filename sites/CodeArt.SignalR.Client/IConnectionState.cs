using System;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// Interface for object that maintain connection state
  /// </summary>
  public interface IConnectionState
  {
    /// <summary>
    /// Whether the connection is in connected state
    /// </summary>
    bool Connected { get; }

    /// <summary>
    /// Event to notify subscribers of connected state changes
    /// </summary>
    event EventHandler ConnectedChanged;

    /// <summary>
    /// Connection Id
    /// </summary>
    string Id { get; }
  }
}

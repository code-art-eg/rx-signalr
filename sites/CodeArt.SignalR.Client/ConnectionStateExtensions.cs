using System;
using System.Threading.Tasks;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// Extension methods for objects implementing IConnectionState
  /// </summary>
  public static class ConnectionStateExtensions
  {
    /// <summary>
    /// Wait for object to be in desired state (true for connected, false for disconnected)
    /// </summary>
    /// <param name="connectionState">connection state object</param>
    /// <param name="status">the desired state</param>
    /// <returns>A task that completes when object reaches desired state</returns>
    public static async Task WaitForStatus(this IConnectionState connectionState, bool status, int timout = 30000)
    {
      var tcs = new TaskCompletionSource<bool>();
      void handler(object o, EventArgs a)
      {
        if (connectionState.Connected == status)
        {
          tcs.TrySetResult(true);
        }
        connectionState.ConnectedChanged -= handler;
      }

      connectionState.ConnectedChanged += handler;
      if (connectionState.Connected == status)
      {
        connectionState.ConnectedChanged -= handler;
        tcs.TrySetResult(true);
        await tcs.Task;
      }
      var delay = Task.Delay(30000);
      var task = await Task.WhenAny(delay, tcs.Task);
      if (task == delay)
      {
        connectionState.ConnectedChanged -= handler;
        if (connectionState.Connected == status)
        {
          tcs.TrySetResult(true);
        }
        else
        {
          tcs.TrySetException(new TimeoutException($"Waiting for connection to change state to {status} timed out after {timout} ms"));
        }
      }
      await tcs.Task;
    }

    /// <summary>
    /// Wait for object to be connected
    /// </summary>
    /// <param name="connectionState">connection state object</param>
    /// <returns>A task that completes when object reaches connected state</returns>
    public static Task WaitForConnected(this IConnectionState state, int timeout = 3000) => state.WaitForStatus(true, timeout);

    /// <summary>
    /// Wait for object to be disconnected
    /// </summary>
    /// <param name="connectionState">connection state object</param>
    /// <returns>A task that completes when object reaches disconnected state</returns>
    public static Task WaitForDisconnected(this IConnectionState state, int timeout = 3000) => state.WaitForStatus(false, timeout);
  }
}

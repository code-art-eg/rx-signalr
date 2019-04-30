using System.Threading.Tasks;

namespace CodeArt.SignalR.Client
{
  public interface IHubWrapper
  {
    /// <summary>
    /// Invokes a method on hub (will fail if disconnected)
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="methodName">method name</param>
    /// <param name="args">args list</param>
    /// <returns>data returned from server</returns>
    Task<TResult> InvokeAsync<TResult>(string methodName, params object[] args);

    /// <summary>
    /// Invokes a method on hub (will fail if disconnected)
    /// </summary>
    /// <param name="methodName">method name</param>
    /// <param name="args">args list</param>
    Task InvokeAsync(string methodName, params object[] args);
  }
}

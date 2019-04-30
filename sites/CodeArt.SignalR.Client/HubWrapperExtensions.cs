namespace CodeArt.SignalR.Client
{
  public static class HubWrapperExtensions
  {
    public static void Invoke(this IHubWrapper hubWrapper, string methodName, params object[] args)
    {
      hubWrapper.InvokeAsync(methodName, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public static T Invoke<T>(this IHubWrapper hubWrapper, string methodName, params object[] args)
    {
      return hubWrapper.InvokeAsync<T>(methodName, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }
  }
}

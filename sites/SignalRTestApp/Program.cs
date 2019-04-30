using System.Diagnostics;

namespace SignalRTestApp
{
  internal static class Program
  {
    public static void Main(string[] args)
    {
      var task =
        args.Length > 0 && int.TryParse(args[0], out var port) ?
        App.Start(port) : App.Start();
      task.Wait();
    }
  }
}

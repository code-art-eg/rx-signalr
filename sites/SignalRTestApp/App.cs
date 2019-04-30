using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SignalRTestApp
{
  public class App : IDisposable
  {
    private readonly IDisposable _webApp;
    private readonly TaskCompletionSource<bool> _appCompletion = new TaskCompletionSource<bool>();
    private static readonly object _lock = new object();
    private static App _instance = null;

    public static Task Start(int port = 0)
    {
      lock (_lock)
      {
        if (_instance == null)
        {
          _instance = new App(port);
        }
        return _instance._appCompletion.Task;
      }
    }

    public static void Stop()
    {
      lock (_lock)
      {
        if (_instance != null)
        {
          _instance.Dispose();
        }
      }
    }

    private App(int port)
    {
      if (port == 0)
      {
        port = FindFreePort();
      }
      _webApp = WebApp.Start($"http://localhost:{port}/");
      Console.WriteLine(JsonConvert.SerializeObject(new { port }));
    }

    private static int FindFreePort()
    {
      var listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      try
      {
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return port;
      }
      finally
      {
        listener.Stop();
      }
    }

    public void Dispose()
    {
      _webApp.Dispose();
      _appCompletion.SetResult(true);
      _instance = null;
    }
  }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace CodeArt.SignalR.Client.Tests
{
  public abstract class BaseTest
  {
    private const string AppName = "SignalRTestApp";
    private static readonly string _dir = Path.GetDirectoryName(typeof(BaseTest).Assembly.Location);
    private static readonly string _execPath = Path.Combine(_dir, "..", "..", "..", "..", AppName, "bin", "Debug", "net471", AppName + ".exe");

    private Process _process;
    private int _port;

    [TestInitialize]
    public void InitTest()
    {
      StartProcess();
    }

    [TestCleanup]
    public void CleanupTest()
    {
      KillProcess();
      _port = 0;
    }

    protected void StartProcess()
    {
      var info = new ProcessStartInfo
      {
        FileName = Path.Combine(_execPath),
        RedirectStandardOutput = true,
        Arguments = _port == 0 ? null : _port.ToString(CultureInfo.InvariantCulture),
        UseShellExecute = false,
      };
      _process = Process.Start(info);
      var json = _process.StandardOutput.ReadLine();
      _port = JsonConvert.DeserializeObject<ProcessPortInfo>(json).Port;
    }

    protected void KillProcess()
    {
      _process?.Kill();
      _process?.Dispose();
    }

    protected SignalRObservableOptions CreateOptions()
    {
      return new SignalRObservableOptions
      (
        new ConnectionOptions($"http://localhost:{_port}"),
        "EchoHub",
        "notifyMessage",
        new List<string>() { "g1" }
      );
    }
  }
}

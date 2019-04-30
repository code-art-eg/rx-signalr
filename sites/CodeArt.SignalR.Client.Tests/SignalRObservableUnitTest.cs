using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeArt.SignalR.Client.Tests
{
  [TestClass]
  public class SignalRObservableUnitTest : BaseTest
  {
    [TestMethod]
    public async Task ConnectsAndDisconnects()
    {
      var opt = CreateOptions();
      var t = new SignalRObservable<EchoEvent>(opt);
      var observer = new Observer<EchoEvent>();
      using (t.Subscribe(observer))
      {
        await ExpectConnected(t);
      }
      await ExpectDisconnected(t);
    }

    [TestMethod]
    public async Task SendsAndReceicesMessages()
    {
      var opt = CreateOptions();
      var t = new SignalRObservable<EchoEvent>(opt);
      var observer = new Observer<EchoEvent>();
      using (t.Subscribe(observer))
      {
        await ExpectMessage(t);
        await ExpectMessage(t);
        await ExpectConnected(t);
        Assert.AreEqual(2, observer.Events.Count, "Should have received 2 messages");
      }
      await ExpectDisconnected(t);
    }

    [TestMethod]
    public async Task ConnectsIfServerInitiallyDown()
    {
      KillProcess();
      var opt = CreateOptions();
      var t = new SignalRObservable<EchoEvent>(opt);
      var observer = new Observer<EchoEvent>();
      using (t.Subscribe(observer))
      {
        await ExpectDisconnected(t);
        StartProcess();
        await Task.Delay(1000);
        await ExpectMessage(t);
        Assert.AreEqual(1, observer.Events.Count, "Should have received 1 messages");
      }
      await ExpectDisconnected(t);
    }

    [TestMethod]
    public async Task ReconnectsAfterLosingConnection()
    {
      var opt = CreateOptions();
      var t = new SignalRObservable<EchoEvent>(opt);
      var observer = new Observer<EchoEvent>();
      using (t.Subscribe(observer))
      {
        await ExpectMessage(t);
        await Task.Delay(1000);
        KillProcess();
        await ExpectDisconnected(t);

        StartProcess();
        await ExpectMessage(t);
        Assert.AreEqual(2, observer.Events.Count, "Should have received 2 messages");
      }
      await ExpectDisconnected(t);
    }

    private static async Task ExpectMessage(SignalRObservable<EchoEvent> t)
    {
      await ExpectConnected(t);
      var obs = new Observer<EchoEvent>();
      using (t.Subscribe(obs))
      {
        var msg = Guid.NewGuid().ToString();
        await t.InvokeAsync("send", "g1", 1, msg);
        await Task.Delay(1100);
        Assert.AreEqual(1, obs.Events.Count, "Should have received 1 message");
        Assert.IsNotNull(obs.Events[0], "Should have received 1 message");
        Assert.AreEqual("g1", obs.Events[0].Group, "Group should match");
        Assert.AreEqual(msg, obs.Events[0].Message, "Group should match");
      }
    }

    private static async Task ExpectConnected(SignalRObservable<EchoEvent> t)
    {
      await t.WaitForConnected();
      Assert.IsTrue(t.Connected, "SignalR should be connected.");
    }

    private static async Task ExpectDisconnected(SignalRObservable<EchoEvent> t)
    {
      await t.WaitForDisconnected();
      Assert.IsFalse(t.Connected, "SignalR should be disconnected.");
    }
  }
}

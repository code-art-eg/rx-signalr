using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeArt.SignalR.Client.Tests
{
  [TestClass]
  public class SignalREventsUnitTest : BaseTest
  {
    [TestMethod]
    public async Task ConnectsAndDisconnects()
    {
      var opt = CreateOptions();
      var t = new SignalREvents<EchoEvent>(opt);
      EventHandler<SignalREventArgs<EchoEvent>> handler = (e, v) =>
      {

      };
      t.DataReceived += handler;
      try
      {
        await ExpectConnected(t);
      }
      finally
      {
        t.DataReceived -= handler;
      }
      await ExpectDisconnected(t);
    }

    [TestMethod]
    public async Task UnsubcalledTwice()
    {
      var opt = CreateOptions();
      var t = new SignalREvents<EchoEvent>(opt);
      EventHandler<SignalREventArgs<EchoEvent>> handler = (e, v) =>
      {

      };
      t.DataReceived += handler;
      try
      {
        await ExpectConnected(t);
      }
      finally
      {
        t.DataReceived -= handler;
      }
      t.DataReceived -= handler;
      await ExpectDisconnected(t);
    }

    [TestMethod]
    public async Task SendsAndReceicesMessages()
    {
      var opt = CreateOptions();
      var t = new SignalREvents<EchoEvent>(opt);
      int count = 0;
      EventHandler<SignalREventArgs<EchoEvent>> handler = (e, v) =>
      {
        count++;
      };
      t.DataReceived += handler;
      try
      {
        await ExpectMessage(t);
        await ExpectMessage(t);
        await ExpectConnected(t);
      }
      finally
      {
        t.DataReceived -= handler;
      }
      Assert.AreEqual(2, count, "Should have received 2 messages");
      await ExpectDisconnected(t);
    }

    [TestMethod]
    public async Task ConnectsIfServerInitiallyDown()
    {
      KillProcess();
      var opt = CreateOptions();
      var t = new SignalREvents<EchoEvent>(opt);
      int count = 0;
      EventHandler<SignalREventArgs<EchoEvent>> handler = (e, v) =>
      {
        count++;
      };
      t.DataReceived += handler;
      try
      {
        await ExpectDisconnected(t);
        StartProcess();
        await Task.Delay(1000);
        await ExpectMessage(t);
        Assert.AreEqual(1, count, "Should have received 1 messages");
      }
      finally
      {
        t.DataReceived -= handler;
      }
      await ExpectDisconnected(t);
    }

    [TestMethod]
    public async Task ReconnectsAfterLosingConnection()
    {
      var opt = CreateOptions();
      var t = new SignalREvents<EchoEvent>(opt);
      int count = 0;
      EventHandler<SignalREventArgs<EchoEvent>> handler = (e, v) =>
      {
        count++;
      };
      t.DataReceived += handler;
      try
      {
        await ExpectMessage(t);
        await Task.Delay(1000);
        KillProcess();
        await ExpectDisconnected(t);

        StartProcess();
        await ExpectMessage(t);
        Assert.AreEqual(2, count, "Should have received 2 messages");
      }
      finally
      {
        t.DataReceived -= handler;
      }
      await ExpectDisconnected(t);
    }

    private static async Task ExpectMessage(SignalREvents<EchoEvent> t)
    {
      await ExpectConnected(t);
      EchoEvent evt = null;
      var count = 0;
      EventHandler<SignalREventArgs<EchoEvent>> handler = (o, e) =>
      {
        evt = e.Data;
        count++;
      };
      t.DataReceived += handler;
      try
      {
        var msg = Guid.NewGuid().ToString();
        await t.InvokeAsync("send", "g1", 1, msg);
        await Task.Delay(1100);
        Assert.AreEqual(1, count, "Should have received 1 message");
        Assert.IsNotNull(evt, "Should have received 1 message");
        Assert.AreEqual("g1", evt.Group, "Group should match");
        Assert.AreEqual(msg, evt.Message, "Group should match");
      }
      finally
      {
        t.DataReceived -= handler;
      }
    }

    private static async Task ExpectConnected(SignalREvents<EchoEvent> t)
    {
      await t.WaitForConnected();
      Assert.IsTrue(t.Connected, "SignalR should be connected.");
    }

    private static async Task ExpectDisconnected(SignalREvents<EchoEvent> t)
    {
      await t.WaitForDisconnected();
      Assert.IsFalse(t.Connected, "SignalR should be disconnected.");
    }
  }
}

using Microsoft.AspNet.SignalR;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SignalRTestApp
{
  public class EchoHub : Hub
  {
    public Task JoinGroup(string groupName)
    {
      return Groups.Add(Context.ConnectionId, groupName);
    }

    public Task LeaveGroup(string groupName)
    {
      return Groups.Remove(Context.ConnectionId, groupName);
    }

    public Task Send(string group, int delay, string message)
    {
      Timer timer = null;
      timer = new Timer((s) =>
      {
        GlobalHost.ConnectionManager.GetHubContext<EchoHub>().Clients.Group(group).notifyMessage(
          new
          {
            group,
            message,
          }
        );
        timer.Dispose();
      }, null, 1000 * delay, 0);
      return Task.CompletedTask;
    }

    public Task ShutDown()
    {
      Timer timer = null;
      timer = new Timer((s) =>
      {
        App.Stop();
        timer.Dispose();
      }, null, 1000, 0);
      
      return Task.CompletedTask;
    }
  }
}

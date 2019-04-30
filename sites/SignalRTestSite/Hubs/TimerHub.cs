using Microsoft.AspNet.SignalR;
using SignalRTestSite.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SignalRTestSite.Hubs
{
  public class TimerHub : Hub
  {
    private static readonly Dictionary<string, Timer> _initializedGroups = new Dictionary<string, Timer>();

    public Task JoinGroup(string name)
    {
      Initialize(name);
      return Groups.Add(Context.ConnectionId, name);
    }

    public Task LeaveGroup(string name)
    {
      return Groups.Remove(Context.ConnectionId, name);
    }

    private static void Initialize(string name)
    {
      lock(_initializedGroups)
      {
        if (_initializedGroups.ContainsKey(name))
        {
          return;
        }
        var timer = new Timer((t) =>
        {
          GlobalHost.ConnectionManager.GetHubContext<TimerHub>().Clients.Group(name).notifyTimer(new TimerEvent
          {
            GroupName = name,
            Time = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
          }, name, 5000, 5000);
        });
        timer.Change(5000, 5000);
        _initializedGroups.Add(name, timer);
      }
    }
  }
}

using System;
using System.Threading.Tasks;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// Group information
  /// </summary>
  internal class GroupInfo : RefCountedObject<string>
  {
    /// <summary>
    /// underlying hub
    /// </summary>
    private readonly HubInfo _hub;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="hub">hub</param>
    /// <param name="key">group name</param>
    public GroupInfo(HubInfo hub, string key) : base(key)
    {
      _hub = hub;
    }

    /// <summary>
    /// call join group if connected, and whenever the state changes to connected
    /// </summary>
    protected override void OnStart()
    {
      JoinGroupIfConnected();
      _hub.ConnectedChanged += ConnectedChangeHandler;
    }

    /// <summary>
    /// On stop leave group
    /// </summary>
    /// <returns></returns>
    protected override async Task OnStop()
    {
      // stop calling join group when connected
      _hub.ConnectedChanged -= ConnectedChangeHandler;
      if (_hub.Connected)
      {
        try
        {
          // Call leave group
          await _hub.InvokeAsync("leaveGroup", Key);
        }
        catch
        {

        }
      }
    }

    /// <summary>
    /// Join group if connected
    /// </summary>
    private async void JoinGroupIfConnected()
    {
      if (_hub.Connected)
      {
        await Task.Delay(100);
        await _hub.InvokeAsync("joinGroup", Key);
      }
    }

    /// <summary>
    /// event handler that rejoin with reconnected
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    private void ConnectedChangeHandler(object sender, EventArgs eventArgs)
    {
      JoinGroupIfConnected();
    }
  }
}

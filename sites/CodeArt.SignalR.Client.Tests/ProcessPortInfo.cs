using Newtonsoft.Json;

namespace CodeArt.SignalR.Client.Tests
{
  public class ProcessPortInfo
  {
    [JsonProperty("port")]
    public int Port { get; set; }
  }
}

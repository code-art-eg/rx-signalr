using Newtonsoft.Json;

namespace CodeArt.SignalR.Client.Tests
{
  public class EchoEvent
  {
    [JsonProperty("group")]
    public string Group { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
  }
}

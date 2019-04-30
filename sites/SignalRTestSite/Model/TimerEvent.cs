using System.Runtime.Serialization;

namespace SignalRTestSite.Model
{
  [DataContract]
  public class TimerEvent
  {
    [DataMember(Name = "group")]
    public string GroupName { get; set; }

    [DataMember(Name = "time")]
    public int Time { get; set; }
  }
}

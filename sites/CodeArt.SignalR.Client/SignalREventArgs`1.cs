using System;

namespace CodeArt.SignalR.Client
{
  public class SignalREventArgs<T> : EventArgs
  {
    public SignalREventArgs(T data)
    {
      Data = data;
    }

    public T Data { get; }
  }
}

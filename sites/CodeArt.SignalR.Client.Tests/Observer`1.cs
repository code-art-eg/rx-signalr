using System;
using System.Collections.Generic;

namespace CodeArt.SignalR.Client.Tests
{
  public class Observer<T> : IObserver<T>
  {
    public List<T> Events = new List<T>();
    public bool Completed { get; private set; }
    public Exception Error { get; private set; }

    public void OnCompleted() => Completed = true;
    public void OnError(Exception error) => Error = error;
    public void OnNext(T value) => Events.Add(value);
  }
}

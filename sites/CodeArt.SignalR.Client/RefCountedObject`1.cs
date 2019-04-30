using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// An object that keeps track of how many objects refer to it
  /// </summary>
  /// <typeparam name="T"></typeparam>
  internal abstract class RefCountedObject<T> where T: class
  {
    /// <summary>
    /// task completion source for when the object is no longer needed
    /// </summary>
    private readonly TaskCompletionSource<bool> _completedSource = new TaskCompletionSource<bool>();
    private int _refCount = 0;

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="key">key to distinguish object</param>
    protected RefCountedObject(T key)
    {
      Key = key ?? throw new ArgumentNullException(nameof(key));
    }

    /// <summary>
    /// Whether the object is started
    /// </summary>
    public bool Started { get; private set; }

    /// <summary>
    /// Whether the object is completed (and is no longer needed)
    /// </summary>
    public bool Completed => _completedSource.Task.IsCompleted;


    /// <summary>
    /// completed task (a task that completes whent the object is no longer needed)
    /// </summary>
    public Task CompletedTask => _completedSource.Task;

    /// <summary>
    /// Exception that occured (if any)
    /// </summary>
    public Exception Exception { get; private set; }

    /// <summary>
    /// Oject key
    /// </summary>
    public T Key { get; }

    /// <summary>
    /// Add a reference to the object 
    /// </summary>
    public void AddRef()
    {
      lock (_completedSource)
      {
        if (Completed)
        {
          return;
        }
        if (Interlocked.Increment(ref _refCount) == 1)
        {
          Started = true;
          OnStart();
        }
      }
    }

    /// <summary>
    /// Release the object
    /// </summary>
    /// <returns></returns>
    public Task Stop()
    {
      lock (_completedSource)
      {
        if (Completed)
        {
          return CompletedTask;
        }
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
          _completedSource.TrySetResult(true);
          return OnStop();
        }
        return CompletedTask;
      }
    }

    /// <summary>
    /// Called when an error occurs on object
    /// </summary>
    /// <param name="ex"></param>
    protected void OnError(Exception ex)
    {
      lock(_completedSource)
      {
        if (!Completed)
        {
          Exception = ex;
          _completedSource.TrySetResult(true);
        }
      }
    }

    /// <summary>
    /// On stop called when object ref count goes from 1 to 0
    /// </summary>
    /// <returns></returns>
    protected abstract Task OnStop();

    /// <summary>
    /// On start called when object ref count goes from 0 to 1
    /// </summary>
    protected abstract void OnStart();
  }
}

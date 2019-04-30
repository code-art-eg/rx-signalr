using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// A thread safe collection that stores <see cref="RefCountedObject{T}"/>
  /// </summary>
  /// <typeparam name="TKey"></typeparam>
  /// <typeparam name="TValue"></typeparam>
  internal class RefCountedCollection<TKey, TValue>
    where TValue : RefCountedObject<TKey>
    where TKey: class
  {

    public static readonly Task CompletedTask = Task.FromResult(true);

    /// <summary>
    /// internal collection (and lock)
    /// </summary>
    private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

    /// <summary>
    /// factory to create new object
    /// </summary>
    private readonly Func<TKey, TValue> _factory;

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="factory">factory to create new objects</param>
    public RefCountedCollection(Func<TKey, TValue> factory)
    {
      _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Get object by key
    /// </summary>
    /// <param name="key">key to look for</param>
    /// <returns>Object that matches that key or a new object is returned</returns>
    public TValue GetByKey(TKey key)
    {
      lock (_dictionary)
      {
        _dictionary.TryGetValue(key, out var item);
        if (item != null)
        {
          item.AddRef();
          return item;
        }
        item = _factory(key);
        item.AddRef();
        _dictionary.Add(key, item);
        item.CompletedTask.ContinueWith((t) =>
        {
          lock (_dictionary)
          {
            _dictionary.Remove(key);
          }
        }, TaskScheduler.Default);
        return item;
      }
    }

    /// <summary>
    /// Call stop for all items in collection
    /// </summary>
    /// <returns></returns>
    public Task StopAll()
    {
      List<TValue> items;
      lock (_dictionary)
      {
        items = _dictionary.Values.ToList();
      }
      return Task.WhenAll(items.Select(it => ForceStopItem(it)));
    }

    /// <summary>
    /// Stop an item by key
    /// </summary>
    /// <param name="key">key</param>
    /// <returns></returns>
    public Task StopItemByKey(TKey key)
    {
      TValue item;
      lock(_dictionary)
      {
        _dictionary.TryGetValue(key, out item);
      }
      if (item == null)
      {
        return CompletedTask;
      }
      return item.Stop();
    }

    /// <summary>
    /// Call stop until state is complete
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private async Task ForceStopItem(TValue item)
    {
      while (!item.Completed)
      {
        await item.Stop();
      }
    }
  }
}

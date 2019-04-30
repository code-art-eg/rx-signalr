using System;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// hub options
  /// </summary>
  internal sealed class HubOptions : IEquatable<HubOptions>
  {
    /// <summary>
    /// hub options
    /// </summary>
    /// <param name="name">hub name</param>
    /// <param name="connectionOptions">connection options</param>
    public HubOptions(string name, ConnectionOptions connectionOptions)
    {
      if (string.IsNullOrWhiteSpace(name))
      {
        throw new ArgumentException("Hub name cannot be empty", nameof(name));
      }

      Name = name;
      ConnectionOptions = connectionOptions ?? throw new ArgumentNullException(nameof(connectionOptions));
    }

    /// <summary>
    /// hub name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Connection options
    /// </summary>
    public ConnectionOptions ConnectionOptions { get; }

    /// <summary>
    /// compare with other object for equality
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(HubOptions other)
    {
      if (other == null)
      {
        return false;
      }
      return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) && ConnectionOptions.Equals(other.ConnectionOptions);
    }

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is HubOptions other && this.Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
      unchecked
      {
        int hash = 17;
        hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Name);
        hash = hash * 31 + ConnectionOptions.GetHashCode();
        return hash;
      }
    }
  }
}

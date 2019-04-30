using System;

namespace CodeArt.SignalR.Client
{
  /// <summary>
  /// SignalR connection options
  /// </summary>
  public sealed class ConnectionOptions : IEquatable<ConnectionOptions>
  {

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="url">server url</param>
    /// <param name="queryString">query string</param>
    /// <param name="useDefaultPath">whether to use default path ('/signalR')</param>
    public ConnectionOptions(string url, string queryString = null, bool useDefaultPath = true)
    {
      if (string.IsNullOrWhiteSpace(url))
      {
        throw new ArgumentException("Url cannot be empty.", nameof(url));
      }

      Url = url;
      QueryString = queryString;
      UseDefaultPath = useDefaultPath;
    }

    /// <summary>
    /// Server url
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Query string
    /// </summary>
    public string QueryString { get; }

    /// <summary>
    /// Whether to use default path
    /// </summary>
    public bool UseDefaultPath { get; }

    /// <summary>
    /// Compare with another connections options for equality
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(ConnectionOptions other)
    {
      if (other == null)
      {
        return false;
      }
      return Url == other.Url && QueryString == other.QueryString && UseDefaultPath == other.UseDefaultPath;
    }


    /// <inheritdoc />
    public override bool Equals(object obj)
    {
      return obj is ConnectionOptions other && this.Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      unchecked
      {
        int hash = 17;
        hash = hash * 31 + Url.GetHashCode();
        hash = hash * 31 + (QueryString?.GetHashCode() ?? 0);
        hash = hash * 31 + UseDefaultPath.GetHashCode();
        return hash;
      }
    }
  }
}

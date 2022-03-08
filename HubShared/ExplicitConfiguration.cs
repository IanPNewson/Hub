using System;

namespace HubShared;
public class ExplicitConfiguration : IHubConfiguration
{
    public string Host { get; }

    public int Port { get; }

    public ExplicitConfiguration(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentOutOfRangeException(nameof(host));

        Host = host;
        Port = port;
    }
}

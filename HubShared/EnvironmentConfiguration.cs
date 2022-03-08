using System;

namespace HubShared;
public class EnvironmentConfiguration : IHubConfiguration
{

    public const string DEFAULTKEY_HOST = "HUB_HOST";
    public const string DEFAULTKEY_PORT = "HUB_PORT";

    public string HostKeyName { get; }

    public string PortKeyName { get; }

    public EnvironmentConfiguration(string hostKeyName = DEFAULTKEY_HOST, string portKeyName = DEFAULTKEY_PORT)
    {
        HostKeyName = hostKeyName;
        PortKeyName = portKeyName;

        var vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
        var hostPort = GetHostPort(vars);

        if (hostPort == null)
            hostPort = GetHostPort(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User));

        if (hostPort == null)
            hostPort = GetHostPort(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User));

        if (hostPort == null)
            throw new InvalidOperationException($"You must either set a host and port as an enviorment variable (using the key names '{this.HostKeyName}' and '{this.PortKeyName}'), or you must explicitly pass those values to the contructor");

        this.Host = hostPort.Value.host;
        this.Port = hostPort.Value.port;
    }

    private (string host, int port)? GetHostPort(System.Collections.IDictionary vars)
    {
        if (vars.Contains(this.HostKeyName) && vars.Contains(this.PortKeyName))
        {
            var host = vars[this.HostKeyName]?.ToString();
            var strPort = vars[this.PortKeyName]?.ToString();
            if (int.TryParse(strPort, out var port) &&
                !string.IsNullOrWhiteSpace(host))
            {
                return (host, port);
            }
        }
        return null;
    }

    public string Host { get; }

    public int Port { get; }
}

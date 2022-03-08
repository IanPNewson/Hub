using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HubShared
{
    public abstract class HubDataReceiver : IDisposable
    {
        private MemoryStream _buffer = new MemoryStream();
        public static readonly byte[] TERMINATOR = new byte[] { (byte)'\n' };
        public const char SEPARATOR = (char)127;

        public IHubConfiguration Configuration { get; }

        protected HubDataReceiver(IHubConfiguration? configuration)
        {
            Configuration = configuration ?? new EnvironmentConfiguration();
        }

        public virtual void Dispose()
        {
            _buffer.Dispose();
        }

        protected void Events_DataReceived(object? sender, DataReceivedEventArgs e)
        {
            foreach (var b in e.Data)
            {
                if (b == '\n')
                {
                    var bytes = _buffer.ToArray();
                    _buffer.Dispose();
                    _buffer = new MemoryStream();
                    ProcessMessage(bytes, e.IpPort);
                }
                else
                {
                    _buffer.WriteByte(b);
                }
            }
        }

        private HubMessage ParseMessage(string line)
        {
            try
            {
                var parts = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                var typeName = parts[0];
                if (!typeName.Contains("."))
                    typeName = $"{typeof(HubMessage).Namespace}.{typeName}";
                Type type = LoadType(typeName);

                if (type == null) return new UnknownMessageTypeMessage(parts);

                var isArrayCtor = true;

                var args = parts.Skip(1).ToArray();

                var constructor = type.GetConstructor(new[] { typeof(string[]) });
                if (constructor == null)
                    constructor = type.GetConstructor(new[] { typeof(IEnumerable<string>) });
                if (constructor == null)
                {
                    constructor = type.GetConstructor(Enumerable.Range(0, args.Length).Select(_ => typeof(string)).ToArray());
                    isArrayCtor = false;
                }

                if (constructor == null) return new UnknownMessageTypeMessage(parts);

                object[] ctorArgs;
                if (isArrayCtor)
                {
                    ctorArgs = new object[] { args };
                }
                else
                {
                    ctorArgs = args;
                }

                var instance = (HubMessage)constructor.Invoke(ctorArgs);
                return instance;
            }
            catch (Exception ex)
            {
                return new ExceptionMessage($"Error parsing message on server: {ex.Message}");
            }
        }

        protected virtual Type LoadType(string typeName)
        {
            return Type.GetType(typeName);
        }

        private void ProcessMessage(byte[] bytes, string sender)
        {
            if (bytes.Length == 0) return;

            var line = System.Text.Encoding.ASCII.GetString(bytes);
            if (string.IsNullOrWhiteSpace(line)) return;

            var message = ParseMessage(line);

            OnMessageReceivedFromHub?.Invoke(this, message);
            OnMessageReceived(message, sender);
        }

        public event EventHandler<HubMessage>? OnMessageReceivedFromHub;

        public abstract void OnMessageReceived(HubMessage message, string sender);

    }
}
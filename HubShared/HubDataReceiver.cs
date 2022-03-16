using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HubShared
{
    public abstract class HubDataReceiver : IDisposable
    {
        private Dictionary<string, MemoryStream> _buffers = new Dictionary<string, MemoryStream>();
        public static readonly char TERMINATOR = '\n';
        public const char SEPARATOR = (char)127;
        private Encoding _encoding = System.Text.Encoding.ASCII;

        protected Encoding Encoding {  get => _encoding; }

        public IHubConfiguration Configuration { get; }

        protected HubDataReceiver(IHubConfiguration? configuration)
        {
            Configuration = configuration ?? new EnvironmentConfiguration();
        }

        public virtual void Dispose()
        {
            foreach (var buffer in _buffers.Select(kvp => kvp.Value))
                buffer.Dispose();
        }

        private MemoryStream GetBuffer(string sender)
        {
            if (_buffers.ContainsKey(sender))
            {
                return _buffers[sender];
            }
            else
            {
                var stream = new MemoryStream();
                _buffers.Add(sender, stream);
                return stream;
            }
        }

        protected void Events_DataReceived(object? sender, DataReceivedEventArgs e)
        {
            var buffer = GetBuffer(e.IpPort);
            foreach (var b in e.Data)
            {
                if (b == '\n')
                {
                    var bytes = buffer.ToArray();
                    buffer.SetLength(0);
                    ProcessMessage(bytes, e.IpPort);
                }
                else
                {
                    buffer.WriteByte(b);
                }
            }
        }

        private HubMessage ParseMessage(string line)
        {
            var parts = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
            var typeName = parts[0];
            try
            {
                if (!typeName.Contains("."))
                    typeName = $"{typeof(HubMessage).Namespace}.{typeName}";
                Type type = null;
                try
                {
                    type = LoadType(typeName);
                }
                catch (TypeLoadException)
                {
                    return new UntypedMessage(parts);
                }

                if (type == null) return new UntypedMessage(parts);

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

                if (constructor == null) return new UntypedMessage(parts);

                object[] ctorArgs;
                if (isArrayCtor)
                {
                    ctorArgs = new object[] { args };
                }
                else
                {
                    ctorArgs = args;
                }

                var instance = constructor.Invoke(ctorArgs);
                if (instance is HubMessage message) return message;
                return new UntypedMessage(parts);
            }
            catch (Exception ex)
            {
                return new ExceptionMessage($"Error parsing message on server: {ex.Message}");
            }
        }

        protected virtual Type LoadType(string typeName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(ass => ass.GetTypes())
                .FirstOrDefault(t => t.FullName == typeName);
        }

        private void ProcessMessage(byte[] bytes, string sender)
        {
            if (bytes.Length == 0) return;

            var line = _encoding.GetString(bytes);
            if (string.IsNullOrWhiteSpace(line)) return;

            var message = ParseMessage(line);

            OnMessageReceivedFromHub?.Invoke(this, message);
            OnMessageReceived(message, sender);
        }

        public event EventHandler<HubMessage>? OnMessageReceivedFromHub;

        public abstract void OnMessageReceived(HubMessage message, string sender);

        protected byte[] SerializeMessage(HubMessage message)
        {
            var line = message.Serialize();
            var bytes = Encoding.GetBytes(line);
            return bytes;
        }

    }
}
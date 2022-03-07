using StfcPipe;
using SuperSimpleTcp;
using System;
using System.IO;
using System.Linq;

namespace StfcHubShared
{
    public abstract class HubDataReceiver : IDisposable
    {
        private MemoryStream _buffer = new MemoryStream();
        public static readonly byte[] TERMINATOR = new byte[] { (byte)'\n' };

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
                    ProcessMessage(e.IpPort);
                    _buffer.Dispose();
                    _buffer = new MemoryStream();
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
                var parts = line.Split(':');
                var typeName = parts[0];
                if (!typeName.Contains("."))
                    typeName = $"{typeof(HubMessage).Namespace}.{typeName}";
                var type = Type.GetType(typeName);

                if (type == null) return new UnknownMessageTypeMessage(parts);

                var args = parts.Skip(1).ToArray();

                var constructor = type.GetConstructor(Enumerable.Range(0, args.Length).Select(_ => typeof(string)).ToArray());

                if (constructor == null) return new UnknownMessageTypeMessage(parts);

                var instance = (HubMessage)constructor.Invoke(args);
                return instance;
            }
            catch (Exception ex)
            {
                return new ExceptionMessage($"Error parsing message on server: {ex.Message}");
            }
        }

        private void ProcessMessage(string sender)
        {
            var bytes = _buffer.ToArray();
            if (bytes.Length == 0) return;

            var line = System.Text.Encoding.ASCII.GetString(bytes);
            if (string.IsNullOrWhiteSpace(line)) return;

            var message = ParseMessage(line);

            OnMessageReceivedFromHub?.Invoke(this, message);
            OnMessageReceived(message);
        }

        public event EventHandler<HubMessage>? OnMessageReceivedFromHub;

        public abstract void OnMessageReceived(HubMessage message);

    }
}
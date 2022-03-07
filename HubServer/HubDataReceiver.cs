using StfcPipe;
using SuperSimpleTcp;

namespace StfcHubServer
{
    public abstract class HubDataReceiver : IDisposable
    {
        private MemoryStream _buffer = new MemoryStream();

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
                }
                else
                {
                    _buffer.WriteByte(b);
                }
            }
        }

        private HubMessage ParseMessage(string line)
        {
            var parts = line.Split(':');
            var typeName = parts[0];
            if (!typeName.Contains("."))
                typeName = $"{typeof(HubMessage).Namespace}.{typeName}";
            var type = Type.GetType(typeName);


            var args = parts.Skip(1).ToArray();

            var constructor = type.GetConstructor(Enumerable.Range(0, args.Length).Select(_ => typeof(string)).ToArray());
            var instance = (HubMessage)constructor.Invoke(args);
            return instance;
        }

        private void ProcessMessage(string sender)
        {
            var bytes = _buffer.ToArray();
            var line = System.Text.Encoding.ASCII.GetString(bytes);
            var message = ParseMessage(line);

            OnMessageReceived(message);
        }

        public abstract void OnMessageReceived(HubMessage message);

    }
}
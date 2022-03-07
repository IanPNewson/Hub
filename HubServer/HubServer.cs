using StfcHubShared;
using StfcPipe;
using SuperSimpleTcp;

namespace StfcHubServer
{

    public class HubServer : HubDataReceiver, IDisposable
    {
        private SimpleTcpServer _server;

        public void Start()
        {
            _server = new SimpleTcpServer("0.0.0.0", StfcHubConstants.PORT);
            _server.Events.DataReceived += Events_DataReceived;
            _server.Start();
        }

        public override void Dispose()
        {
            base.Dispose();
            _server.Dispose();
        }

        public override void OnMessageReceived(HubMessage message)
        {
            foreach (var client in _server.GetClients())
            {
                _server.Send(client, message.Serialize());
                _server.Send(client, TERMINATOR);
            }
        }

        public IEnumerable<string> Clients
        {
            get => _server.GetClients();
        }
    }
}
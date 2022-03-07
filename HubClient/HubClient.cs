using StfcHubShared;
using StfcPipe;
using SuperSimpleTcp;

namespace StfcHubClient
{
    public class HubClient : HubDataReceiver
    {
        private SimpleTcpClient _client;
        private bool _keepRunning = true;

        public HubClient()
        {
            _client = new SimpleTcpClient("127.0.0.1", StfcHubConstants.PORT);
            _client.Events.DataReceived += base.Events_DataReceived;
            _client.Connect();

            new Thread(() =>
            {
                while (_keepRunning)
                {
                    if (!_client.IsConnected)
                    {
                        _client.Connect();
                    }
                    Thread.Sleep(1000);
                }

            })
            {
                Name = "HubCliient reconnect thread"
            }
            .Start();
        }

        ~HubClient()
        {
            _keepRunning = false;
        }

        public event EventHandler<HubMessage>? OnMessageReceivedFromHub;

        public override void OnMessageReceived(HubMessage message)
        {
            OnMessageReceivedFromHub?.Invoke(this, message);
        }

        public void Send(HubMessage message)
        {
            _client.Send(message.Serialize());
            _client.Send(TERMINATOR);
        }
    }
}
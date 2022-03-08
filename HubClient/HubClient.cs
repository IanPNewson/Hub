using HubShared;
using SuperSimpleTcp;
using System.Net.Sockets;

namespace Hub
{
    public class HubClient : HubDataReceiver
    {
        private SimpleTcpClient _client;
        private bool _keepRunning = true;
        private const int CONNECT_TIMEOUT = 10_000;
        private Thread _connectingThread = null;
        private Dictionary<Type, List<Action<HubMessage?>>> _typeHandlers = new Dictionary<Type, List<Action<HubMessage?>>>();

        public bool IsConnected { get => _client.IsConnected; }
        public Type[] MessageTypes { get; }

        public HubClient(params Type[] messageTypes)
        {
            _client = new SimpleTcpClient("127.0.0.1", StfcHubConstants.PORT);
            MessageTypes = messageTypes;

            _client.Logger = str =>
            {
                Console.WriteLine(str);
            };

            _client.Events.DataReceived += Events_DataReceived;
            _client.Events.Disconnected += (sender, args) =>
            {
                if (_client.IsConnected)
                    throw new Exception("Client is lying");
                TryConnecting();
            };
            _client.Events.Connected += (sender, args) =>
            {
                if (messageTypes.Any())
                {
                    this.Send(new HubServerRequestMessageTypes());
                }
                OnConnected?.Invoke(this, this);
            };

            if (messageTypes.Any())
            {
                foreach(var type in messageTypes)
                {
                    if (!type.IsSubclassOf(typeof(HubMessage)))
                        throw new ArgumentException($"{type.FullName} does not derive from {nameof(HubMessage)}");
                }
                AddHandler<HubServerResponseMessageTypes>(CheckForMessageTypes);
            }

        }

        public void AddHandler<T>(Action<T> handler)
            where T : HubMessage
        {
            if (!_typeHandlers.ContainsKey(typeof(T)))
            {
                _typeHandlers.Add(typeof(T), new List<Action<HubMessage?>>());
            }
            _typeHandlers[typeof(T)].Add(message => handler((T)message));
        }

        private void CheckForMessageTypes(HubServerResponseMessageTypes messageTypesResponse)
        {
            var missingTypes = MessageTypes.Where(type => !messageTypesResponse.TypeNames.Contains(type.FullName));

            var assemblies = missingTypes
                .Select(type => type.Assembly)
                .Distinct();

            foreach (var assembly in assemblies)
            {
                this.Send(new HubServerLoadDllRequest(assembly.Location));
            }
        }

        public void Start()
        {
            TryConnecting();
        }

        private void TryConnecting()
        {
            if (_connectingThread?.ThreadState == ThreadState.Running)
                return;

            _connectingThread = new Thread(() =>
            {
                var wasConnected = _client.IsConnected;
                while (_keepRunning && !_client.IsConnected)
                {
                    if (wasConnected && !_client.IsConnected)
                        System.Diagnostics.Debugger.Break();
                    if (!_client.IsConnected)
                    {
                        try
                        {
                            _client.Connect();
                        }
                        catch (SocketException ex) { }
                    }
                    Thread.Sleep(1000);
                }

            })
            {
                Name = "HubCliient reconnect thread"
            };
            _connectingThread.Start();
        }

        ~HubClient()
        {
            _keepRunning = false;
        }

        public event EventHandler<HubMessage>? OnMessageReceivedFromHub;
        public event EventHandler<HubClient>? OnConnected;

        public override void OnMessageReceived(HubMessage message, string sender)
        {
            foreach (var group in _typeHandlers)
            {
                if (group.Key.IsAssignableFrom(message.GetType()))
                {
                    foreach(var handler in group.Value)
                    {
                        handler(message);
                    }
                }
            }
            OnMessageReceivedFromHub?.Invoke(this, message);
        }

        public void Send(HubMessage message)
        {
            _client.Send(message.Serialize());
            _client.Send(TERMINATOR);
        }
    }
}
using HubShared;
using SuperSimpleTcp;
using System.Net.Sockets;

namespace Hub;

public class HubClient : HubDataReceiver
{
    private SimpleTcpClient _client;
    private List<Type> _messageTypes;
    private bool _keepRunning = true;
    private const int CONNECT_TIMEOUT = 10_000;
    private Thread _connectingThread = null;
    private Dictionary<Type, List<Action<HubMessage>>> _typeHandlers = new Dictionary<Type, List<Action<HubMessage>>>();
    private Dictionary<string, List<Action<HubMessage>>> _typeNameHandlers = new Dictionary<string, List<Action<HubMessage>>>();

    public bool IsConnected { get => _client.IsConnected; }
    public IEnumerable<Type> MessageTypes { get => _messageTypes; }

    public HubClient(params Type[] messageTypes) : this(configuration: null, messageTypes) { }

    public HubClient(IHubConfiguration? configuration = null, params Type[] messageTypes) : base(configuration)
    {
        _client = new SimpleTcpClient(this.Configuration.Host, this.Configuration.Port);
        _messageTypes = new List<Type>(messageTypes);

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
            if (MessageTypes.Any())
            {
                //this.Send(new HubServerRequestMessageTypes());
            }
            OnConnected?.Invoke(this, this);

            _client.Logger("Connected");
        };

        foreach (var type in MessageTypes)
        {
            if (!type.IsSubclassOf(typeof(HubMessage)))
                throw new ArgumentException($"{type.FullName} does not derive from {nameof(HubMessage)}");
        }
        //AddHandler<HubServerResponseMessageTypes>(CheckForMessageTypes);
    }

    public void AddHandler(string typeName, Action<HubMessage> handler)
    {
        if (!_typeNameHandlers.ContainsKey(typeName))
        {
            _typeNameHandlers.Add(typeName, new List<Action<HubMessage>>());
        }
        _typeNameHandlers[typeName].Add(handler);
    }

    public void AddHandler<T>(Action<T> handler)
        where T : HubMessage
    {
        if (!_messageTypes.Contains(typeof(T)))
        {
            if (!IsConnected)
            {
                _messageTypes.Add(typeof(T));
            }
            else
            {
                throw new InvalidOperationException($"Cannot add a message handler of type '{typeof(T).FullName}' ass it was not requested before this client was started and therefore the server was not asked to load it");
            }
        }

        if (!_typeHandlers.ContainsKey(typeof(T)))
        {
            _typeHandlers.Add(typeof(T), new List<Action<HubMessage?>>());
        }
        _typeHandlers[typeof(T)].Add(message => handler((T)message));
    }

    private void CheckForMessageTypes(HubServerResponseMessageTypes messageTypesResponse)
    {
        var missingTypes = MessageTypes.Where(type => type.IsAssignableTo(typeof(HubServerMessage)) && !messageTypesResponse.TypeNames.Contains(type.FullName));

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
                    catch (NullReferenceException ex) { }//Bug in SuperSimpleTcp I think
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
                foreach (var handler in group.Value)
                {
                    handler(message);
                }
            }
        }

        foreach (var typeNameHandler in _typeNameHandlers.Where(x => IsTypeName(x.Key, message.TypeName))
            .SelectMany(x => x.Value))
        {
            typeNameHandler(message);
        }

        OnMessageReceivedFromHub?.Invoke(this, message);
    }

    protected bool IsTypeName(string typeName, string test)
    {
        if (!test.Contains(".") && typeName.Contains("."))
            typeName = typeName.Substring(typeName.LastIndexOf(".") + 1);
        if (!typeName.Contains(".") && test.Contains("."))
            test = test.Substring(test.LastIndexOf(".") + 1);

        return typeName == test;
    }

    public void Send(HubMessage message)
    {
        byte[] bytes = SerializeMessage(message);
        _client.Send(bytes);
    }

}

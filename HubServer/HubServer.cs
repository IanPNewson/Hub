using HubShared;
using INHelpers.ExtensionMethods;
using SuperSimpleTcp;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace Hub;

public class HubServer : HubDataReceiver, IDisposable
{
    private SimpleTcpServer _server;
    private List<AssemblyLoadContext> _loadContexts = new List<AssemblyLoadContext>();
    private DirectoryInfo _tempDirectory = new DirectoryInfo(Path.GetTempPath())
        .Subdir(nameof(HubServer))
        .EnsureExists();

    public HubServer(IHubConfiguration? configuration = null) : base(configuration)
    {
    }

    public void Start()
    {
        _server = new SimpleTcpServer("0.0.0.0", this.Configuration.Port);
        _server.Events.DataReceived += Events_DataReceived;
        _server.Events.ClientConnected += (sender, args) =>
        {
            Debug.WriteLine($"Client connected: {args.IpPort}");
        };
        _server.Start();
    }

    public override void Dispose()
    {
        base.Dispose();

        foreach (var client in _server.GetClients())
            _server.DisconnectClient(client);

        _server.Dispose();
    }

    public override void OnMessageReceived(HubMessage message, string sender)
    {
        Db.Log(message, MessageDirection.In);

        if (message is HubServerMessage)
        {
            try
            {
                switch (message)
                {
                    case HubServerLoadDllRequest dllRequest:
                        HandleLoadDllRequest(dllRequest);
                        break;
                    case HubServerRequestMessageTypes dllRequestTypes:
                        HandleMessageTypesRequest(dllRequestTypes, sender);
                        break;
                    default:
                        throw new InvalidOperationException($"This server doesn't know how to handle a server message of type {message.GetType().FullName}");
                }
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                this.Send(sender, new ExceptionMessage(ex.Message));
            }
        }
        else
        {
            BroadcastMessage(message);
        }
    }

    private void HandleMessageTypesRequest(HubServerRequestMessageTypes dllRequestTypes, string sender)
    {
        var types = new List<Type>();

        for(var i = _loadContexts.Count - 1; i >= 0; i--)
        {
            var ctx = _loadContexts[i];
            AddHubMessageTypes(types, ctx.Assemblies);
        }

        AddHubMessageTypes(types, AppDomain.CurrentDomain.GetAssemblies());

        var response = new HubServerResponseMessageTypes(types.Select(t => t.FullName!).ToArray());

        this.Send(sender, response);

    }

    private static void AddHubMessageTypes(List<Type> types, IEnumerable<Assembly> assemblies)
    {
        foreach (var ass in assemblies)
        {
            types.AddRange(ass.GetTypes().Where(t => typeof(HubMessage).IsAssignableFrom(t)));
        }
    }

    private void HandleLoadDllRequest(HubServerLoadDllRequest message)
    {
        var file = new FileInfo(message.DllPath);
        if (!file.Exists)
            throw new FileNotFoundException(message.DllPath);

        var dllTempParentFolder = _tempDirectory.Subdir("ActiveDlls")
            .Subdir(file.FullName.ToSafePath())
            .EnsureExists();


        DirectoryInfo dllTempFolder;
        var index = 0;
        do
        {
            dllTempFolder = dllTempParentFolder.Subdir((index++).ToString(), create:false);
        }
        while (dllTempFolder.Exists);

        dllTempFolder.EnsureExists();

        foreach (var realFile in file.Directory.GetFiles())
        {
            var shadowFile = dllTempFolder.File(realFile.Name);
            realFile.CopyTo(shadowFile.FullName);
        }

        var shadowDll = dllTempFolder.File(file.Name);

        var ctx = new AssemblyLoadContext(message.DllPath, true);
        ctx.Resolving += (sender, args) =>
        {
            var assFile = dllTempFolder.File($"{args.Name}.dll");
            if (!assFile.Exists)
                throw new FileNotFoundException($"Expected to find '{assFile.FullName}' while loading '{file.Name}', but it was not.");
            return ctx.LoadFromAssemblyPath(assFile.FullName);
        };

        ctx.LoadFromAssemblyPath(shadowDll.FullName);

        _loadContexts.Add(ctx);
    }

    protected override Type LoadType(string typeName)
    {
        for (var i = _loadContexts.Count - 1; i >= 0; i--)
        {
            var ctx = _loadContexts[i];
            var type = ctx.Assemblies
                .SelectMany(ass => ass.GetTypes())
                .FirstOrDefault(type => type.FullName == typeName);
            if (type != null)
                return type;
        }
        return base.LoadType(typeName);
    }

    private void BroadcastMessage(HubMessage message)
    {
        foreach (var client in _server.GetClients())
        {
            Send(client, message);
        }
    }

    public void Send(string client, HubMessage message)
    {
        Db.Log(message, MessageDirection.Out);
        _server.Send(client, this.SerializeMessage(message));
    }

    public IEnumerable<string> Clients
    {
        get => _server.GetClients();
    }
}

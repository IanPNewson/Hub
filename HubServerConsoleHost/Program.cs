using Hub;
using HubShared;

static void Write(string line) => Console.WriteLine(line);


HubServer server = null;
do
{
    Write("Starting server");

    if (server != null)
    {
        server.Dispose();
    }
    server = new HubServer();
    server.OnMessageReceivedFromHub += (sender, args) =>
    {
        if (args is HubServerMessage)
        {
            Write($"Received server message from hub: {args}: {args.Serialize()}");
        }
        else
        {
            Write($"Received message from hub: {args}, broadcasting to {server.Clients.Count()} hosts");
        }
    };
    server.Start();

    Write("Server running, press x to exit, anyother key to restart");
}
while (Console.ReadKey().KeyChar != 'x');
Write("Exiting...");

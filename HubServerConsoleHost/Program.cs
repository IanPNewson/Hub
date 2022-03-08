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
            Write($"Received message from hub: {args}, broadcasting to:");
            foreach (var client in server.Clients)
            {
                Write(client);
            }
        }
    };
    server.Start();

    Write("Server running, press x to exit, anyother key to restart");
}
while (Console.ReadKey().KeyChar != 'x');
Write("Exiting...");

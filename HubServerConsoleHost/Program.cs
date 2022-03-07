using StfcHubServer;

static void Write(string line) => Console.WriteLine(line);

new Thread(() =>
{

}).Start();

using (var server = new HubServer())
{
    server.OnMessageReceivedFromHub += (sender, args) =>
    {
        Write($"Received message from hub: {args}, broadcasting to:");
        foreach (var client in server.Clients)
        {
            Write(client);
        }
    };
    server.Start();

    Write("Server running, press x to exit");
    while (Console.ReadKey().KeyChar != 'x') ;
    Write("Exiting...");
}
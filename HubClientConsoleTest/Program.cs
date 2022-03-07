using StfcHubClient;
using StfcPipe;
static void Write(string line) => Console.WriteLine(line);

Thread.Sleep(2000);

using (var client = new HubClient())
{
    client.OnMessageReceivedFromHub += (sender, message) => Console.WriteLine(message.ToString());

    Write("Ready, press x to exit");

    ConsoleKeyInfo key;
    while ((key = Console.ReadKey()).KeyChar != 'x')
    {
        HubMessage message;
        switch (key.KeyChar)
        {
            case '0':
                message = new TrickMessage("fkdfjsdi", "jfifsdio");
                break;
            default:
                message = new HubMessage($"Test message");
                break;
        }
        client.Send(message);
    }
}

public class TrickMessage : HubMessage
{
    public TrickMessage(string ha, string haha) : base(ha, haha) { }
}
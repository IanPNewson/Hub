using HubShared;
using Hub;

static void Write(string line) => Console.WriteLine(line);

Thread.Sleep(2000);

using (var client = new HubClient(typeof(TrickMessage)))
{
    client.OnMessageReceivedFromHub += (sender, message) =>
    {
        switch (message)
        {
            case HubServerResponseMessageTypes messageTypesResponse:
                Write(nameof(HubServerResponseMessageTypes));
                foreach (var typeName in messageTypesResponse.TypeNames)
                    Write(typeName);
                break;
            default:
                Console.WriteLine(message.ToString());
                break;
        }

    };

    client.Start();

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
            case '1':
                message = new HubServerLoadDllRequest(@"C:\Users\Ian\source\repos\ReenableWifi\ReenableWifi\bin\Debug\net5.0\ReenableWifi.dll");
                break;
            case '2':
                message = new HubServerRequestMessageTypes();
                break;
            case '3':
                message = new ThrowExceptionServerMessage();
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
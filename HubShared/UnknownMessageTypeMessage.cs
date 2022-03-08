namespace HubShared
{
    public class UnknownMessageTypeMessage : HubMessage
    {
        public UnknownMessageTypeMessage(params string[] args) : base(args)
        {
        }
    }
}

namespace HubShared
{
    public abstract class HubServerMessage : HubMessage
    {
        protected HubServerMessage(params string[] args) : base(args) { }
    }

}

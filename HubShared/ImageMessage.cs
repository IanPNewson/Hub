namespace HubShared
{
    public abstract class ImageMessage : HubMessage
    {
        public ImageMessage(string imageName) : base(imageName) { }
    }
}

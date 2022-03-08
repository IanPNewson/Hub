namespace HubShared
{
    public class ImageActionMessage : HubMessage
    {
        public ImageActionMessage(string imageName, string action) : base(imageName, action)
        {
        }
    }
}

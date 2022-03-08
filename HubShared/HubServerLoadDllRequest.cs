namespace HubShared
{
    public class HubServerLoadDllRequest : HubServerMessage
    {
        public HubServerLoadDllRequest(string dllPath) : base(dllPath)
        {
            DllPath = dllPath;
        }

        public string DllPath { get; }
    }

}

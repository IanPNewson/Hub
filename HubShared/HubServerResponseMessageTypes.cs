using System;

namespace HubShared
{
    public class HubServerResponseMessageTypes : HubServerMessage
    {
        private string[] _typeNames;

        public HubServerResponseMessageTypes(params string[] typeNames) : base(typeNames)
        {
            _typeNames = typeNames;
        }

        public string[] TypeNames { get => _typeNames; }
    }

}

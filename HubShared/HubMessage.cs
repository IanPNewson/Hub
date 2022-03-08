using System;
using System.Linq;

namespace HubShared
{
    public class HubMessage
    {

        private string[] _args;

        protected string[] Args
        {
            get => _args;
        }

        public HubMessage(string arg1) : this(new[] { arg1 }) { }
        public HubMessage(string arg1, string arg2) : this(new[] { arg1, arg2 }) { }
        public HubMessage(string arg1, string arg2, string arg3) : this(new[] { arg1, arg2, arg3 }) { }

        protected HubMessage(params string[] args)
        {
            _args = args;
        }

        public string Serialize()
        {
            if (_args.Any(x => x.Contains(HubDataReceiver.SEPARATOR)))
                throw new ArgumentException($"Arguments may not contain the separator character '{HubDataReceiver.SEPARATOR}'");
            return $"{GetType().Name}{HubDataReceiver.SEPARATOR}{string.Join(HubDataReceiver.SEPARATOR, _args)}";
        }

        public override string ToString()
        {
            return Serialize();
        }
    }

}

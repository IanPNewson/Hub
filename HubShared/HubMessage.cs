namespace StfcPipe
{
    public class HubMessage
    {

        private string[] _args;

        public HubMessage(string arg1) : this(new[] { arg1 }) { }
        public HubMessage(string arg1, string arg2) : this(new[] { arg1, arg2 }) { }
        public HubMessage(string arg1, string arg2, string arg3) : this(new[] { arg1, arg2, arg3 }) { }

        protected HubMessage(params string[] args)
        {
            _args = args;
        }

        public string Serialize()
        {
            return this.GetType().Name + ":" + string.Join(":", _args);
        }

        public override string ToString()
        {
            return this.Serialize();
        }
    }
}

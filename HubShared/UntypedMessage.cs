using System.Collections.Generic;
using System.Linq;

namespace HubShared;

public class UntypedMessage : HubMessage
{
    private string[] _args;

    public UntypedMessage(params string[] args) : base(args)
    {
        _args = args;
    }

    public override string TypeName { get => _args.First(); }

    public override string[] Args => _args.Skip(1).ToArray();

}


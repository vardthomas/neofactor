using Neo.Common;
using Neo.Common.Primitives;

namespace Neo.Core.SmartContract
{
    public interface ICode
    {
        byte[] Script { get; }
        ContractParameterType[] ParameterList { get; }
        ContractParameterType ReturnType { get; }
        UInt160 ScriptHash { get; }
    }
}

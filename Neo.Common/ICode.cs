using Neo.Common.Primitives;

namespace Neo.Common
{
    public interface ICode
    {
        byte[] Script { get; }
        ContractParameterType[] ParameterList { get; }
        ContractParameterType ReturnType { get; }
        UInt160 ScriptHash { get; }
    }
}

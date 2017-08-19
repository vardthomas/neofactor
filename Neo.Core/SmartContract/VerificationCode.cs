using Neo.Common;
using Neo.Common.Primitives;

namespace Neo.Core.SmartContract
{
    public class VerificationCode : ICode
    {
        public byte[] Script { get; set; }
        public ContractParameterType[] ParameterList { get; set; }

        ContractParameterType ICode.ReturnType => ContractParameterType.Boolean;

        private UInt160 _scriptHash;
        public UInt160 ScriptHash
        {
            get
            {
                if (_scriptHash == null)
                {
                    _scriptHash = Script.ToScriptHash();
                }
                return _scriptHash;
            }
        }
    }
}

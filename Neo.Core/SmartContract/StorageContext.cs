using Neo.Common;
using Neo.Common.Primitives;

namespace Neo.Core.SmartContract
{
    internal class StorageContext : IInteropInterface
    {
        public UInt160 ScriptHash;

        public byte[] ToArray()
        {
            return ScriptHash.ToArray();
        }
    }
}

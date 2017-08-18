using Neo.Common;
using Neo.Core.Wallets;

namespace Neo.Core.Core
{
    public class SpentCoin
    {
        public TransactionOutput Output;
        public uint StartHeight;
        public uint EndHeight;

        public Fixed8 Value => Output.Value;
    }
}

﻿using Neo.Common;
using Neo.Common.IO.Caching;
using Neo.Common.Primitives;
using Neo.Core;
using Neo.Core.State;
using Neo.VM;

namespace Neo.SmartContract
{
    internal class CachedScriptTable : IScriptTable
    {
        private DataCache<UInt160, ContractState> contracts;

        public CachedScriptTable(DataCache<UInt160, ContractState> contracts)
        {
            this.contracts = contracts;
        }

        byte[] IScriptTable.GetScript(byte[] script_hash)
        {
            return contracts[new UInt160(script_hash)].Code.Script;
        }
    }
}

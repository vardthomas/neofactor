﻿using System;
using System.Collections.Generic;
using System.IO;
using Neo.Common;
using Neo.Common.IO;
using Neo.Common.IO.Json;
using Neo.Common.Primitives;

namespace Neo.Core.Core
{
    public class InvocationTransaction : Transaction
    {
        public byte[] Script;
        public Fixed8 Gas;

        public override int Size => base.Size + Script.GetVarSize();

        public override Fixed8 SystemFee => Gas;

        public InvocationTransaction()
            : base(TransactionType.InvocationTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version > 1) throw new FormatException();
            Script = reader.ReadVarBytes(65536);
            if (Script.Length == 0) throw new FormatException();
            if (Version >= 1)
            {
                Gas = reader.ReadSerializable<Fixed8>();
                if (Gas < Fixed8.Zero) throw new FormatException();
            }
            else
            {
                Gas = Fixed8.Zero;
            }
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
            if (Version >= 1)
                writer.Write(Gas);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["script"] = Script.ToHexString();
            json["gas"] = Gas.ToString();
            return json;
        }

        public override bool Verify(IEnumerable<Transaction> mempool)
        {
            if (Gas.GetData() % 100000000 != 0) return false;
            return base.Verify(mempool);
        }
    }
}

﻿using System.Collections;
using System.IO;
using System.Linq;
using Neo.Common;
using Neo.Common.Cryptography;
using Neo.Common.IO;
using Neo.Common.Primitives;
using Neo.Core.Core;

namespace Neo.Core.Network.Payloads
{
    internal class MerkleBlockPayload : BlockBase
    {
        public int TxCount;
        public UInt256[] Hashes;
        public byte[] Flags;

        public override int Size => base.Size + sizeof(int) + Hashes.GetVarSize() + Flags.GetVarSize();

        public static MerkleBlockPayload Create(Block block, BitArray flags)
        {
            MerkleTree tree = new MerkleTree(block.Transactions.Select(p => p.Hash).ToArray());
            tree.Trim(flags);
            byte[] buffer = new byte[(flags.Length + 7) / 8];
#if NET461
            flags.CopyTo(buffer, 0);
#else
            // the method 'BitArray.CopyTo' is not available in .Net Core
            for (int i = 0; i < flags.Length; i++)
                if (flags.Get(i))
                    buffer[i / 8] |= (byte)(1 << (i % 8));
#endif
            return new MerkleBlockPayload
            {
                Version = block.Version,
                PrevHash = block.PrevHash,
                MerkleRoot = block.MerkleRoot,
                Timestamp = block.Timestamp,
                Index = block.Index,
                ConsensusData = block.ConsensusData,
                NextConsensus = block.NextConsensus,
                Script = block.Script,
                TxCount = block.Transactions.Length,
                Hashes = tree.ToHashArray(),
                Flags = buffer
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            TxCount = (int)reader.ReadVarInt(int.MaxValue);
            Hashes = reader.ReadSerializableArray<UInt256>();
            Flags = reader.ReadVarBytes();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarInt(TxCount);
            writer.Write(Hashes);
            writer.WriteVarBytes(Flags);
        }
    }
}

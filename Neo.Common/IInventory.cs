using Neo.Common.Primitives;

namespace Neo.Common
{
    public interface IInventory : IVerifiable
    {
        UInt256 Hash { get; }

        InventoryType InventoryType { get; }

        bool Verify();
    }
}

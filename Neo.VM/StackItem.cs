using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.VM
{
    /// <summary>
    ///   <en>
    ///     Represents an item that can be put onto a <see cref="RandomAccessStack{T}"/>. <remarks> This class serves as a base class for datastructures that should have the ability to be pushed/popped onto a <see cref="RandomAccessStack{T}"/> It contains methods, such as <see cref="GetBigInteger"/> and <see cref="GetBoolean" /> that attempt to map the <see cref="StackItem"/> to "primitive" vm datatypes. It also contains implementations for casting from implicit types. </remarks>
    ///   </en>
    ///   <zh-CN>
    ///     表示可以放在一个的项目<see cref="RandomAccessStack{T}"/> 。 <remarks>该类作为数据结构的基类，应该能够被推/弹出来<see cref="RandomAccessStack{T}"/>它包含方法，如<see cref="GetBigInteger"/>和<see cref="GetBoolean" />试图映射<see cref="StackItem"/>到“原始”vm数据类型。它还包含从隐式类型转换的实现。 </remarks>
    ///   </zh-CN>
    ///   <es>
    ///     Representa un elemento que se puede colocar en un <see cref="RandomAccessStack{T}"/> . <remarks> Esta clase sirve como una clase base para las estructuras de datos que deben tener la capacidad de ser empujados / <see cref="RandomAccessStack{T}"/> Contiene métodos, como <see cref="GetBigInteger"/> y <see cref="GetBoolean" /> Que intentan mapear el <see cref="StackItem"/> A &quot;primitivo&quot; vm tipos de datos. También contiene implementaciones para la conversión de tipos implícitos. </remarks>
    ///   </es>
    /// </summary>
    public abstract class StackItem : IEquatable<StackItem>
    {
        public virtual bool IsArray => false;
        public virtual bool IsStruct => false;

        public abstract bool Equals(StackItem other);

        public static StackItem FromInterface(IInteropInterface value)
        {
            return new InteropInterface(value);
        }

        public virtual StackItem[] GetArray()
        {
            throw new NotSupportedException();
        }

        public virtual BigInteger GetBigInteger()
        {
            return new BigInteger(GetByteArray());
        }

        public virtual bool GetBoolean()
        {
            return GetByteArray().Any(p => p != 0);
        }

        public abstract byte[] GetByteArray();

        public virtual T GetInterface<T>() where T : class, IInteropInterface
        {
            throw new NotSupportedException();
        }

        public static implicit operator StackItem(int value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(uint value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(long value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(ulong value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(BigInteger value)
        {
            return new Integer(value);
        }

        public static implicit operator StackItem(bool value)
        {
            return new Boolean(value);
        }

        public static implicit operator StackItem(byte[] value)
        {
            return new ByteArray(value);
        }

        public static implicit operator StackItem(StackItem[] value)
        {
            return new Array(value);
        }
    }
}

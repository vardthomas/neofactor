using System;
using System.IO;
using System.Numerics;
using System.Text;

namespace Neo.VM
{
    /// <summary>
    ///   <en>
    ///     Builds a byte representation of a script. <remarks> This class is heavily used in the Neo GUI wallet. It is used to process <see cref="OpCode"/>'s and emit a corresponding byte array. Internally, the opcodes are stored in the <see cref="ms"/> field. </remarks>
    ///   </en>
    ///   <zh-CN>
    ///     构建脚本的字节表示。 <remarks>这个类在Neo GUI钱包中被大量使用。它用于处理<see cref="OpCode"/>并发出相应的字节数组。在内部，操作码存储在<see cref="ms"/>领域。 </remarks>
    ///   </zh-CN>
    ///   <es>
    ///     Crea una representación de bytes de un script. <remarks> Esta clase es muy utilizada en la cartera de Neo GUI. Se utiliza para procesar <see cref="OpCode"/> &#39;S y emitir una matriz de bytes correspondiente. Internamente, los opcodes se almacenan <see cref="ms"/> campo. </remarks>
    ///   </es>
    /// </summary>
    public class ScriptBuilder : IDisposable
    {
        private MemoryStream ms = new MemoryStream();

        /// <summary>
        ///   <en>
        ///     Gets the current offset for the memory stream.
        ///   </en>
        ///   <zh-CN>
        ///     获取内存流的当前偏移量。
        ///   </zh-CN>
        ///   <es>
        ///     Obtiene el desplazamiento actual para el flujo de memoria.
        ///   </es>
        /// </summary>
        public int Offset => (int)ms.Position;

        public void Dispose()
        {
            ms.Dispose();
        }

        /// <summary>
        ///   <en>
        ///     Writes the byte representation of the OpCode into the <see cref="ms"/> field. If the <see cref="arg"/> property is not null, it will write it immedatiately after the opcode.
        ///   </en>
        ///   <zh-CN>
        ///     将OpCode的字节表示形式写入<see cref="ms"/>领域。如果<see cref="arg"/>属性不为空，它将在操作码后面写入。
        ///   </zh-CN>
        ///   <es>
        ///     Escribe la representación de bytes del OpCode en la <see cref="ms"/> campo. Si el <see cref="arg"/> Propiedad no es nula, lo escribirá inmediatamente después del código de operación.
        ///   </es>
        /// </summary>
        /// <param name="op">The OpCode to emit.</param>
        /// <param name="arg">Optional byte array that can be used to pass in arguments along with the OpCode.</param>
        public ScriptBuilder Emit(OpCode op, byte[] arg = null)
        {
            ms.WriteByte((byte)op);
            if (arg != null)
                ms.Write(arg, 0, arg.Length);
            return this;
        }

        /// <summary>
        ///   <en>
        ///     Emits a call to a script.
        ///   </en>
        ///   <zh-CN>
        ///     发出一个脚本的调用。
        ///   </zh-CN>
        ///   <es>
        ///     Emite una llamada a un script.
        ///   </es>
        /// </summary>
        /// <param name="scriptHash"></param>
        /// <param name="useTailCall"></param>
        /// <returns></returns>
        public ScriptBuilder EmitAppCall(byte[] scriptHash, bool useTailCall = false)
        {
            if (scriptHash.Length != 20)
                throw new ArgumentException();
            return Emit(useTailCall ? OpCode.TAILCALL : OpCode.APPCALL, scriptHash);
        }

        /// <summary>
        ///   <en>
        ///     Emits a jump to a new offset.
        ///   </en>
        ///   <zh-CN>
        ///     发出一个跳转到一个新的偏移。
        ///   </zh-CN>
        ///   <es>
        ///     Emite un salto a un nuevo desplazamiento.
        ///   </es>
        /// </summary>
        /// <param name="op"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public ScriptBuilder EmitJump(OpCode op, short offset)
        {
            if (op != OpCode.JMP && op != OpCode.JMPIF && op != OpCode.JMPIFNOT && op != OpCode.CALL)
                throw new ArgumentException();
            return Emit(op, BitConverter.GetBytes(offset));
        }

        /// <summary>
        ///   <en>
        ///     Emits push operation with a correspending <see cref="BigInteger"/>
        ///   </en>
        ///   <zh-CN>
        ///     发射推动操作与相应的<see cref="BigInteger"/>
        ///   </zh-CN>
        ///   <es>
        ///     Emite la operación de empuje con una <see cref="BigInteger"/>
        ///   </es>
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public ScriptBuilder EmitPush(BigInteger number)
        {
            if (number == -1) return Emit(OpCode.PUSHM1);
            if (number == 0) return Emit(OpCode.PUSH0);
            if (number > 0 && number <= 16) return Emit(OpCode.PUSH1 - 1 + (byte)number);
            return EmitPush(number.ToByteArray());
        }

        /// <summary>
        ///   <en>
        ///     If data is true, emits 1, else emits 0
        ///   </en>
        ///   <zh-CN>
        ///     如果数据为真，则发出1，否则发出0
        ///   </zh-CN>
        ///   <es>
        ///     Si los datos son verdaderos, emite 1, else emite 0
        ///   </es>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ScriptBuilder EmitPush(bool data)
        {
            return Emit(data ? OpCode.PUSHT : OpCode.PUSHF);
        }

        /// <summary>
        ///   <en>
        ///     First emits the length of <see cref="data"/>, then pushes its contents onto the stack
        ///   </en>
        ///   <zh-CN>
        ///     首先发射的长度<see cref="data"/> ，然后将其内容推送到堆栈上
        ///   </zh-CN>
        ///   <es>
        ///     Primero emite la longitud de <see cref="data"/> , Luego empuja su contenido sobre la pila
        ///   </es>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ScriptBuilder EmitPush(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException();
            if (data.Length <= (int)OpCode.PUSHBYTES75)
            {
                ms.WriteByte((byte)data.Length);
                ms.Write(data, 0, data.Length);
            }
            else if (data.Length < 0x100)
            {
                Emit(OpCode.PUSHDATA1);
                ms.WriteByte((byte)data.Length);
                ms.Write(data, 0, data.Length);
            }
            else if (data.Length < 0x10000)
            {
                Emit(OpCode.PUSHDATA2);
                ms.Write(BitConverter.GetBytes((ushort)data.Length), 0, 2);
                ms.Write(data, 0, data.Length);
            }
            else// if (data.Length < 0x100000000L)
            {
                Emit(OpCode.PUSHDATA4);
                ms.Write(BitConverter.GetBytes((uint)data.Length), 0, 4);
                ms.Write(data, 0, data.Length);
            }
            return this;
        }

        /// <summary>
        ///   <en>
        ///     Emits a system call.
        ///   </en>
        ///   <zh-CN>
        ///     发出系统调用。
        ///   </zh-CN>
        ///   <es>
        ///     Emite una llamada al sistema.
        ///   </es>
        /// </summary>
        /// <param name="api"></param>
        /// <returns></returns>
        public ScriptBuilder EmitSysCall(string api)
        {
            if (api == null)
                throw new ArgumentNullException();
            byte[] api_bytes = Encoding.ASCII.GetBytes(api);
            if (api_bytes.Length == 0 || api_bytes.Length > 252)
                throw new ArgumentException();
            byte[] arg = new byte[api_bytes.Length + 1];
            arg[0] = (byte)api_bytes.Length;
            Buffer.BlockCopy(api_bytes, 0, arg, 1, api_bytes.Length);
            return Emit(OpCode.SYSCALL, arg);
        }



        /// <summary>
        ///   <en>
        ///     Returns byte representation of the current program
        ///   </en>
        ///   <zh-CN>
        ///     返回当前程序的字节表示
        ///   </zh-CN>
        ///   <es>
        ///     Devuelve la representación de bytes del programa actual
        ///   </es>
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            return ms.ToArray();
        }
    }
}

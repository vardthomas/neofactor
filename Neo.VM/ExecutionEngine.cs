﻿using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Neo.Common;
using Neo.Common.Cryptography;

namespace Neo.VM
{
    /// <summary>
    ///   <en>
    ///     Executes a series of <see cref="OpCode"/> instructions.
    ///   </en>
    ///   <zh-CN>
    ///     执行一系列<see cref="OpCode"/>说明。
    ///   </zh-CN>
    ///   <es>
    ///     Ejecuta una serie de <see cref="OpCode"/> instrucciones.
    ///   </es>
    /// </summary>
    public class ExecutionEngine : IDisposable
    {
        /// <summary>
        ///   <en>
        ///     Used to resolve scripts based on a script_hash.
        ///   </en>
        ///   <zh-CN>
        ///     用于基于script_hash解析脚本。
        ///   </zh-CN>
        ///   <es>
        ///     Se utiliza para resolver scripts basados ​​en un script_hash.
        ///   </es>
        /// </summary>
        private readonly IScriptTable table;

        /// <summary>
        ///   <en>
        ///     Used to execute sys calls.
        ///   </en>
        ///   <zh-CN>
        ///     用于执行sys调用。
        ///   </zh-CN>
        ///   <es>
        ///     Se utiliza para ejecutar llamadas de sistema.
        ///   </es>
        /// </summary>
        private readonly InteropService service;

        /// <summary>
        ///   <en>
        ///     Holds the message that needs to be verified.
        ///   </en>
        ///   <zh-CN>
        ///     持有需要验证的消息。
        ///   </zh-CN>
        ///   <es>
        ///     Contiene el mensaje que necesita ser verificado.
        ///   </es>
        /// </summary>
        public IScriptContainer ScriptContainer { get; }

        /// <summary>
        ///   <en>
        ///     Uses this to calculate hashes and verifiy signatures.
        ///   </en>
        ///   <zh-CN>
        ///     使用它来计算散列和验证签名。
        ///   </zh-CN>
        ///   <es>
        ///     Utiliza esto para calcular hashes y firmas verifiy.
        ///   </es>
        /// </summary>
        public ICrypto Crypto { get; }

        /// <summary>
        ///   <en>
        ///     Serves as the callstack for scripts.
        ///   </en>
        ///   <zh-CN>
        ///     作为脚本的调用堆栈。
        ///   </zh-CN>
        ///   <es>
        ///     Sirve como la pila de llamadas para los scripts.
        ///   </es>
        /// </summary>
        public RandomAccessStack<ExecutionContext> InvocationStack { get; } = new RandomAccessStack<ExecutionContext>();

        /// <summary>
        ///   <en>
        ///     Main stack used to evaluate expressions.
        ///   </en>
        ///   <zh-CN>
        ///     主堆栈用于评估表达式。
        ///   </zh-CN>
        ///   <es>
        ///     Pila principal utilizada para evaluar expresiones.
        ///   </es>
        /// </summary>
        public RandomAccessStack<StackItem> EvaluationStack { get; } = new RandomAccessStack<StackItem>();

        /// <summary>
        ///   <en>
        ///     Often used as a temporary stack to store values from <see cref="EvaluationStack"/>
        ///   </en>
        ///   <zh-CN>
        ///     经常用作临时堆栈来存储值<see cref="EvaluationStack"/>
        ///   </zh-CN>
        ///   <es>
        ///     A menudo se utiliza como una pila temporal para almacenar valores de <see cref="EvaluationStack"/>
        ///   </es>
        /// </summary>
        public RandomAccessStack<StackItem> AltStack { get; } = new RandomAccessStack<StackItem>();

        /// <summary>
        ///   <en>
        ///     Tracks the currently executing execution context
        ///   </en>
        ///   <zh-CN>
        ///     跟踪当前执行的执行上下文
        ///   </zh-CN>
        ///   <es>
        ///     Rastrea el contexto de ejecución en ejecución
        ///   </es>
        /// </summary>
        public ExecutionContext CurrentContext => InvocationStack.Peek();

        /// <summary>
        ///   <en>
        ///     Tracks the execution context that called the currently executing context.
        ///   </en>
        ///   <zh-CN>
        ///     跟踪调用当前执行上下文的执行上下文。
        ///   </zh-CN>
        ///   <es>
        ///     Rastrea el contexto de ejecución que llamó al contexto actualmente en ejecución.
        ///   </es>
        /// </summary>
        public ExecutionContext CallingContext => InvocationStack.Count > 1 ? InvocationStack.Peek(1) : null;

        /// <summary>
        ///   <en>
        ///     Tracks the first execution context that pushed onto the stack.
        ///   </en>
        ///   <zh-CN>
        ///     跟踪推送到堆栈的第一个执行上下文。
        ///   </zh-CN>
        ///   <es>
        ///     Rastrea el primer contexto de ejecución que se inserta en la pila.
        ///   </es>
        /// </summary>
        public ExecutionContext EntryContext => InvocationStack.Peek(InvocationStack.Count - 1);

        /// <summary>
        ///   <en>
        ///     Returns that state of the virtual machine at any given time.
        ///   </en>
        ///   <zh-CN>
        ///     在任何给定时间返回虚拟机的状态。
        ///   </zh-CN>
        ///   <es>
        ///     Devuelve el estado de la máquina virtual en un momento dado.
        ///   </es>
        /// </summary>
        public VMState State { get; private set; } = VMState.BREAK;

        public ExecutionEngine(IScriptContainer container, ICrypto crypto, IScriptTable table = null, InteropService service = null)
        {
            this.ScriptContainer = container;
            this.Crypto = crypto;
            this.table = table;
            this.service = service ?? new InteropService();
        }

        public void AddBreakPoint(uint position)
        {
            CurrentContext.BreakPoints.Add(position);
        }

        public void Dispose()
        {
            while (InvocationStack.Count > 0)
                InvocationStack.Pop().Dispose();
        }

        public void Execute()
        {
            State &= ~VMState.BREAK;
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK))
                StepInto();
        }

        /// <summary>
        ///   <en>
        ///     This is the main method of execution. It executes a single <see cref="Opcode"/> within the context of <see cref="context"/>
        ///   </en>
        ///   <zh-CN>
        ///     这是执行的主要方法。它执行一个<see cref="Opcode"/>在上下文中<see cref="context"/>
        ///   </zh-CN>
        ///   <es>
        ///     Este es el principal método de ejecución. Ejecuta una sola <see cref="Opcode"/> Dentro del contexto de <see cref="context"/>
        ///   </es>
        /// </summary>
        /// <param name="opcode">Instruction to be executed</param>
        /// <param name="context">Context in which to execute the instruction.</param>
        private void ExecuteOp(OpCode opcode, ExecutionContext context)
        {
            if (opcode > OpCode.PUSH16 && opcode != OpCode.RET && context.PushOnly)
            {
                State |= VMState.FAULT;
                return;
            }
            if (opcode >= OpCode.PUSHBYTES1 && opcode <= OpCode.PUSHBYTES75)
                EvaluationStack.Push(context.OpReader.ReadBytes((byte)opcode));
            else
                switch (opcode)
                {
                    // Push value
                    case OpCode.PUSH0:
                        EvaluationStack.Push(new byte[0]);
                        break;
                    case OpCode.PUSHDATA1:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadByte()));
                        break;
                    case OpCode.PUSHDATA2:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadUInt16()));
                        break;
                    case OpCode.PUSHDATA4:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadInt32()));
                        break;
                    case OpCode.PUSHM1:
                    case OpCode.PUSH1:
                    case OpCode.PUSH2:
                    case OpCode.PUSH3:
                    case OpCode.PUSH4:
                    case OpCode.PUSH5:
                    case OpCode.PUSH6:
                    case OpCode.PUSH7:
                    case OpCode.PUSH8:
                    case OpCode.PUSH9:
                    case OpCode.PUSH10:
                    case OpCode.PUSH11:
                    case OpCode.PUSH12:
                    case OpCode.PUSH13:
                    case OpCode.PUSH14:
                    case OpCode.PUSH15:
                    case OpCode.PUSH16:
                        EvaluationStack.Push((int)opcode - (int)OpCode.PUSH1 + 1);
                        break;

                    // Control
                    case OpCode.NOP:
                        break;
                    case OpCode.JMP:
                    case OpCode.JMPIF:
                    case OpCode.JMPIFNOT:
                        {
                            int offset = context.OpReader.ReadInt16();
                            offset = context.InstructionPointer + offset - 3;
                            if (offset < 0 || offset > context.Script.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool fValue = true;
                            if (opcode > OpCode.JMP)
                            {
                                fValue = EvaluationStack.Pop().GetBoolean();
                                if (opcode == OpCode.JMPIFNOT)
                                    fValue = !fValue;
                            }
                            if (fValue)
                                context.InstructionPointer = offset;
                        }
                        break;
                    case OpCode.CALL:
                        InvocationStack.Push(context.Clone());
                        context.InstructionPointer += 2;
                        ExecuteOp(OpCode.JMP, CurrentContext);
                        break;
                    case OpCode.RET:
                        InvocationStack.Pop().Dispose();
                        if (InvocationStack.Count == 0)
                            State |= VMState.HALT;
                        break;
                    case OpCode.APPCALL:
                    case OpCode.TAILCALL:
                        {
                            if (table == null)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] script_hash = context.OpReader.ReadBytes(20);
                            byte[] script = table.GetScript(script_hash);
                            if (script == null)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (opcode == OpCode.TAILCALL)
                                InvocationStack.Pop().Dispose();
                            LoadScript(script);
                        }
                        break;
                    case OpCode.SYSCALL:
                        if (!service.Invoke(Encoding.ASCII.GetString(context.OpReader.ReadVarBytes(252)), this))
                            State |= VMState.FAULT;
                        break;

                    // Stack ops
                    case OpCode.DUPFROMALTSTACK:
                        EvaluationStack.Push(AltStack.Peek());
                        break;
                    case OpCode.TOALTSTACK:
                        AltStack.Push(EvaluationStack.Pop());
                        break;
                    case OpCode.FROMALTSTACK:
                        EvaluationStack.Push(AltStack.Pop());
                        break;
                    case OpCode.XDROP:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Remove(n);
                        }
                        break;
                    case OpCode.XSWAP:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (n == 0) break;
                            StackItem xn = EvaluationStack.Peek(n);
                            EvaluationStack.Set(n, EvaluationStack.Peek());
                            EvaluationStack.Set(0, xn);
                        }
                        break;
                    case OpCode.XTUCK:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            if (n <= 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Insert(n, EvaluationStack.Peek());
                        }
                        break;
                    case OpCode.DEPTH:
                        EvaluationStack.Push(EvaluationStack.Count);
                        break;
                    case OpCode.DROP:
                        EvaluationStack.Pop();
                        break;
                    case OpCode.DUP:
                        EvaluationStack.Push(EvaluationStack.Peek());
                        break;
                    case OpCode.NIP:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case OpCode.OVER:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Peek();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case OpCode.PICK:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Push(EvaluationStack.Peek(n));
                        }
                        break;
                    case OpCode.ROLL:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (n == 0) break;
                            EvaluationStack.Push(EvaluationStack.Remove(n));
                        }
                        break;
                    case OpCode.ROT:
                        {
                            StackItem x3 = EvaluationStack.Pop();
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x3);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case OpCode.SWAP:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case OpCode.TUCK:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case OpCode.CAT:
                        {
                            byte[] x2 = EvaluationStack.Pop().GetByteArray();
                            byte[] x1 = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(x1.Concat(x2).ToArray());
                        }
                        break;
                    case OpCode.SUBSTR:
                        {
                            int count = (int)EvaluationStack.Pop().GetBigInteger();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            int index = (int)EvaluationStack.Pop().GetBigInteger();
                            if (index < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(x.Skip(index).Take(count).ToArray());
                        }
                        break;
                    case OpCode.LEFT:
                        {
                            int count = (int)EvaluationStack.Pop().GetBigInteger();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(x.Take(count).ToArray());
                        }
                        break;
                    case OpCode.RIGHT:
                        {
                            int count = (int)EvaluationStack.Pop().GetBigInteger();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            if (x.Length < count)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Push(x.Skip(x.Length - count).ToArray());
                        }
                        break;
                    case OpCode.SIZE:
                        {
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(x.Length);
                        }
                        break;

                    // Bitwise logic
                    case OpCode.INVERT:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(~x);
                        }
                        break;
                    case OpCode.AND:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 & x2);
                        }
                        break;
                    case OpCode.OR:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 | x2);
                        }
                        break;
                    case OpCode.XOR:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 ^ x2);
                        }
                        break;
                    case OpCode.EQUAL:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x1.Equals(x2));
                        }
                        break;

                    // Numeric
                    case OpCode.INC:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x + 1);
                        }
                        break;
                    case OpCode.DEC:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x - 1);
                        }
                        break;
                    case OpCode.SIGN:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x.Sign);
                        }
                        break;
                    case OpCode.NEGATE:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(-x);
                        }
                        break;
                    case OpCode.ABS:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(BigInteger.Abs(x));
                        }
                        break;
                    case OpCode.NOT:
                        {
                            bool x = EvaluationStack.Pop().GetBoolean();
                            EvaluationStack.Push(!x);
                        }
                        break;
                    case OpCode.NZ:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x != BigInteger.Zero);
                        }
                        break;
                    case OpCode.ADD:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 + x2);
                        }
                        break;
                    case OpCode.SUB:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 - x2);
                        }
                        break;
                    case OpCode.MUL:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 * x2);
                        }
                        break;
                    case OpCode.DIV:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 / x2);
                        }
                        break;
                    case OpCode.MOD:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 % x2);
                        }
                        break;
                    case OpCode.SHL:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x << n);
                        }
                        break;
                    case OpCode.SHR:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x >> n);
                        }
                        break;
                    case OpCode.BOOLAND:
                        {
                            bool x2 = EvaluationStack.Pop().GetBoolean();
                            bool x1 = EvaluationStack.Pop().GetBoolean();
                            EvaluationStack.Push(x1 && x2);
                        }
                        break;
                    case OpCode.BOOLOR:
                        {
                            bool x2 = EvaluationStack.Pop().GetBoolean();
                            bool x1 = EvaluationStack.Pop().GetBoolean();
                            EvaluationStack.Push(x1 || x2);
                        }
                        break;
                    case OpCode.NUMEQUAL:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 == x2);
                        }
                        break;
                    case OpCode.NUMNOTEQUAL:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 != x2);
                        }
                        break;
                    case OpCode.LT:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 < x2);
                        }
                        break;
                    case OpCode.GT:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 > x2);
                        }
                        break;
                    case OpCode.LTE:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 <= x2);
                        }
                        break;
                    case OpCode.GTE:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 >= x2);
                        }
                        break;
                    case OpCode.MIN:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(BigInteger.Min(x1, x2));
                        }
                        break;
                    case OpCode.MAX:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(BigInteger.Max(x1, x2));
                        }
                        break;
                    case OpCode.WITHIN:
                        {
                            BigInteger b = EvaluationStack.Pop().GetBigInteger();
                            BigInteger a = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(a <= x && x < b);
                        }
                        break;

                    // Crypto
                    case OpCode.SHA1:
                        using (SHA1 sha = SHA1.Create())
                        {
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(sha.ComputeHash(x));
                        }
                        break;
                    case OpCode.SHA256:
                        using (SHA256 sha = SHA256.Create())
                        {
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(sha.ComputeHash(x));
                        }
                        break;
                    case OpCode.HASH160:
                        {
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(Crypto.Hash160(x));
                        }
                        break;
                    case OpCode.HASH256:
                        {
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(Crypto.Hash256(x));
                        }
                        break;
                    case OpCode.CHECKSIG:
                        {
                            byte[] pubkey = EvaluationStack.Pop().GetByteArray();
                            byte[] signature = EvaluationStack.Pop().GetByteArray();
                            try
                            {
                                EvaluationStack.Push(Crypto.VerifySignature(ScriptContainer.GetMessage(), signature, pubkey));
                            }
                            catch (ArgumentException)
                            {
                                EvaluationStack.Push(false);
                            }
                        }
                        break;
                    case OpCode.CHECKMULTISIG:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            if (n < 1)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[][] pubkeys = new byte[n][];
                            for (int i = 0; i < n; i++)
                                pubkeys[i] = EvaluationStack.Pop().GetByteArray();
                            int m = (int)EvaluationStack.Pop().GetBigInteger();
                            if (m < 1 || m > n)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[][] signatures = new byte[m][];
                            for (int i = 0; i < m; i++)
                                signatures[i] = EvaluationStack.Pop().GetByteArray();
                            byte[] message = ScriptContainer.GetMessage();
                            bool fSuccess = true;
                            try
                            {
                                for (int i = 0, j = 0; fSuccess && i < m && j < n;)
                                {
                                    if (Crypto.VerifySignature(message, signatures[i], pubkeys[j]))
                                        i++;
                                    j++;
                                    if (m - i > n - j)
                                        fSuccess = false;
                                }
                            }
                            catch (ArgumentException)
                            {
                                fSuccess = false;
                            }
                            EvaluationStack.Push(fSuccess);
                        }
                        break;

                    // Array
                    case OpCode.ARRAYSIZE:
                        {
                            StackItem item = EvaluationStack.Pop();
                            if (!item.IsArray)
                                EvaluationStack.Push(item.GetByteArray().Length);
                            else
                                EvaluationStack.Push(item.GetArray().Length);
                        }
                        break;
                    case OpCode.PACK:
                        {
                            int size = (int)EvaluationStack.Pop().GetBigInteger();
                            if (size < 0 || size > EvaluationStack.Count)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem[] items = new StackItem[size];
                            for (int i = 0; i < size; i++)
                                items[i] = EvaluationStack.Pop();
                            EvaluationStack.Push(items);
                        }
                        break;
                    case OpCode.UNPACK:
                        {
                            StackItem item = EvaluationStack.Pop();
                            if (!item.IsArray)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem[] items = item.GetArray();
                            for (int i = items.Length - 1; i >= 0; i--)
                                EvaluationStack.Push(items[i]);
                            EvaluationStack.Push(items.Length);
                        }
                        break;
                    case OpCode.PICKITEM:
                        {
                            int index = (int)EvaluationStack.Pop().GetBigInteger();
                            if (index < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem item = EvaluationStack.Pop();
                            if (!item.IsArray)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem[] items = item.GetArray();
                            if (index >= items.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Push(items[index]);
                        }
                        break;
                    case OpCode.SETITEM:
                        {
                            StackItem newItem = EvaluationStack.Pop();
                            if (newItem.IsStruct)
                            {
                                newItem = (newItem as Types.Struct).Clone();
                            }
                            int index = (int)EvaluationStack.Pop().GetBigInteger();
                            StackItem arrItem = EvaluationStack.Pop();
                            if (!arrItem.IsArray)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem[] items = arrItem.GetArray();
                            if (index < 0 || index >= items.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            items[index] = newItem;
                        }
                        break;
                    case OpCode.NEWARRAY:
                        {
                            int count = (int)EvaluationStack.Pop().GetBigInteger();
                            StackItem[] items = new StackItem[count];
                            for (var i = 0; i < count; i++)
                            {
                                items[i] = false;
                            }
                            EvaluationStack.Push(new Types.Array(items));
                        }
                        break;
                    case OpCode.NEWSTRUCT:
                        {
                            int count = (int)EvaluationStack.Pop().GetBigInteger();
                            StackItem[] items = new StackItem[count];
                            for (var i = 0; i < count; i++)
                            {
                                items[i] = false;
                            }
                            EvaluationStack.Push(new VM.Types.Struct(items));
                        }
                        break;
                    default:
                        State |= VMState.FAULT;
                        return;
                }
            if (!State.HasFlag(VMState.FAULT) && InvocationStack.Count > 0)
            {
                if (CurrentContext.BreakPoints.Contains((uint)CurrentContext.InstructionPointer))
                    State |= VMState.BREAK;
            }
        }

        public void LoadScript(byte[] script, bool push_only = false)
        {
            InvocationStack.Push(new ExecutionContext(this, script, push_only));
        }

        public bool RemoveBreakPoint(uint position)
        {
            if (InvocationStack.Count == 0) return false;
            return CurrentContext.BreakPoints.Remove(position);
        }

        /// <summary>
        ///   <en>
        ///     Executes the next opcode instruction.
        ///   </en>
        ///   <zh-CN>
        ///     执行下一个操作码指令。
        ///   </zh-CN>
        ///   <es>
        ///     Ejecuta la siguiente instrucción opcode.
        ///   </es>
        /// </summary>
        public void StepInto()
        {
            if (InvocationStack.Count == 0)
            {
                State |= VMState.HALT;
            }

            if (State.HasFlag(VMState.HALT) || State.HasFlag(VMState.FAULT))
            {
                return;
            }

            OpCode opcode = CurrentContext.InstructionPointer >= CurrentContext.Script.Length ? OpCode.RET : (OpCode)CurrentContext.OpReader.ReadByte();

            try
            {
                ExecuteOp(opcode, CurrentContext);
            }
            catch
            {
                State |= VMState.FAULT;
            }
        }

        /// <summary>
        ///   <en>
        ///     Executes the rest of the <see cref="OpCode"/>'s in the current <see cref="ExecutionContext"/>, then returns to the calling context.
        ///   </en>
        ///   <zh-CN>
        ///     执行其余的<see cref="OpCode"/>在目前<see cref="ExecutionContext"/> ，然后返回到调用上下文。
        ///   </zh-CN>
        ///   <es>
        ///     Ejecuta el resto de la <see cref="OpCode"/> En la actualidad <see cref="ExecutionContext"/> , Y luego regresa al contexto de llamada.
        ///   </es>
        /// </summary>
        public void StepOut()
        {
            State &= ~VMState.BREAK;
            int c = InvocationStack.Count;
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK) && InvocationStack.Count >= c)
                StepInto();
        }

        /// <summary>
        ///   <en>
        ///     If the <see cref="StepInto"/> method pushes a new script call onto the <see cref="InvocationStack"/>, this method will run all opcodes in the new <see cref="ExecutionContext"/>
        ///   </en>
        ///   <zh-CN>
        ///     如果<see cref="StepInto"/>方法将新的脚本调用推送到<see cref="InvocationStack"/> ，这个方法将运行新的所有操作码<see cref="ExecutionContext"/>
        ///   </zh-CN>
        ///   <es>
        ///     Si el <see cref="StepInto"/> Método empuja una nueva llamada a la <see cref="InvocationStack"/> , Este método ejecutará todos los opcodes en el nuevo <see cref="ExecutionContext"/>
        ///   </es>
        /// </summary>
        public void StepOver()
        {
            if (State.HasFlag(VMState.HALT) || State.HasFlag(VMState.FAULT)) return;
            State &= ~VMState.BREAK;
            int c = InvocationStack.Count;
            do
            {
                StepInto();
            } while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK) && InvocationStack.Count > c);
        }
    }
}

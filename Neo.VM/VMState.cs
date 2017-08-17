using System;

namespace Neo.VM
{
    /// <summary>
    ///   <en>
    ///     Keeps track of the current state of the executing virtual machine. <remarks> This is mostly used within the <see cref="ExecutionEngine"/> class.</remarks>
    ///   </en>
    ///   <zh-CN>
    ///     跟踪执行的虚拟机的当前状态。 <remarks>这主要是在内部使用的<see cref="ExecutionEngine"/>类。 </remarks>
    ///   </zh-CN>
    ///   <es>
    ///     Controla el estado actual de la máquina virtual de ejecución. <remarks> Esto se utiliza principalmente <see cref="ExecutionEngine"/> clase. </remarks>
    ///   </es>
    /// </summary>
    [Flags]
    public enum VMState : byte
    {
        NONE = 0,

        HALT = 1 << 0,
        FAULT = 1 << 1,
        BREAK = 1 << 2,
    }
}

namespace Neo.Common
{
    /// <summary>
    ///   <en>
    ///     Defines a way for callers to get Scripts based on its script hash.
    ///   </en>
    ///   <zh-CN>
    ///     定义呼叫者基于脚本哈希获取脚本的方式。
    ///   </zh-CN>
    ///   <es>
    ///     Define una forma para que los llamantes obtengan scripts basados ​​en su hash de script.
    ///   </es>
    /// </summary>
    public interface IScriptTable
    {
        byte[] GetScript(byte[] script_hash);
    }
}

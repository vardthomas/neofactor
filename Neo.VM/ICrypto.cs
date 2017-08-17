namespace Neo.VM
{
    /// <summary>
    ///   <en>
    ///     Performs basic hashing and message authentication.
    ///   </en>
    ///   <zh-CN>
    ///     执行基本散列和消息认证。
    ///   </zh-CN>
    ///   <es>
    ///     Realiza el hash básico y la autenticación de mensajes.
    ///   </es>
    /// </summary>
    public interface ICrypto
    {
        byte[] Hash160(byte[] message);

        byte[] Hash256(byte[] message);

        bool VerifySignature(byte[] message, byte[] signature, byte[] pubkey);
    }
}

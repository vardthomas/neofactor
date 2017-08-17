namespace Neo.VM
{
    /// <summary>
    ///   <en>
    ///     Allows callers to get a message. This is often used along with a public key and signature to verify message authenticity.
    ///   </en>
    ///   <zh-CN>
    ///     允许来电者收到消息。这通常与公钥和签名一起使用以验证消息的真实性。
    ///   </zh-CN>
    ///   <es>
    ///     Permite a las personas que llaman recibir un mensaje. Esto se utiliza a menudo junto con una clave pública y una firma para verificar la autenticidad del mensaje.
    ///   </es>
    /// </summary>
    /// <seealso cref="OpCode.CHECKSIG"/>
    /// <seealso cref="OpCode.CHECKMULTISIG"/>
    public interface IScriptContainer : IInteropInterface
    {
        byte[] GetMessage();
    }
}

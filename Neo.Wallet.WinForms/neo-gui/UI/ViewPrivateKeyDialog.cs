using Neo.Common.Primitives;
using Neo.Core.Wallets;
using System.Windows.Forms;
using Neo.Common;


namespace Neo.UI
{
    internal partial class ViewPrivateKeyDialog : Form
    {
        public ViewPrivateKeyDialog(KeyPair key, UInt160 scriptHash)
        {
            InitializeComponent();
            textBox3.Text = Wallet.ToAddress(scriptHash);
            textBox4.Text = key.PublicKey.EncodePoint(true).ToHexString();
            using (key.Decrypt())
            {
                textBox1.Text = key.PrivateKey.ToHexString();
            }
            textBox2.Text = key.Export();
        }
    }
}

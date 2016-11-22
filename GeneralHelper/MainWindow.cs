using LykkeWalletServices;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq.Expressions;
using System.Configuration;
using static LykkeWalletServices.OpenAssetsHelper;

namespace GeneralHelper
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
            ReadAppSettings();
        }

        private bool CheckEncryptionKey()
        {
            if (string.IsNullOrEmpty(textEncryptionKey.Text))
            {
                MessageBox.Show("Encryption key should be filled.");
                return false;
            }
            return true;
        }

        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            if (!CheckEncryptionKey())
            {
                return;
            }

            if (string.IsNullOrEmpty(textDecryptedText.Text))
            {
                MessageBox.Show("Decrypted text should be filled.");
                return;
            }

            textEncryptedText.Text = TripleDESManaged.Encrypt(textEncryptionKey.Text, textDecryptedText.Text);
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            if (!CheckEncryptionKey())
            {
                return;
            }

            if (string.IsNullOrEmpty(textEncryptedText.Text))
            {
                MessageBox.Show("Encrypted text should be filled.");
                return;
            }

            textDecryptedText.Text = TripleDESManaged.Decrypt(textEncryptionKey.Text, textEncryptedText.Text);
        }

        private void checkBoxFeePayerSame_CheckedChanged(object sender, EventArgs e)
        {
            UpdateFeePayerSection();
        }

        private void UpdateFeePayerSection()
        {
            if (checkBoxFeePayerSame.Checked)
            {
                labelFeePayerPrivateKey.Enabled = false;
                textFeePayerPrivateKey.Enabled = false;
            }
            else
            {
                labelFeePayerPrivateKey.Enabled = true;
                textFeePayerPrivateKey.Enabled = true;
            }
        }

        private void radioButtonFeePayed_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonFeeNotPayed.Checked)
            {
                checkBoxFeePayerSame.Enabled = true;
                UpdateFeePayerSection();
            }
            else
            {
                checkBoxFeePayerSame.Enabled = false;
                labelFeePayerPrivateKey.Enabled = false;
                textFeePayerPrivateKey.Enabled = false;
            }
        }

        /*
        private static string RPCUsername;
        private static string RPCPassword;
        private static string RPCServerIpAddress" value="40.113.151.120"/>

    <add key = "RPCServerPort" value="18332"/>
    */
        private static RPCConnectionParams ConnectionParams
        {
            get;
            set;
        }

        private static void ReadAppSettings()
        {
            QBitNinjaBaseUrl =
                ConfigurationManager.AppSettings["QBitNinjaBaseUrl"];

            string RPCUsername = ConfigurationManager.AppSettings["RPCUsername"];
            string RPCPassword = ConfigurationManager.AppSettings["RPCPassword"];
            string RPCServerIpAddress = ConfigurationManager.AppSettings["RPCServerIpAddress"];
            string network = ConfigurationManager.AppSettings["Network"];
            ConnectionParams = new RPCConnectionParams { Username = RPCUsername, Password = RPCPassword, IpAddress = RPCServerIpAddress, Network = network };
            WebSettings.ConnectionParams = ConnectionParams;
        }

        private async void buttonFinalize_Click(object sender, EventArgs e)
        {
            SigHash sigHash = SigHash.All;

            try
            {
                if (string.IsNullOrWhiteSpace(textUnsignedTransactionHex.Text))
                {
                    MessageBox.Show("Unsigned transaction should have a value.");
                    return;
                }

                var unsignedTransaction = new Transaction(textUnsignedTransactionHex.Text);

                if (string.IsNullOrWhiteSpace(textClientPrivateKey.Text))
                {
                    MessageBox.Show("The client private key should have a value.");
                }

                var privateKey = Base58Data.GetFromBase58Data(textClientPrivateKey.Text);
                if (!(privateKey is BitcoinSecret))
                {
                    MessageBox.Show("The provided value for private key is not a private key.");
                    return;
                }

                var secret = privateKey as BitcoinSecret;

                BitcoinSecret feePayerSecret = null;
                if (radioButtonFeeNotPayed.Checked)
                {
                    sigHash = SigHash.All | SigHash.AnyoneCanPay;

                    if (checkBoxFeePayerSame.Checked)
                    {
                        feePayerSecret = secret;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(textFeePayerPrivateKey.Text))
                        {
                            MessageBox.Show("The private key for the wallet paying the fee should be provided.");
                            return;
                        }

                        var feePayerPrivateKey = Base58Data.GetFromBase58Data(textFeePayerPrivateKey.Text);
                        if (!(feePayerPrivateKey is BitcoinSecret))
                        {
                            MessageBox.Show("The provided value for fee payer private key is not a fee payer private key.");
                            return;
                        }
                        feePayerSecret = feePayerPrivateKey as BitcoinSecret;

                        if (feePayerSecret.Network != secret.Network)
                        {
                            MessageBox.Show("The network for provided private keys should be the same.");
                            return;
                        }
                    }

                    var feeOutputs = await OpenAssetsHelper.GetWalletOutputs(feePayerSecret.GetAddress().ToString(),
                        feePayerSecret.Network, null, () => { return 0; });
                    if (feeOutputs.Item2)
                    {
                        MessageBox.Show(feeOutputs.Item3);
                        return;
                    }

                    var feeOutputToUse = feeOutputs.Item1.OrderBy(item => item.GetValue())
                        .Where(item => item.GetValue() > 15000).Select(item => item).FirstOrDefault();
                    if (feeOutputToUse == null)
                    {
                        MessageBox.Show("Could not find the proper fee output to use.");
                        return;
                    }

                    var txHex = await OpenAssetsHelper.GetTransactionHex(feeOutputToUse.GetTransactionHash(), ConnectionParams);
                    if (txHex.Item1)
                    {
                        MessageBox.Show(txHex.Item2);
                        return;
                    }

                    unsignedTransaction.AddInput(new Transaction(txHex.Item3), feeOutputToUse.GetOutputIndex());

                    TransactionSignRequest feePrivateKeySignRequest = new TransactionSignRequest { PrivateKey = feePayerSecret.ToString(), TransactionToSign = unsignedTransaction.ToHex() };
                    var feeSignedTransaction = await OpenAssetsHelper.SignTransactionWorker(feePrivateKeySignRequest, sigHash);

                    var feeSignedTransactionSighashAll = await OpenAssetsHelper.SignTransactionWorker(feePrivateKeySignRequest, SigHash.All);
                    unsignedTransaction = new Transaction(feeSignedTransaction);
                }

                TransactionSignRequest clientPrivateKeySignRequest = new TransactionSignRequest { PrivateKey = secret.ToString(), TransactionToSign = unsignedTransaction.ToHex() };
                var finalSignedTransaction = await OpenAssetsHelper.SignTransactionWorker(clientPrivateKeySignRequest, sigHash);

                textSignedTransaction.Text = finalSignedTransaction;
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.ToString());
                return;
            }

        }
    }
}

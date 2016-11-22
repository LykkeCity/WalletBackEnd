using LykkeWalletServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeneralHelper
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
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
    }
}

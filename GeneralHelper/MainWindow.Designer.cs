namespace GeneralHelper
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabGeneral = new System.Windows.Forms.TabControl();
            this.tabSettingsEncryption = new System.Windows.Forms.TabPage();
            this.btnDecrypt = new System.Windows.Forms.Button();
            this.labelEncryptedText = new System.Windows.Forms.Label();
            this.textEncryptedText = new System.Windows.Forms.TextBox();
            this.btnEncrypt = new System.Windows.Forms.Button();
            this.labelEncryptionText = new System.Windows.Forms.Label();
            this.textDecryptedText = new System.Windows.Forms.TextBox();
            this.labelEncryptionKey = new System.Windows.Forms.Label();
            this.textEncryptionKey = new System.Windows.Forms.TextBox();
            this.tabRefundFinalizer = new System.Windows.Forms.TabPage();
            this.checkBoxFeePayerSame = new System.Windows.Forms.CheckBox();
            this.labelFeePayerPrivateKey = new System.Windows.Forms.Label();
            this.textFeePayerPrivateKey = new System.Windows.Forms.TextBox();
            this.radioButtonFeePayed = new System.Windows.Forms.RadioButton();
            this.radioButtonFeeNotPayed = new System.Windows.Forms.RadioButton();
            this.buttonFinalize = new System.Windows.Forms.Button();
            this.textSignedTransaction = new System.Windows.Forms.TextBox();
            this.labelSignedTransaction = new System.Windows.Forms.Label();
            this.textClientPrivateKey = new System.Windows.Forms.TextBox();
            this.labelClientPrivateKey = new System.Windows.Forms.Label();
            this.textUnsignedTransactionHex = new System.Windows.Forms.TextBox();
            this.labelUnsignedTransaction = new System.Windows.Forms.Label();
            this.tabGeneral.SuspendLayout();
            this.tabSettingsEncryption.SuspendLayout();
            this.tabRefundFinalizer.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add(this.tabSettingsEncryption);
            this.tabGeneral.Controls.Add(this.tabRefundFinalizer);
            this.tabGeneral.Location = new System.Drawing.Point(12, 12);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.SelectedIndex = 0;
            this.tabGeneral.Size = new System.Drawing.Size(1222, 603);
            this.tabGeneral.TabIndex = 0;
            // 
            // tabSettingsEncryption
            // 
            this.tabSettingsEncryption.Controls.Add(this.btnDecrypt);
            this.tabSettingsEncryption.Controls.Add(this.labelEncryptedText);
            this.tabSettingsEncryption.Controls.Add(this.textEncryptedText);
            this.tabSettingsEncryption.Controls.Add(this.btnEncrypt);
            this.tabSettingsEncryption.Controls.Add(this.labelEncryptionText);
            this.tabSettingsEncryption.Controls.Add(this.textDecryptedText);
            this.tabSettingsEncryption.Controls.Add(this.labelEncryptionKey);
            this.tabSettingsEncryption.Controls.Add(this.textEncryptionKey);
            this.tabSettingsEncryption.Location = new System.Drawing.Point(4, 25);
            this.tabSettingsEncryption.Name = "tabSettingsEncryption";
            this.tabSettingsEncryption.Padding = new System.Windows.Forms.Padding(3);
            this.tabSettingsEncryption.Size = new System.Drawing.Size(1214, 574);
            this.tabSettingsEncryption.TabIndex = 0;
            this.tabSettingsEncryption.Text = "Settings Encryption";
            this.tabSettingsEncryption.UseVisualStyleBackColor = true;
            // 
            // btnDecrypt
            // 
            this.btnDecrypt.Location = new System.Drawing.Point(927, 71);
            this.btnDecrypt.Name = "btnDecrypt";
            this.btnDecrypt.Size = new System.Drawing.Size(85, 29);
            this.btnDecrypt.TabIndex = 7;
            this.btnDecrypt.Text = "Decrypt";
            this.btnDecrypt.UseVisualStyleBackColor = true;
            this.btnDecrypt.Click += new System.EventHandler(this.btnDecrypt_Click);
            // 
            // labelEncryptedText
            // 
            this.labelEncryptedText.AutoSize = true;
            this.labelEncryptedText.Location = new System.Drawing.Point(107, 83);
            this.labelEncryptedText.Name = "labelEncryptedText";
            this.labelEncryptedText.Size = new System.Drawing.Size(107, 17);
            this.labelEncryptedText.TabIndex = 6;
            this.labelEncryptedText.Text = "Encrypted Text:";
            // 
            // textEncryptedText
            // 
            this.textEncryptedText.Location = new System.Drawing.Point(231, 78);
            this.textEncryptedText.Name = "textEncryptedText";
            this.textEncryptedText.Size = new System.Drawing.Size(675, 22);
            this.textEncryptedText.TabIndex = 5;
            // 
            // btnEncrypt
            // 
            this.btnEncrypt.Location = new System.Drawing.Point(927, 41);
            this.btnEncrypt.Name = "btnEncrypt";
            this.btnEncrypt.Size = new System.Drawing.Size(85, 29);
            this.btnEncrypt.TabIndex = 4;
            this.btnEncrypt.Text = "Encrypt";
            this.btnEncrypt.UseVisualStyleBackColor = true;
            this.btnEncrypt.Click += new System.EventHandler(this.btnEncrypt_Click);
            // 
            // labelEncryptionText
            // 
            this.labelEncryptionText.AutoSize = true;
            this.labelEncryptionText.Location = new System.Drawing.Point(72, 53);
            this.labelEncryptionText.Name = "labelEncryptionText";
            this.labelEncryptionText.Size = new System.Drawing.Size(142, 17);
            this.labelEncryptionText.TabIndex = 3;
            this.labelEncryptionText.Text = "Text to be encrypted:";
            // 
            // textDecryptedText
            // 
            this.textDecryptedText.Location = new System.Drawing.Point(231, 48);
            this.textDecryptedText.Name = "textDecryptedText";
            this.textDecryptedText.Size = new System.Drawing.Size(675, 22);
            this.textDecryptedText.TabIndex = 2;
            // 
            // labelEncryptionKey
            // 
            this.labelEncryptionKey.AutoSize = true;
            this.labelEncryptionKey.Location = new System.Drawing.Point(107, 23);
            this.labelEncryptionKey.Name = "labelEncryptionKey";
            this.labelEncryptionKey.Size = new System.Drawing.Size(107, 17);
            this.labelEncryptionKey.TabIndex = 1;
            this.labelEncryptionKey.Text = "Encryption Key:";
            // 
            // textEncryptionKey
            // 
            this.textEncryptionKey.Location = new System.Drawing.Point(231, 20);
            this.textEncryptionKey.Name = "textEncryptionKey";
            this.textEncryptionKey.Size = new System.Drawing.Size(675, 22);
            this.textEncryptionKey.TabIndex = 0;
            // 
            // tabRefundFinalizer
            // 
            this.tabRefundFinalizer.Controls.Add(this.checkBoxFeePayerSame);
            this.tabRefundFinalizer.Controls.Add(this.labelFeePayerPrivateKey);
            this.tabRefundFinalizer.Controls.Add(this.textFeePayerPrivateKey);
            this.tabRefundFinalizer.Controls.Add(this.radioButtonFeePayed);
            this.tabRefundFinalizer.Controls.Add(this.radioButtonFeeNotPayed);
            this.tabRefundFinalizer.Controls.Add(this.buttonFinalize);
            this.tabRefundFinalizer.Controls.Add(this.textSignedTransaction);
            this.tabRefundFinalizer.Controls.Add(this.labelSignedTransaction);
            this.tabRefundFinalizer.Controls.Add(this.textClientPrivateKey);
            this.tabRefundFinalizer.Controls.Add(this.labelClientPrivateKey);
            this.tabRefundFinalizer.Controls.Add(this.textUnsignedTransactionHex);
            this.tabRefundFinalizer.Controls.Add(this.labelUnsignedTransaction);
            this.tabRefundFinalizer.Location = new System.Drawing.Point(4, 25);
            this.tabRefundFinalizer.Name = "tabRefundFinalizer";
            this.tabRefundFinalizer.Size = new System.Drawing.Size(1214, 574);
            this.tabRefundFinalizer.TabIndex = 1;
            this.tabRefundFinalizer.Text = "Refund Finalizer";
            this.tabRefundFinalizer.UseVisualStyleBackColor = true;
            // 
            // checkBoxFeePayerSame
            // 
            this.checkBoxFeePayerSame.AutoSize = true;
            this.checkBoxFeePayerSame.Checked = true;
            this.checkBoxFeePayerSame.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxFeePayerSame.Enabled = false;
            this.checkBoxFeePayerSame.Location = new System.Drawing.Point(874, 268);
            this.checkBoxFeePayerSame.Name = "checkBoxFeePayerSame";
            this.checkBoxFeePayerSame.Size = new System.Drawing.Size(298, 21);
            this.checkBoxFeePayerSame.TabIndex = 11;
            this.checkBoxFeePayerSame.Text = "Fee payer address same as client address";
            this.checkBoxFeePayerSame.UseVisualStyleBackColor = true;
            this.checkBoxFeePayerSame.CheckedChanged += new System.EventHandler(this.checkBoxFeePayerSame_CheckedChanged);
            // 
            // labelFeePayerPrivateKey
            // 
            this.labelFeePayerPrivateKey.AutoSize = true;
            this.labelFeePayerPrivateKey.Enabled = false;
            this.labelFeePayerPrivateKey.Location = new System.Drawing.Point(36, 269);
            this.labelFeePayerPrivateKey.Name = "labelFeePayerPrivateKey";
            this.labelFeePayerPrivateKey.Size = new System.Drawing.Size(149, 17);
            this.labelFeePayerPrivateKey.TabIndex = 10;
            this.labelFeePayerPrivateKey.Text = "Fee payer private key:";
            // 
            // textFeePayerPrivateKey
            // 
            this.textFeePayerPrivateKey.Enabled = false;
            this.textFeePayerPrivateKey.Location = new System.Drawing.Point(204, 267);
            this.textFeePayerPrivateKey.Name = "textFeePayerPrivateKey";
            this.textFeePayerPrivateKey.Size = new System.Drawing.Size(664, 22);
            this.textFeePayerPrivateKey.TabIndex = 9;
            // 
            // radioButtonFeePayed
            // 
            this.radioButtonFeePayed.AutoSize = true;
            this.radioButtonFeePayed.Checked = true;
            this.radioButtonFeePayed.Location = new System.Drawing.Point(330, 228);
            this.radioButtonFeePayed.Name = "radioButtonFeePayed";
            this.radioButtonFeePayed.Size = new System.Drawing.Size(97, 21);
            this.radioButtonFeePayed.TabIndex = 8;
            this.radioButtonFeePayed.TabStop = true;
            this.radioButtonFeePayed.Text = "Fee Payed";
            this.radioButtonFeePayed.UseVisualStyleBackColor = true;
            this.radioButtonFeePayed.CheckedChanged += new System.EventHandler(this.radioButtonFeePayed_CheckedChanged);
            // 
            // radioButtonFeeNotPayed
            // 
            this.radioButtonFeeNotPayed.AutoSize = true;
            this.radioButtonFeeNotPayed.Location = new System.Drawing.Point(204, 226);
            this.radioButtonFeeNotPayed.Name = "radioButtonFeeNotPayed";
            this.radioButtonFeeNotPayed.Size = new System.Drawing.Size(123, 21);
            this.radioButtonFeeNotPayed.TabIndex = 7;
            this.radioButtonFeeNotPayed.Text = "Fee Not Payed";
            this.radioButtonFeeNotPayed.UseVisualStyleBackColor = true;
            // 
            // buttonFinalize
            // 
            this.buttonFinalize.Location = new System.Drawing.Point(713, 341);
            this.buttonFinalize.Name = "buttonFinalize";
            this.buttonFinalize.Size = new System.Drawing.Size(156, 28);
            this.buttonFinalize.TabIndex = 6;
            this.buttonFinalize.Text = "Finalize Transaction";
            this.buttonFinalize.UseVisualStyleBackColor = true;
            this.buttonFinalize.Click += new System.EventHandler(this.buttonFinalize_Click);
            // 
            // textSignedTransaction
            // 
            this.textSignedTransaction.Location = new System.Drawing.Point(204, 375);
            this.textSignedTransaction.Multiline = true;
            this.textSignedTransaction.Name = "textSignedTransaction";
            this.textSignedTransaction.Size = new System.Drawing.Size(664, 174);
            this.textSignedTransaction.TabIndex = 5;
            // 
            // labelSignedTransaction
            // 
            this.labelSignedTransaction.AutoSize = true;
            this.labelSignedTransaction.Location = new System.Drawing.Point(13, 375);
            this.labelSignedTransaction.Name = "labelSignedTransaction";
            this.labelSignedTransaction.Size = new System.Drawing.Size(173, 17);
            this.labelSignedTransaction.TabIndex = 4;
            this.labelSignedTransaction.Text = "Signed Transaction (Hex):";
            // 
            // textClientPrivateKey
            // 
            this.textClientPrivateKey.Location = new System.Drawing.Point(205, 197);
            this.textClientPrivateKey.Name = "textClientPrivateKey";
            this.textClientPrivateKey.Size = new System.Drawing.Size(664, 22);
            this.textClientPrivateKey.TabIndex = 3;
            // 
            // labelClientPrivateKey
            // 
            this.labelClientPrivateKey.AutoSize = true;
            this.labelClientPrivateKey.Location = new System.Drawing.Point(65, 197);
            this.labelClientPrivateKey.Name = "labelClientPrivateKey";
            this.labelClientPrivateKey.Size = new System.Drawing.Size(120, 17);
            this.labelClientPrivateKey.TabIndex = 2;
            this.labelClientPrivateKey.Text = "Client private key:";
            // 
            // textUnsignedTransactionHex
            // 
            this.textUnsignedTransactionHex.Location = new System.Drawing.Point(205, 16);
            this.textUnsignedTransactionHex.Multiline = true;
            this.textUnsignedTransactionHex.Name = "textUnsignedTransactionHex";
            this.textUnsignedTransactionHex.Size = new System.Drawing.Size(664, 174);
            this.textUnsignedTransactionHex.TabIndex = 1;
            // 
            // labelUnsignedTransaction
            // 
            this.labelUnsignedTransaction.AutoSize = true;
            this.labelUnsignedTransaction.Location = new System.Drawing.Point(14, 16);
            this.labelUnsignedTransaction.Name = "labelUnsignedTransaction";
            this.labelUnsignedTransaction.Size = new System.Drawing.Size(189, 17);
            this.labelUnsignedTransaction.TabIndex = 0;
            this.labelUnsignedTransaction.Text = "Unsigned Transaction (Hex):";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1246, 627);
            this.Controls.Add(this.tabGeneral);
            this.Name = "MainWindow";
            this.Text = "GeneralHelper";
            this.tabGeneral.ResumeLayout(false);
            this.tabSettingsEncryption.ResumeLayout(false);
            this.tabSettingsEncryption.PerformLayout();
            this.tabRefundFinalizer.ResumeLayout(false);
            this.tabRefundFinalizer.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabGeneral;
        private System.Windows.Forms.TabPage tabSettingsEncryption;
        private System.Windows.Forms.Label labelEncryptionText;
        private System.Windows.Forms.TextBox textDecryptedText;
        private System.Windows.Forms.Label labelEncryptionKey;
        private System.Windows.Forms.TextBox textEncryptionKey;
        private System.Windows.Forms.Button btnDecrypt;
        private System.Windows.Forms.Label labelEncryptedText;
        private System.Windows.Forms.TextBox textEncryptedText;
        private System.Windows.Forms.Button btnEncrypt;
        private System.Windows.Forms.TabPage tabRefundFinalizer;
        private System.Windows.Forms.TextBox textClientPrivateKey;
        private System.Windows.Forms.Label labelClientPrivateKey;
        private System.Windows.Forms.TextBox textUnsignedTransactionHex;
        private System.Windows.Forms.Label labelUnsignedTransaction;
        private System.Windows.Forms.TextBox textSignedTransaction;
        private System.Windows.Forms.Label labelSignedTransaction;
        private System.Windows.Forms.Button buttonFinalize;
        private System.Windows.Forms.CheckBox checkBoxFeePayerSame;
        private System.Windows.Forms.Label labelFeePayerPrivateKey;
        private System.Windows.Forms.TextBox textFeePayerPrivateKey;
        private System.Windows.Forms.RadioButton radioButtonFeePayed;
        private System.Windows.Forms.RadioButton radioButtonFeeNotPayed;
    }
}


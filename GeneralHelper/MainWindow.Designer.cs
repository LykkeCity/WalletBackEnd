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
            this.textEncryptionKey = new System.Windows.Forms.TextBox();
            this.labelEncryptionKey = new System.Windows.Forms.Label();
            this.textDecryptedText = new System.Windows.Forms.TextBox();
            this.labelEncryptionText = new System.Windows.Forms.Label();
            this.btnEncrypt = new System.Windows.Forms.Button();
            this.labelEncryptedText = new System.Windows.Forms.Label();
            this.textEncryptedText = new System.Windows.Forms.TextBox();
            this.btnDecrypt = new System.Windows.Forms.Button();
            this.tabGeneral.SuspendLayout();
            this.tabSettingsEncryption.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add(this.tabSettingsEncryption);
            this.tabGeneral.Location = new System.Drawing.Point(12, 12);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.SelectedIndex = 0;
            this.tabGeneral.Size = new System.Drawing.Size(1041, 377);
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
            this.tabSettingsEncryption.Size = new System.Drawing.Size(1033, 348);
            this.tabSettingsEncryption.TabIndex = 0;
            this.tabSettingsEncryption.Text = "Settings Encryption";
            this.tabSettingsEncryption.UseVisualStyleBackColor = true;
            // 
            // textEncryptionKey
            // 
            this.textEncryptionKey.Location = new System.Drawing.Point(231, 20);
            this.textEncryptionKey.Name = "textEncryptionKey";
            this.textEncryptionKey.Size = new System.Drawing.Size(675, 22);
            this.textEncryptionKey.TabIndex = 0;
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
            // textDecryptedText
            // 
            this.textDecryptedText.Location = new System.Drawing.Point(231, 48);
            this.textDecryptedText.Name = "textDecryptedText";
            this.textDecryptedText.Size = new System.Drawing.Size(675, 22);
            this.textDecryptedText.TabIndex = 2;
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
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1060, 405);
            this.Controls.Add(this.tabGeneral);
            this.Name = "MainWindow";
            this.Text = "GeneralHelper";
            this.tabGeneral.ResumeLayout(false);
            this.tabSettingsEncryption.ResumeLayout(false);
            this.tabSettingsEncryption.PerformLayout();
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
    }
}


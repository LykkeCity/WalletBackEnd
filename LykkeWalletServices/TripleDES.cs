using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    // Used the code from here: https://msdn.microsoft.com/en-us/library/system.security.cryptography.des(v=vs.110).aspx
    public static class TripleDESManaged
    {
        public static byte[] GetNewIVKey()
        {
            using (TripleDESCryptoServiceProvider myTripleDES = new TripleDESCryptoServiceProvider())
            {
                return myTripleDES.IV.Concat(myTripleDES.Key).ToArray();
            }
        }

        public static string Encrypt(string inputKey, string message)
        {
            if(string.IsNullOrEmpty(message))
            {
                return message;
            }

            string stringIV = inputKey.Substring(0, 16);
            string stringKey = inputKey.Substring(16, 48);
            var iv = OpenAssetsHelper.StringToByteArray(stringIV);
            var key = OpenAssetsHelper.StringToByteArray(stringKey);

            return BitConverter.ToString(EncryptStringToBytes(message, key, iv)).Replace("-", string.Empty);

        }

        public static string Decrypt(string inputKey, string encrypted)
        {
            if (string.IsNullOrEmpty(encrypted))
            {
                return encrypted;
            }

            string stringIV = inputKey.Substring(0, 16);
            string stringKey = inputKey.Substring(16, 48);
            var iv = OpenAssetsHelper.StringToByteArray(stringIV);
            var key = OpenAssetsHelper.StringToByteArray(stringKey);

            return DecryptStringFromBytes(OpenAssetsHelper.StringToByteArray(encrypted), key, iv);
        }
        static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an TripleDESCryptoServiceProvider object
            // with the specified key and IV.
            using (TripleDESCryptoServiceProvider tdsAlg = new TripleDESCryptoServiceProvider())
            {
                tdsAlg.Key = Key;
                tdsAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = tdsAlg.CreateEncryptor(tdsAlg.Key, tdsAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return encrypted;

        }

        static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an TripleDESCryptoServiceProvider object
            // with the specified key and IV.
            using (TripleDESCryptoServiceProvider tdsAlg = new TripleDESCryptoServiceProvider())
            {
                tdsAlg.Key = Key;
                tdsAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = tdsAlg.CreateDecryptor(tdsAlg.Key, tdsAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
    }
}

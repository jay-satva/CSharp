using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SecureEMS.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] encryptionKey;
        private readonly byte[] iv;

        public EncryptionService(byte[] encryptionKey, byte[] iv)
        {
            this.encryptionKey = encryptionKey;
            this.iv = iv;
        }

        public string Encrypt(string plaintext)
        {
            using var aes = Aes.Create();
            aes.Key = encryptionKey;
            aes.IV = iv;

            var encryptor = aes.CreateEncryptor();

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            using var streamWriter = new StreamWriter(cryptoStream);

            streamWriter.Write(plaintext);
            streamWriter.Close();

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public string Decrypt(string cyphertext)
        {
            using var aes = Aes.Create();
            aes.Key = encryptionKey;
            aes.IV = iv;

            var decryptor = aes.CreateDecryptor();

            var cipherBytes = Convert.FromBase64String(cyphertext);

            using var memoryStream = new MemoryStream(cipherBytes);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream);

            return reader.ReadToEnd();
        }
    }
}

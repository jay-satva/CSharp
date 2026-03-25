using System;
using System.Collections.Generic;
using System.Text;

namespace SecureEMS.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plaintext);
        string Decrypt(string cyphertext);
    }
}

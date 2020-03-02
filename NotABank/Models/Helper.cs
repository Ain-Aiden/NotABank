using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NotABank.Models
{
    public class Helper
    {
        private static byte[] PrivateKey;

        private void KeyGen()
        {
            using (Aes AesGen = Aes.Create())
            {
                if (PrivateKey == null)
                {
                    PrivateKey = AesGen.Key;
                }
            }
        }
    }
}

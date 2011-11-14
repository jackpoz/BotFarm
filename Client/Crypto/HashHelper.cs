using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Client.Crypto
{
    using CryptoNS = System.Security.Cryptography;
    using HashAlgo = System.Security.Cryptography.HashAlgorithm;

    enum HashAlgorithm
    {
        SHA1,
    }

    static class HashHelper
    {
        private delegate byte[] HashFunction(params byte[][] data);

        static Dictionary<HashAlgorithm, HashFunction> HashFunctions;
        static Dictionary<HashAlgorithm, HashAlgo> HashAlgorithms;

        static HashHelper()
        {
            HashFunctions = new Dictionary<HashAlgorithm, HashFunction>();
            HashAlgorithms = new Dictionary<HashAlgorithm, HashAlgo>();

            HashFunctions[HashAlgorithm.SHA1] = SHA1;
            HashAlgorithms[HashAlgorithm.SHA1] = CryptoNS.SHA1.Create();
        }

        private static byte[] Combine(byte[][] buffers)
        {
            MemoryStream stream = new MemoryStream();

            foreach (byte[] buffer in buffers)
                stream.Write(buffer, 0, buffer.Length);

            return stream.ToArray();
        }

        public static byte[] Hash(this HashAlgorithm algorithm, params byte[][] data)
        {
            return HashFunctions[algorithm](data);
        }

        private static byte[] SHA1(params byte[][] data)
        {
            return HashAlgorithms[HashAlgorithm.SHA1].ComputeHash(Combine(data));
        }
    }
}

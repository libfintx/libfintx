using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;

namespace libfintx.EBICS.Signierung
{
    public class MGF1
    {
        const int MFG_LEN = 32;
        public byte[] Generate(byte[] seed, int masklen)
        {
            if (masklen > 4294967296 * MFG_LEN)
                throw new ArgumentException("Mask too long");

            byte[][] b = new byte[divceil(masklen, MFG_LEN)][];
            for (int i = 0; i < b.Length; i++)
            {
                var sub = i2osp(i, 4);
                var sub2 = seed.Concat(sub).ToArray();
                b[i] = Signierer.CalcSSH265Hash(sub2);
            }

            byte[] result = new byte[0];
            foreach (var item in b)
            {
                result = result.Concat(item).ToArray();
            }
            result = result.Take(masklen).ToArray();
            return result;
        }

        private int divceil(int a, int b)
        {
            return ~~(((a + b) - 1) / b);
        }

        public string rjust(string s, int width, string padding)
        {
            padding = padding.Substring(0, 1);
            if (s.Length < width)
            {
                return s.PadLeft(width, Convert.ToChar(padding));
            }
            return s;
        }

        private byte[] i2osp(int x, int len)
        {
            if (x >= Math.Pow(256, len))
                throw new ArgumentException("Integer too large");

            var arr = new BigInteger(x).ToByteArray();
            while(arr.Length < len)
            {
                arr = new byte[] { 0 }.Concat(arr).ToArray();
            }
            var result = rjust(Regex.Replace(Encoding.UTF8.GetString(arr), "\x00", "", RegexOptions.IgnoreCase), len, "\0");
            return Encoding.UTF8.GetBytes(result);
        }

        public byte[] Xor(byte[] arr1, byte[] arr2)
        {
            if (arr1.Length != arr2.Length)
                throw new ArgumentException("Different length for arr1 and arr2");

            byte[] result = new byte[arr1.Length];

            for (int i = 0; i < arr1.Length; ++i)
                result[i] = (byte) (arr1[i] ^ arr2[i]);

            return result;
        }
    }
}

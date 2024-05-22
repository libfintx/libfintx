using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ionic.Crc;
using libfintx.EBICSConfig;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;

namespace libfintx.EBICS.Signierung
{
    public class Signierer
    {
        IBytesCreator _bytesCreator;

        public Signierer(IBytesCreator bytesCreator)
        {
            _bytesCreator = bytesCreator;
        }

        public byte[] SigniereA006(string data, SignKeyPair kp)
        {
            byte[] signedData = null;
            data = FormatCctXml(data);
            var databytes = Encoding.UTF8.GetBytes(data);
            signedData = SigniereA006(databytes, kp);
            return signedData;
        }

        public byte[] SigniereA006(byte[] databytes, SignKeyPair kp)
        {
            byte[] signedData = null;
            var hash = Signierer.CalcSSH265Hash(databytes);

            var salt = _bytesCreator.CreateRandomBytes(32);

            var emsapssValue = CreateEmsaPSS(hash, salt);
            var baseValue = new BigInteger(emsapssValue, true, true);

            var keyParams = kp.PrivateKey.ExportParameters(true);
            var power = new BigInteger(keyParams.D, true, true);

            var mod = new BigInteger(keyParams.Modulus, true, true);

            var modPowValue = BigInteger.ModPow(baseValue, power, mod);
            var buffer = Signierer.ConvertToByteArray(modPowValue);

            if (buffer.Length == 257 && buffer[0] == 0)
            {
                signedData = buffer.Skip(1).Take(256).ToArray();
            }
            else
            {
                signedData = buffer;
            }

            return signedData;
        }
        
        private byte[] CreateEmsaPSS(byte[] msg, byte[] salt)
        {
            var eigntNullBytes = _bytesCreator.CreateEmptyBytes(8);
            var digestedMsg = Signierer.CalcSSH265Hash(msg);
            var mTickHash = Signierer.CalcSSH265Hash(eigntNullBytes.Concat(digestedMsg).Concat(salt).ToArray());

            var ps = _bytesCreator.CreateEmptyBytes(190);
            var db = ps.Concat(new byte[] {1}).Concat(salt).ToArray();

            var mgf1 = new MGF1();
            var dbMask = mgf1.Generate(mTickHash, db.Length);
            var maskedDB = mgf1.Xor(db, dbMask);

            NumberFormatInfo bigIntegerFormatter = new NumberFormatInfo();
            bigIntegerFormatter.NegativeSign = "~";

            var firstByte = new BigInteger(maskedDB.Take(1).ToArray());
            var firstByteTrimmed = firstByte.ToBinaryString().TrimStart('0');
            var maskedDbMsb = mgf1.rjust(firstByteTrimmed, 8, "0");

            maskedDbMsb = $"0{maskedDbMsb.Substring(1)}";

            var maskedDbMsbDec = BinToDec(maskedDbMsb);
            var maskedDbMsbByte = (new BigInteger(long.Parse(maskedDbMsbDec))).ToByteArray();
            maskedDB[0] = maskedDbMsbByte[0];

            var BC = Signierer.ConvertFromHexString("BC");
            var result = maskedDB.Concat(mTickHash).Concat(BC).ToArray();
            return result;
        }

        public string BinToDec(string value)
        {
            BigInteger res = 0;

            foreach (char c in value)
            {
                res <<= 1;
                res += c == '1' ? 1 : 0;
            }

            return res.ToString();
        }

        private string FormatCctXml(string xmlStr)
        {
            xmlStr = xmlStr.Replace("\n", "");
            xmlStr = xmlStr.Replace("\r", "");
            xmlStr = xmlStr.Replace("\t", "");
            return xmlStr;
        }

        public static byte[] CalcSSH265Hash(byte[] data)
        {
            byte[] hash;
            using (SHA256 mySHA256 = SHA256.Create())
            {
                hash = mySHA256.ComputeHash(data);
            }
            return hash;
        }

        public static string ConvertToHexString(byte[] data)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                builder.Append(data[i].ToString("x2").ToUpper());
            }
            return builder.ToString();
        }

        public static byte[] ConvertFromHexString(string hex)
        { 
            var result = Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();
            return result;
        }

        public static byte[] ConvertToByteArray( System.Numerics.BigInteger bigint)
        {
            libfintx.EBICS.Signierung.BigIntegerBouncy bigInteger = new libfintx.EBICS.Signierung.BigIntegerBouncy(bigint.ToString());
            return bigInteger.toByteArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace libfintx.EBICS
{
    public class Helper
    {
        internal static bool IsOsSpecificChar(byte octet)
        {
            switch (octet)
            {
                case (byte) '\r': // CR
                case (byte) '\n': // LF
                case 0x1A:       // CTRL-Z / EOF
                    return true;
                default:
                    return false;
            }
        }

        public static byte[] RemoveOSSpecificChars(byte[] buf)
        {
            using (MemoryStream output = new MemoryStream())
            {
                foreach (byte aBuf in buf)
                {
                    if (IsOsSpecificChar(aBuf))
                    {
                        continue;
                    }
                    output.WriteByte(aBuf);
                }
                return output.ToArray();
            }
        }
    }
}

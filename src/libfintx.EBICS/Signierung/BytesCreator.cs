using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libfintx.EBICS.Signierung
{
    public class BytesCreator : IBytesCreator
    {
        public byte[] CreateRandomBytes(int size)
        {
            Random rand = new Random();
            byte[] data = new byte[size];
            rand.NextBytes(data);
            return data;
        }

        public byte[] CreateEmptyBytes(int size)
        {
            return new byte[size];
        }
    }
}

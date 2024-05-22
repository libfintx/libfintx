using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libfintx.EBICS.Signierung
{
    public class FakeBytesCreator : IBytesCreator
    {
        public byte[] CreateRandomBytes(int size)
        {            
            return new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32};
        }

        public byte[] CreateEmptyBytes(int size)
        {
            return new byte[size];
        }
    }
}

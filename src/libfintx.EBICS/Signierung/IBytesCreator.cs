using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libfintx.EBICS.Signierung
{
    public interface IBytesCreator
    {
        byte[] CreateRandomBytes(int size);
        byte[] CreateEmptyBytes(int size);
    }
}

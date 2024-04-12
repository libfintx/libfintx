using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libfintx.EBICS.Letters
{
    public class A006Letter : Letter
    {
        public override string Title => "Signature certificate letter";
        public override string CertTitle => "Signature certificate";
        public override string HashTitle => "Signature of Signature certificate";
        public override string Version => "A006";

    }
}

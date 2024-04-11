using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libfintx.Security
{
    public class PwUtils : IPasswordFinder
    {
        private readonly string password;

        public PwUtils(string password)
        {
            this.password = password;
        }

        public char[] GetPassword()
        {
            return password.ToCharArray();
        }
    }
}

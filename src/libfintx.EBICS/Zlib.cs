/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 3 of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software Foundation,
 *  Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 * 	
 */

using System.IO;
using System.IO.Compression;

namespace libfintx.EBICS
{
    /// <summary>
    /// Compresses and decompresses EBICS order data in the zlib format (RFC 1950).
    /// </summary>
    internal static class Zlib
    {
        internal static byte[] Compress(byte[] buffer)
        {
            using (var output = new MemoryStream())
            {
                using (var zs = new ZLibStream(output, CompressionMode.Compress))
                {
                    zs.Write(buffer, 0, buffer.Length);
                }

                return output.ToArray();
            }
        }

        internal static byte[] Decompress(byte[] buffer)
        {
            using (var input = new MemoryStream(buffer))
            using (var zs = new ZLibStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                zs.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}

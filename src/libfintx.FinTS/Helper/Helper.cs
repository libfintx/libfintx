/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  Copyright (C) 2016 - 2023 Torsten Klinger
 * 	E-Mail: torsten.klinger@googlemail.com
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using libfintx.FinTS.Data.Segment;
using libfintx.Globals;
using libfintx.Logger.Log;

namespace libfintx.FinTS
{
    public static partial class Helper
    {
        /// <summary>
        /// Escapes all special Characters (':', '+', ''') with a question mark '?'.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string EscapeHbciString(string str)
        {
            return str?.Replace(":", "?:").Replace("'", "?'").Replace("+", "?+");
        }

        /// <summary>
        /// Combine byte arrays
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static byte[] CombineByteArrays(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];

            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);

            return ret;
        }

        /// <summary>
        /// Encode to Base64
        /// </summary>
        /// <param name="toEncode"></param>
        /// <returns></returns>
        public static string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes = Encoding.ASCII.GetBytes(toEncode);
            string returnValue = Convert.ToBase64String(toEncodeAsBytes);

            return returnValue;
        }

        /// <summary>
        /// Decode from Base64
        /// </summary>
        /// <param name="encodedData"></param>
        /// <returns></returns>
        public static string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes = Convert.FromBase64String(encodedData);
            string returnValue = Encoding.ASCII.GetString(encodedDataAsBytes);

            return returnValue;
        }

        /// <summary>
        /// Decode from Base64 default
        /// </summary>
        /// <param name="encodedData"></param>
        /// <returns></returns>
        public static string DecodeFrom64EncodingDefault(string encodedData)
        {
            byte[] encodedDataAsBytes = Convert.FromBase64String(encodedData);
            string returnValue = Encoding.GetEncoding("ISO-8859-1").GetString(encodedDataAsBytes);

            return returnValue;
        }

        /// <summary>
        /// Encrypt -> HNVSD
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        public static string Encrypt(string segments)
        {
            return "HNVSD:999:1+@" + segments.Length + "@" + segments + "'";
        }

        /// <summary>
        /// Extract value from string
        /// </summary>
        /// <param name="strSource"></param>
        /// <param name="strStart"></param>
        /// <param name="strEnd"></param>
        /// <returns></returns>
        public static string Parse_String(string strSource, string strStart, string strEnd)
        {
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                var start = strSource.IndexOf(strStart, 0) + strStart.Length;
                var end = strSource.IndexOf(strEnd, start);

                return strSource.Substring(start, end - start);
            }
            else
            {
                return string.Empty;
            }
        }

        [Obsolete("This method has been moved into class FinTsClient. In case you need it, implement libfintx.FinTS from source.", true)]
        public static Segment Parse_Segment(string segmentCode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Parsing segment -> UPD, BPD
        /// </summary>
        /// <param name="client"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        [Obsolete("This method has been moved into class FinTsClient. In case you need it, implement libfintx.FinTS from source.", true)]
        public static List<HBCIBankMessage> Parse_Segments(FinTsClient client, string Message)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Parsing bank message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Obsolete("This method has been moved into class FinTsClient. In case you need it, implement libfintx.FinTS from source.", true)]
        public static List<Segment> Parse_Message(FinTsClient client, string message)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Parse balance
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Obsolete("This method has been moved into class FinTsClient. In case you need it, implement libfintx.FinTS from source.", true)]
        public static AccountBalance Parse_Balance(string message)
        {
            throw new NotSupportedException();
        }

        [Obsolete("This method has been moved into class FinTsClient. In case you need it, implement libfintx.FinTS from source.", true)]
        internal static string Parse_Transactions_Startpoint(string bankCode)
        {
            throw new NotSupportedException();
        }

        [Obsolete("This method has been moved into class FinTsClient. In case you need it, implement libfintx.FinTS from source.", true)]
        public static List<string> Parse_TANMedium(string bankCode)
        {
            throw new NotSupportedException();
        }

        private static FlickerRenderer flickerCodeRenderer = null;

        /// <summary>
        /// Parse a single bank result message.
        /// </summary>
        /// <param name="bankCodeMessage"></param>
        /// <returns></returns>
        [Obsolete("This method has been moved into class FinTsClient. In case you need it, implement libfintx.FinTS from source.", true)]
        public static HBCIBankMessage? Parse_BankCode_Message(string bankCodeMessage)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Parse bank error codes
        /// </summary>
        /// <param name="bankCode"></param>
        /// <returns>Banks messages with "??" as seperator.</returns>
        [Obsolete("This method has been moved into class FinTsClient. In case you need it, implement libfintx.FinTS from source.", true)]
        public static IEnumerable<HBCIBankMessage> Parse_BankCode(string bankCode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// RUN Flicker Code Rendering
        /// </summary>
        private static void RUN_flickerCodeRenderer()
        {
            flickerCodeRenderer.Start();
        }

        /// <summary>
        /// STOP Flicker Code Rendering
        /// </summary>
        public static void RunAfterTimespan(Action action, TimeSpan span)
        {
            Thread.Sleep(span);
            action();
        }

        private static void STOP_flickerCodeRenderer()
        {
            flickerCodeRenderer.Stop();
        }

        /// <summary>
        /// Make filename valid
        /// </summary>
        public static string MakeFilenameValid(string value)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }
            return value.Replace(" ", "_");
        }

        [Obsolete("Not supported anymore. Please handle the BPD files through a BPD data store, e.g.FinTsClient.BpdStore", true)]
        private static string GetBPDDir()
        {
            throw new NotSupportedException();
        }

        [Obsolete("Not supported anymore. Please handle the BPD files through a BPD data store, e.g.FinTsClient.BpdStore", true)]
        private static string GetBPDFile(string dir, int BLZ)
        {
            throw new NotSupportedException();
        }

        private static string GetUPDDir()
        {
            var dir = FinTsGlobals.ProgramBaseDir;
            return Path.Combine(dir, "UPD");
        }

        private static string GetUPDFile(string dir, int BLZ, string UserID)
        {
            //return Path.Combine(dir, "280_" + BLZ + "_" + UserID + ".upd");
            return Path.Combine(dir, "280_" + BLZ + "_" + MakeFilenameValid(UserID) + ".upd");
        }

        public static void SaveUPD(int BLZ, string UserID, string upd)
        {
            string dir = GetUPDDir();
            Directory.CreateDirectory(dir);
            var file = GetUPDFile(dir, BLZ, UserID);
            Log.Write($"Saving UPD to '{file}' ...");
            if (!File.Exists(file))
            {
                using (File.Create(file)) { };
            }
            File.WriteAllText(file, upd);
        }

        public static string GetUPD(int BLZ, string UserID)
        {
            var dir = GetUPDDir();
            var file = GetUPDFile(dir, BLZ, UserID);
            var content = File.Exists(file) ? File.ReadAllText(file) : string.Empty;

            return content;
        }

        [Obsolete("Please use FinTsClient.BpdStore.SaveBPD instead", true)]
        public static void SaveBPD(int BLZ, string upd)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Please use FinTsClient.BpdStore.GetBPD instead", true)]
        public static string GetBPD(int BLZ)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Please use FinTsClient.BPD.IsTANReuqired instead", true)]
        public static bool IsTANRequired(string gvName)
        {
            throw new NotSupportedException();
        }
    }
}



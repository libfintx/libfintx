/*	
* 	
*  This file is part of libfintx.
*  
*  Copyright (C) 2024 Torsten Klement
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

using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Text;

namespace libfintx.EBICS.Letters
{
    public abstract class Letter
    {
        public string hostId =null;
        public string bankName = null;
        public string userId = null;
        public string username = null;
        public string partnerId = null;
        public abstract string Version { get; }
        public abstract string Title { get; }
        public abstract string CertTitle { get; }
        public abstract string HashTitle { get; }

        public StringBuilder sb = new StringBuilder();
        static byte[] CalcHash(X509Certificate cert)
        {
            return System.Security.Cryptography.SHA256.Create().ComputeHash(cert.GetEncoded());
        }
        static byte[] CalcHash(byte[] cert)
        {
            return System.Security.Cryptography.SHA256.Create().ComputeHash(cert);
        }
        static byte[] CalcHash(byte[] modulus, byte[] exponent)
        {
            var txt = BitConverter.ToString(exponent).TrimStart('0').Replace("-", "").ToLower() +
                BitConverter.ToString(modulus).TrimStart('0').Replace("-", "").ToLower();
            var cert = System.Text.Encoding.UTF8.GetBytes(txt);
            return System.Security.Cryptography.SHA256.Create().ComputeHash(cert);
        }
        public void Build(byte[] certificate,byte [] modulus, byte [] exponent)
        {
            BuildTitle();
            BuildHeader();
            if (certificate != null)
            {
                BuildCertificate(CertTitle, certificate);
                var hash = CalcHash(certificate);
                BuildHash(HashTitle, hash);
            }
            if (modulus != null && exponent != null)
            {
                Buildkey("Modulus", modulus);
                Buildkey("Exponent", exponent);
                var hash = CalcHash(modulus, exponent);
                BuildHash(HashTitle, hash);
            }
            BuildFooter();
        }
        void Emit(string s)
        {
            sb.Append(s);
        }
        const string LINE_SEPARATOR = "\r\n";
        void AppendSpacer()
        {
            Emit("\t\t\t");
        }
        string FormatDate(DateTime date)
        {
            return date.ToString("dd.MM.yyyy");
        }
        string FormatTime(DateTime date)
        {
            return date.ToString("HH:mm:ss");
        }
        void BuildTitle()
        {
            Emit(Title);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
        }
        void BuildHeader()
        {
            Emit("User Name");
            AppendSpacer();
            Emit(username);
            Emit(LINE_SEPARATOR);
            Emit("Date");
            AppendSpacer();
            Emit(FormatDate(DateTime.Now));
            Emit(LINE_SEPARATOR);
            Emit("Time");
            AppendSpacer();
            Emit(FormatTime(DateTime.Now));
            Emit(LINE_SEPARATOR);
            Emit("Host Id");
            AppendSpacer();
            Emit(hostId);
            Emit(LINE_SEPARATOR);
            Emit("Bank Name");
            AppendSpacer();
            Emit(bankName);
            Emit(LINE_SEPARATOR);
            Emit("User Id");
            AppendSpacer();
            Emit(userId);
            Emit(LINE_SEPARATOR);
            Emit("Partner Id");
            AppendSpacer();
            Emit(partnerId);
            Emit(LINE_SEPARATOR);
            Emit("Version");
            AppendSpacer();
            Emit(Version);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
        }
        IEnumerable<string> Split(string value, int desiredLength)
        {

            for (int i = 0; i < value.Length; i += desiredLength)
            {
                yield return value.Substring(i,Math.Min(desiredLength, value.Length - i));
            }
        }
        void Buildkey(String title, byte[] part)
        {
            Emit(title);
            Emit(LINE_SEPARATOR);
            Emit(String.Join(LINE_SEPARATOR, Split(BitConverter.ToString(part).Replace('-',' '), 60)));
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
        }
        void BuildCertificate(String title, byte[] cert)
        {
            Emit(title);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
            Emit("-----BEGIN CERTIFICATE-----" + LINE_SEPARATOR);
            Emit(String.Join(LINE_SEPARATOR,Split(Convert.ToBase64String(cert),60)));
            Emit(LINE_SEPARATOR);
            Emit("-----END CERTIFICATE-----" + LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
        }
        void BuildHash(String title, byte[] hash)
        {
            Emit(title);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
            Emit(String.Join(LINE_SEPARATOR, Split(BitConverter.ToString(hash),48)));
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
            Emit(LINE_SEPARATOR);
        }
        void BuildFooter()
        {
            Emit("Date");
            Emit("                                  ");
            Emit("Signature");
        }
    }
}

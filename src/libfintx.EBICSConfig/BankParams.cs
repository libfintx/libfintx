/*	
* 	
*  This file is part of libfintx.
*  
*  Copyright (C) 2018 Bjoern Kuensting
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
*  Updates done by Torsten Klement <torsten.klinger@googlemail.com>
*  
*  Updates Copyright (c) 2024 Torsten Klement
* 	
*/

using System;
using StatePrinting;
using StatePrinting.OutputFormatters;
using ebics = libfintx.Xsd.H004;

namespace libfintx.EBICSConfig
{
    public class BankParams
    {
        private static readonly Stateprinter _printer;
        //protected static string s_signatureAlg => "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        protected static string s_digestAlg => "http://www.w3.org/2001/04/xmlenc#sha256";
        public ebics.StaticHeaderTypeBankPubKeyDigests pubkeydigests
        {
            get
            {
                return new ebics.StaticHeaderTypeBankPubKeyDigests
                {
                    Authentication = AuthKeys != null ? new ebics.StaticHeaderTypeBankPubKeyDigestsAuthentication { Algorithm = s_digestAlg, Version = AuthKeys.Version.ToString(), Value = AuthKeys.Digest } : null,
                    Encryption = CryptKeys != null ? new ebics.StaticHeaderTypeBankPubKeyDigestsEncryption { Algorithm = s_digestAlg, Version = CryptKeys.Version.ToString(), Value = CryptKeys.Digest } : null,
                    Signature = SignKeys != null ? new ebics.StaticHeaderTypeBankPubKeyDigestsSignature { Algorithm = s_digestAlg, Version = SignKeys.Version.ToString(), Value = SignKeys.Digest } : null
                };
            }
        }

        public AuthKeyPair AuthKeys { get; set; }
        public CryptKeyPair CryptKeys { get; set; }
        public SignKeyPair SignKeys { get; set; }

        static BankParams()
        {
            _printer = new Stateprinter();
            _printer.Configuration.SetNewlineDefinition("");
            _printer.Configuration.SetIndentIncrement(" ");
            _printer.Configuration.SetOutputFormatter(new JsonStyle(_printer.Configuration));
        }

        public override string ToString() => _printer.PrintObject(this);

        public void Save(Action<string, byte[]> writebytes)
        {
            AuthKeys.Save(bytes=>writebytes("BankAuthKeys.pubkey",bytes));
            CryptKeys.Save(bytes=>writebytes("BankCryptKeys.pubkey",bytes));
        }

        public void Load(Func<string, byte[]> readbytes)
        {
            if (AuthKeys == null)
                AuthKeys = new AuthKeyPair();
            if (CryptKeys == null)
                CryptKeys = new CryptKeyPair();
            AuthKeys.Load(readbytes("BankAuthKeys.pubkey"));
            CryptKeys.Load(readbytes("BankCryptKeys.pubkey"));

        }
    }
}

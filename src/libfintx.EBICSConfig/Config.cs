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

using StatePrinting;
using StatePrinting.OutputFormatters;
using System;
using System.IO;

namespace libfintx.EBICSConfig
{
    public class Config
    {
        private static readonly Stateprinter _printer;

        public string Address { get; set; }
        public UserParams User { get; set; }
        public BankParams Bank { get; set; }
        public Func<string, Byte[]> readBytes;
        public Action<string, Byte[]> writeBytes;
        public string Vendor = "libfintx";
        public EbicsVersion Version { get; set; } = EbicsVersion.H004;
        public EbicsRevision Revision { get; set; } = EbicsRevision.Rev1;
        public bool TLS { get; set; }
        public bool Insecure { get; set; }

        public libfintx.Xsd.H004.StaticHeaderTypeProduct StaticHeaderTypeProduct
        {
            get {
                return new libfintx.Xsd.H004.StaticHeaderTypeProduct
                {
                    InstituteID = Vendor,
                    Language = "EN",
                    Value = Vendor
                };
            }
        }
        //TODO move to helper
        public libfintx.Xsd.H004.ProductElementType ProductElementType
        {
            get {
                return new libfintx.Xsd.H004.ProductElementType
                {
                    InstituteID = Vendor,
                    Language = "EN",
                    Value = Vendor
                };
            }
        }
        static Config()
        {
            _printer = new Stateprinter();
            _printer.Configuration.SetNewlineDefinition("");
            _printer.Configuration.SetIndentIncrement(" ");
            _printer.Configuration.SetOutputFormatter(new JsonStyle(_printer.Configuration));
        }
        public void LoadBank()
        {
            Bank = new BankParams();
            Bank.Load(readBytes);
        }

        public override string ToString() => _printer.PrintObject(this);
    }
}

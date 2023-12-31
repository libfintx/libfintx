﻿/*	
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
 */

using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using libfintx.EBICSConfig;

namespace libfintx.Xml
{
    public class XPathHelper
    {
        public XmlNamespaceManager _nm;
        public NamespaceConfig _nsc;
        public XDocument _doc;

        public string Xml => _doc?.ToString();

        public XPathHelper(XDocument doc, NamespaceConfig nsc)
        {
            _doc = doc;
            _nsc = nsc;
            var r = _doc.CreateReader();
            _nm = new XmlNamespaceManager(r.NameTable);
            _nm.AddNamespace(_nsc.EbicsPrefix, _nsc.Ebics);
            _nm.AddNamespace(_nsc.XmlDsigPrefix, _nsc.XmlDsig);
        }

        private string DNS(string name)
        {
            return $"{_nsc.EbicsPrefix}:{name}";
        }

        private string SNS(string name)
        {
            return $"{_nsc.XmlDsigPrefix}:{name}";
        }

        public XElement GetTechReturnCode()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.header)}/{DNS(XmlNames.mutable)}/{DNS(XmlNames.ReturnCode)}",
                _nm);
        }

        public XElement GetBusReturnCode()
        {
            return _doc.XPathSelectElement($"/*/{DNS(XmlNames.body)}/{DNS(XmlNames.ReturnCode)}", _nm);
        }

        public XElement GetOrderID()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.header)}/{DNS(XmlNames.staticHeader)}/{DNS(XmlNames.OrderID)}", _nm);
        }

        public XElement GetTimestampBankParameter()
        {
            return _doc.XPathSelectElement($"/*/{DNS(XmlNames.body)}/{DNS(XmlNames.TimestampBankParameter)}", _nm);
        }

        public XElement GetReportText()
        {
            //var s = $"{DNS(XMLNames.header)}/{DNS(XMLNames.mutable)}/{DNS(XMLNames.ReportText)}";
            var elem = _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.header)}/{DNS(XmlNames.mutable)}/{DNS(XmlNames.ReportText)}",
                _nm);
            return elem;
        }

        public XElement GetOrderData()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.body)}/{DNS(XmlNames.DataTransfer)}/{DNS(XmlNames.OrderData)}", _nm);
        }

        public XAttribute GetEncryptionPubKeyDigestVersion()
        {
            var elem = _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.body)}/{DNS(XmlNames.DataTransfer)}/{DNS(XmlNames.DataEncryptionInfo)}/{DNS(XmlNames.EncryptionPubKeyDigest)}",
                _nm);
            return elem?.Attribute(XmlNames.Version);
        }

        public XElement GetEncryptionPubKeyDigest()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.body)}/{DNS(XmlNames.DataTransfer)}/{DNS(XmlNames.DataEncryptionInfo)}/{DNS(XmlNames.EncryptionPubKeyDigest)}",
                _nm);
        }

        public XElement GetTransactionKey()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.body)}/{DNS(XmlNames.DataTransfer)}/{DNS(XmlNames.DataEncryptionInfo)}/{DNS(XmlNames.TransactionKey)}",
                _nm);
        }

        public XElement GetAuthenticationPubKeyInfoX509Data()
        {
            return _doc.XPathSelectElement($"/*/{DNS(XmlNames.AuthenticationPubKeyInfo)}/{DNS(XmlNames.X509)}", _nm);
        }

        public XElement GetEncryptionPubKeyInfoX509Data()
        {
            return _doc.XPathSelectElement($"/*/{DNS(XmlNames.EncryptionPubKeyInfo)}/{DNS(XmlNames.X509)}", _nm);
        }

        public XElement GetEncryptionPubKeyInfoPubKeyValue()
        {
            return _doc.XPathSelectElement($"/*/{DNS(XmlNames.EncryptionPubKeyInfo)}/{DNS(XmlNames.PubKeyValue)}",
                _nm);
        }

        public XElement GetAuthenticationPubKeyInfoPubKeyValue()
        {
            return _doc.XPathSelectElement($"/*/{DNS(XmlNames.AuthenticationPubKeyInfo)}/{DNS(XmlNames.PubKeyValue)}",
                _nm);
        }

        public XElement GetAuthenticationPubKeyInfoAuthenticationVersion()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.AuthenticationPubKeyInfo)}/{DNS(XmlNames.AuthenticationVersion)}", _nm);
        }

        public XElement GetEncryptionPubKeyInfoEncryptionVersion()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.EncryptionPubKeyInfo)}/{DNS(XmlNames.EncryptionVersion)}", _nm);
        }

        public XElement GetAuthenticationPubKeyInfoModulus()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.AuthenticationPubKeyInfo)}/{DNS(XmlNames.PubKeyValue)}/{SNS(XmlNames.RSAKeyValue)}/{SNS(XmlNames.Modulus)}",
                _nm);
        }

        public XElement GetAuthenticationPubKeyInfoExponent()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.AuthenticationPubKeyInfo)}/{DNS(XmlNames.PubKeyValue)}/{SNS(XmlNames.RSAKeyValue)}/{SNS(XmlNames.Exponent)}",
                _nm);
        }

        public XElement GetEncryptionPubKeyInfoModulus()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.EncryptionPubKeyInfo)}/{DNS(XmlNames.PubKeyValue)}/{SNS(XmlNames.RSAKeyValue)}/{SNS(XmlNames.Modulus)}",
                _nm);
        }

        public XElement GetEncryptionPubKeyInfoExponent()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.EncryptionPubKeyInfo)}/{DNS(XmlNames.PubKeyValue)}/{SNS(XmlNames.RSAKeyValue)}/{SNS(XmlNames.Exponent)}",
                _nm);
        }

        public XElement GetTransactionID()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.header)}/{DNS(XmlNames.staticHeader)}/{DNS(XmlNames.TransactionID)}", _nm);
        }

        public XElement GetNumSegments()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.header)}/{DNS(XmlNames.staticHeader)}/{DNS(XmlNames.NumSegments)}", _nm);
        }

        public XElement GetTransactionPhase()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.header)}/{DNS(XmlNames.mutable)}/{DNS(XmlNames.TransactionPhase)}", _nm);
        }

        public XElement GetSegmentNumber()
        {
            return _doc.XPathSelectElement(
                $"/*/{DNS(XmlNames.header)}/{DNS(XmlNames.mutable)}/{DNS(XmlNames.SegmentNumber)}", _nm);
        }
    }
}

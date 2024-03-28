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
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;
using libfintx.EBICS.Exceptions;
using libfintx.EBICS.Parameters;
using libfintx.EBICS.Responses;
using ebics = libfintx.Xsd.H004;

namespace libfintx.EBICS.Commands
{
    internal class IniCommand : Command
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<IniCommand>();

        internal override string OrderType => "INI";
        internal override string OrderAttribute => "DZNNN";
        internal override TransactionType TransactionType => TransactionType.Upload;
        internal override IList<XmlDocument> Requests => CreateRequests();
        internal override XmlDocument InitRequest => null;
        internal override XmlDocument ReceiptRequest => null;

        internal IniParams Params;
        public IniResponse Response=new IniResponse();

        internal override DeserializeResponse Deserialize(string payload)
        {
            var dr = Deserialize_ebicsKeyManagementResponse(payload,out var ebr);
            UpdateResponse(Response, dr);
            return dr;
        }

        private IList<XmlDocument> CreateRequests()
        {
            using (new MethodLogger(s_logger))
            {
                try
                {
                    var reqs = new List<XmlDocument>();
                    var userSigData = new ebics.SignaturePubKeyOrderDataType
                    {
                        PartnerID = Config.User.PartnerId,
                        UserID = Config.User.UserId,
                        SignaturePubKeyInfo = new ebics.SignaturePubKeyInfoType
                        {
                            SignatureVersion = "A005",
                            PubKeyValue = Config.User.SignKeys.PubKeyValueType1,
                        },
                    };
                    var doc = XMLSerializeToDocument(userSigData).OuterXml;
                    s_logger.LogDebug("User signature data:\n{data}", doc);

                    var compressed =
                        Compress(
                            Encoding.UTF8.GetBytes(doc));
                    var req = new ebics.ebicsUnsecuredRequest
                    {
                        header = new ebics.ebicsUnsecuredRequestHeader
                        {
                            @static = new ebics.UnsecuredRequestStaticHeaderType
                            {
                                HostID = Config.User.HostId,
                                PartnerID = Config.User.PartnerId,
                                UserID=Config.User.UserId,
                                Product = Config.ProductElementType,
                                SecurityMedium = Params.SecurityMedium,
                                OrderDetails = new ebics.UnsecuredReqOrderDetailsType
                                {
                                    OrderType = OrderType,
                                    OrderAttribute = OrderAttribute
                                }
                            },
                            mutable = new ebics.EmptyMutableHeaderType(),
                        },
                        body = new ebics.ebicsUnsecuredRequestBody
                        {
                            DataTransfer = new ebics.ebicsUnsecuredRequestBodyDataTransfer
                            {
                                OrderData = new ebics.ebicsUnsecuredRequestBodyDataTransferOrderData
                                {
                                    Value = compressed
                                }
                            }
                        },
                        Version="H004",
                        Revision="1"

                    };

                    reqs.Add(XMLSerializeToDocument(req));
                    return reqs;
                }
                catch (EbicsException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new CreateRequestException($"can't create requests for {OrderType}", ex);
                }
            }
        }
    }
}

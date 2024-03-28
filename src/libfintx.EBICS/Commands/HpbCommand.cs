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
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;
using libfintx.EBICSConfig;
using libfintx.EBICS.Exceptions;
using libfintx.EBICS.Parameters;
using libfintx.EBICS.Responses;
using ebics = libfintx.Xsd.H004;

namespace libfintx.EBICS.Commands
{
    internal class HpbCommand : Command
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<HpbCommand>();
        private IList<XmlDocument> _requests;

        internal HpbParams Params { private get; set; }
        internal override TransactionType TransactionType => TransactionType.Upload;
        internal override IList<XmlDocument> Requests => _requests ?? (_requests = CreateRequests());
        internal override XmlDocument InitRequest => null;
        internal override XmlDocument ReceiptRequest => null;
        internal override string OrderType => "HPB";
        internal override string OrderAttribute => "DZHNN";
        public HpbResponse Response=new HpbResponse();

        internal override DeserializeResponse Deserialize(string payload)
        {
            try
            {
                using (new MethodLogger(s_logger))
                {
                    var dr = base.Deserialize_ebicsKeyManagementResponse(payload,out var ebr);



                    Response.Bank = new BankParams();

                    Response.OrderId = ebr.header.mutable.OrderID;
                    if (ebr.body.TimestampBankParameter!=null)
                        Response.TimestampBankParameter = ebr.body.TimestampBankParameter.Value;

                    if (dr.HasError)
                    {
                        return dr;
                    }
                    var transactionkey_enc = ebr.body.DataTransfer.DataEncryptionInfo.TransactionKey;
                    var od_decrypted = ebr.body.DataTransfer.OrderData.Value;





                    var decryptedOd = DecryptOrderData(ebr.body.DataTransfer);
                    var deflatedOd = Decompress(decryptedOd);
                    var strResp = Encoding.UTF8.GetString(deflatedOd);
                    var r = XMLDeserialize<ebics.HPBResponseOrderDataType>(strResp);

                    s_logger.LogDebug("Order data:\n{orderData}", strResp);

                    if (r.AuthenticationPubKeyInfo.X509Data!=null || r.EncryptionPubKeyInfo.X509Data!=null)
                        throw new DeserializationException("X509 not supported yet", payload);

                    if (r.AuthenticationPubKeyInfo != null)
                    {
                        if (r.AuthenticationPubKeyInfo.AuthenticationVersion != "X002")
                            throw new DeserializationException(
                                "unknown authentication version for bank's authentication key");

                        var modulus = r.AuthenticationPubKeyInfo.PubKeyValue.RSAKeyValue.Modulus;
                        var exponent = r.AuthenticationPubKeyInfo.PubKeyValue.RSAKeyValue.Exponent;
                        var authPubKeyParams = new RSAParameters { Exponent = exponent, Modulus = modulus };
                        var authPubKey = RSA.Create();
                        authPubKey.ImportParameters(authPubKeyParams);
                        Response.Bank.AuthKeys = new AuthKeyPair
                        {
                            PublicKey = authPubKey,
                            Version = AuthVersion.X002
                        };
                    }
                    else
                        throw new DeserializationException("missing bank auth key");
                    if (r.EncryptionPubKeyInfo != null)
                    {
                        if (r.EncryptionPubKeyInfo.EncryptionVersion != "E002")
                            throw new DeserializationException(
                                "unknown Encryption version for bank's Encryption key");

                        var modulus = r.EncryptionPubKeyInfo.PubKeyValue.RSAKeyValue.Modulus;
                        var exponent = r.EncryptionPubKeyInfo.PubKeyValue.RSAKeyValue.Exponent;
                        var authPubKeyParams = new RSAParameters { Exponent = exponent, Modulus = modulus };
                        var authPubKey = RSA.Create();
                        authPubKey.ImportParameters(authPubKeyParams);
                        Response.Bank.CryptKeys = new CryptKeyPair
                        {
                            PublicKey = authPubKey,
                            Version = CryptVersion.E002
                        };
                    }
                    else
                        throw new DeserializationException("missing bank enc key");

                    return dr;
                }
            }
            catch (EbicsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DeserializationException($"can't deserialize {OrderType} response", ex, payload);
            }
        }

        private List<XmlDocument> CreateRequests()
        {
            using (new MethodLogger(s_logger))
            {
                try
                {
                    var reqs = new List<XmlDocument>();

                    var req = new ebics.ebicsNoPubKeyDigestsRequest
                    {
                        header=new ebics.ebicsNoPubKeyDigestsRequestHeader
                        {
                            @static=new ebics.NoPubKeyDigestsRequestStaticHeaderType
                            {
                                HostID=Config.User.HostId,
                                Nonce=CryptoUtils.GetNonceBinary(),
                                TimestampSpecified=true,
                                Timestamp=DateTime.UtcNow,
                                PartnerID=Config.User.PartnerId,
                                UserID=Config.User.UserId,
                                Product=Config.ProductElementType,
                                SecurityMedium = Params.SecurityMedium,
                                OrderDetails=new ebics.NoPubKeyDigestsReqOrderDetailsType
                                {
                                    OrderType = OrderType,
                                    OrderAttribute = OrderAttribute,
                                }
                            },
                            mutable=new ebics.EmptyMutableHeaderType { },
                        },
                        body=new ebics.ebicsNoPubKeyDigestsRequestBody { },
                        Version = "H004",
                        Revision = "1"
                    };

                    reqs.Add(Authenticate(req));
                    return reqs;
                }
                catch (Exception e)
                {
                    throw new CreateRequestException($"can't create requests for {OrderType}", e);
                }
            }
        }
    }
}

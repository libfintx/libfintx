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
 */

using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using libfintx.EBICS.Handler;
using libfintx.EBICS.Parameters;
using libfintx.EBICS.Responses;
using libfintx.EBICSConfig;

namespace libfintx.EBICS
{
    public class EbicsClientFactory
    {
        private Func<Config, IEbicsClient> _ctor;

        public EbicsClientFactory(Func<Config, IEbicsClient> ctor)
        {
            _ctor = ctor;
        }

        public IEbicsClient Create(Config cfg)
        {
            return _ctor(cfg);
        }
    }

    public partial class EbicsClient : IEbicsClient, IDisposable
    {
        private static readonly ILogger Logger = EbicsLogging.CreateLogger<EbicsClient>();
        private Config _config;
        private global::libfintx.EBICS.Version3.Ebics3Client _version3;
        public global::libfintx.EBICS.Version3.Ebics3Client Version3 => _version3 ?? throw new InvalidOperationException("Set Config.Version to H005.");
        private HttpClient _httpClient;
        private readonly ProtocolHandler _protocolHandler;
        private readonly CommandHandler _commandHandler;

        public Config Config
        {
            get => _config;
            set
            {
                _version3?.Dispose();
                _httpClient?.Dispose();
                _config = value ?? throw new ArgumentNullException(nameof(Config));
                _httpClient = new HttpClient {BaseAddress = new Uri(_config.Address)};
                _commandHandler.Config = value;
                _protocolHandler.Client = _httpClient;
                _commandHandler.ProtocolHandler = _protocolHandler;
                var nsCfg = new NamespaceConfig();
                switch (_config.Version)
                {
                    case EbicsVersion.H004:
                        nsCfg.Ebics = $"urn:org:ebics:{EbicsVersion.H004.ToString()}";
                        break;
                    case EbicsVersion.H005:
                        nsCfg.Ebics = $"urn:org:ebics:{EbicsVersion.H005.ToString()}";
                        break;
                    default:
                        throw new ArgumentException(nameof(Config.Version));
                }

                nsCfg.XmlDsig = "http://www.w3.org/2000/09/xmldsig#";
                nsCfg.Cct = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.03";
                nsCfg.Cdd = "urn:iso:std:iso:20022:tech:xsd:pain.008.001.02";
                nsCfg.SignatureData = value.Version == EbicsVersion.H005 ? "http://www.ebics.org/S002" : "http://www.ebics.org/S001";

                _commandHandler.Namespaces = nsCfg;
                _version3 = value.Version == EbicsVersion.H005 ? new global::libfintx.EBICS.Version3.Ebics3Client(value) : null;
            }
        }

        public static EbicsClientFactory Factory()
        {
            return new EbicsClientFactory(x => new EbicsClient {Config = x});
        }

        private EbicsClient()
        {
            _protocolHandler = new ProtocolHandler();
            _commandHandler = new CommandHandler();            
        }

        public HpbResponse HPB(HpbParams p)
        {
            if (_version3 != null) return _version3.HPBAsync(
                global::libfintx.EBICS.Version3.Ebics3Crypto.CertificateDigest(Config.Bank?.AuthKeys?.Certificate),
                global::libfintx.EBICS.Version3.Ebics3Crypto.CertificateDigest(Config.Bank?.CryptKeys?.Certificate)).GetAwaiter().GetResult();
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HpbResponse>(p);
                return resp;
            }
        }

        public PtkResponse PTK(PtkParams p)
        {
            if (_version3 != null)
            {
                throw new NotSupportedException("PTK is not part of EBICS H005. Use Version3.HACAsync() and process the pain.002 acknowledgement.");
            }
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<PtkResponse>(p);
                return resp;
            }
        }

        public StaResponse STA(StaParams p)
        {
            if (_version3 != null)
            {
                var result = _version3.BTDAsync(new global::libfintx.EBICS.Version3.BtdRequest { Service = p.BtfService ?? throw new ArgumentException("Specify the bank's BtfService."), StartDate = p.StartDate, EndDate = p.EndDate }).GetAwaiter().GetResult();
                var response = CopyStatus<StaResponse>(result); response.Data = DecodeData(result.Data, p.BtfService); return response;
            }
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<StaResponse>(p);
                return resp;
            }
        }

        public C53Response C53(C53Params p)
        {
            if (_version3 != null)
            {
                var result = _version3.BTDAsync(new global::libfintx.EBICS.Version3.BtdRequest { Service = p.BtfService ?? throw new ArgumentException("Specify the bank's BtfService."), StartDate = p.StartDate, EndDate = p.EndDate }).GetAwaiter().GetResult();
                var response = CopyStatus<C53Response>(result); response.Data = DecodeData(result.Data, p.BtfService); return response;
            }
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<C53Response>(p);
                return resp;
            }
        }

        public CctResponse CCT(CctParams p)
        {
            if (_version3 != null)
            {
                var service = p.BtfService ?? throw new ArgumentException("Specify the bank's BtfService, including the payload version.");
                var payload = new Commands.CctCommand { Params = p, Config = Config, Namespaces = new NamespaceConfig { Cct = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.03", Cdd = "urn:iso:std:iso:20022:tech:xsd:pain.008.001.02" } }.CreateCctDoc();
                ValidatePayloadService(payload, service);
                return CopyStatus<CctResponse>(_version3.BTUAsync(new global::libfintx.EBICS.Version3.BtuRequest { Service = service, Data = System.Text.Encoding.UTF8.GetBytes(payload.ToString(System.Xml.Linq.SaveOptions.DisableFormatting)) }).GetAwaiter().GetResult());
            }
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<CctResponse>(p);
                return resp;
            }
        }

        public IniResponse INI(IniParams p)
        {
            if (_version3 != null) return CopyStatus<IniResponse>(_version3.INIAsync().GetAwaiter().GetResult());
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<IniResponse>(p);
                return resp;
            }
        }

        public HiaResponse HIA(HiaParams p)
        {
            if (_version3 != null) return CopyStatus<HiaResponse>(_version3.HIAAsync().GetAwaiter().GetResult());
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<HiaResponse>(p);
                return resp;
            }
        }

        public SprResponse SPR(SprParams p)
        {
            if (_version3 != null) return CopyStatus<SprResponse>(_version3.SPRAsync().GetAwaiter().GetResult());
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<SprResponse>(p);
                return resp;
            }
        }

        public CddResponse CDD(CddParams p)
        {
            if (_version3 != null)
            {
                var service = p.BtfService ?? throw new ArgumentException("Specify the bank's BtfService, including the payload version.");
                var payload = new Commands.CddCommand { Params = p, Config = Config, Namespaces = new NamespaceConfig { Cct = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.03", Cdd = "urn:iso:std:iso:20022:tech:xsd:pain.008.001.02" } }.CreateCcdDoc();
                ValidatePayloadService(payload, service);
                return CopyStatus<CddResponse>(_version3.BTUAsync(new global::libfintx.EBICS.Version3.BtuRequest { Service = service, Data = System.Text.Encoding.UTF8.GetBytes(payload.ToString(System.Xml.Linq.SaveOptions.DisableFormatting)) }).GetAwaiter().GetResult());
            }
            using (new MethodLogger(Logger))
            {
                var resp = _commandHandler.Send<CddResponse>(p);
                return resp;
            }
        }
    }
}

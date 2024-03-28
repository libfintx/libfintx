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
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;
using Org.BouncyCastle.OpenSsl;
using StatePrinting;
using StatePrinting.Configurations;
using StatePrinting.FieldHarvesters;
using StatePrinting.OutputFormatters;
using StatePrinting.ValueConverters;

namespace libfintx.EBICSConfig
{
    public abstract class KeyPair<T> : IDisposable
    {
        private static readonly Stateprinter _printer;
        
        protected RSA _publicKey;
        protected RSA _privateKey;
        protected X509Certificate2 _cert;

        [JsonIgnore]
        public X509Certificate2 Certificate
        {
            get => _cert;
            set
            {
                _cert = value;

                if (_cert == null)
                {
                    return;
                }

                _publicKey = _cert.GetRSAPublicKey();
                _privateKey = _cert.GetRSAPrivateKey();
                TimeStamp = DateTime.Parse(_cert.GetEffectiveDateString());
            }
        }

        [JsonIgnore]
        public RSA PrivateKey
        {
            get => _privateKey;
            set => _privateKey = value;
        }

        [JsonIgnore]
        public RSA PublicKey
        {
            get => _publicKey;
            set => _publicKey = value;
        }
        public byte[] Modulus
        {
            get
            {
                if (_publicKey == null)
                {
                    return null;
                }
                var p = _publicKey.ExportParameters(false);
                return p.Modulus;
            }
        }
        public byte[] Exponent
        {
            get
            {
                if (_publicKey == null)
                {
                    return null;
                }
                var p = _publicKey.ExportParameters(false);
                return p.Exponent;
            }
        }

        public byte[] Digest
        {
            get
            {
                if (_publicKey == null)
                {
                    return null;
                }

                var p = _publicKey.ExportParameters(false);
                var hexExp = BitConverter.ToString(p.Exponent).Replace("-", string.Empty).ToLower()
                    .TrimStart('0');
                var hexMod = BitConverter.ToString(p.Modulus).Replace("-", string.Empty).ToLower()
                    .TrimStart('0');
                var hashInput = Encoding.ASCII.GetBytes(string.Format("{0} {1}", hexExp, hexMod));

                using (var sha256 = SHA256.Create())
                {
                    return sha256.ComputeHash(hashInput);
                }
            }
        }

        public libfintx.Xsd.H004.PubKeyValueType PubKeyValueType { get
            {
                return new libfintx.Xsd.H004.PubKeyValueType
                {
                    TimeStamp = TimeStamp.Value,
                    TimeStampSpecified = true,
                    RSAKeyValue = new libfintx.Xsd.H004.RSAKeyValueType
                    {
                        Modulus = Modulus,
                        Exponent = Exponent
                    }
                };
            }
        }
        public libfintx.Xsd.H004.PubKeyValueType1 PubKeyValueType1
        {
            get
            {
                return new libfintx.Xsd.H004.PubKeyValueType1
                {
                    TimeStamp = TimeStamp.Value,
                    TimeStampSpecified = true,
                    RSAKeyValue = new libfintx.Xsd.H004.RSAKeyValueType
                    {
                        Modulus = Modulus,
                        Exponent = Exponent
                    }
                };
            }
        }

        public DateTime? TimeStamp { get; set; }

        public T Version { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _publicKey?.Dispose();
                _privateKey?.Dispose();
                _cert?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        static KeyPair()
        {
            var cfg = new Configuration();

            cfg.SetIndentIncrement(" ");            
            cfg.OutputFormatter = new JsonStyle(cfg);
            
            cfg.Add(new StandardTypesConverter(cfg));
            cfg.Add(new StringConverter());
            cfg.Add(new DateTimeConverter(cfg));
            cfg.Add(new EnumConverter());
            cfg.Add(new PublicFieldsAndPropertiesHarvester());

            _printer = new Stateprinter(cfg);            
        }

        public void Save(Action<byte[]> writebytes)
        {
            using (var ms = new MemoryStream())
            {
                Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters rsa = new Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters(false,
                    new Org.BouncyCastle.Math.BigInteger(1, Modulus),
                    new Org.BouncyCastle.Math.BigInteger(1, Exponent)
                    );
                using (TextWriter sw = new StreamWriter(ms))
                {
                    var pw = new PemWriter(sw);
                    pw.WriteObject(rsa);
                    sw.Flush();
                }
                writebytes(ms.ToArray());
            }
        }
        public void Load(byte[] bytes)
        {
            using (var ms=new MemoryStream(bytes))
            using (var sr = new StreamReader(ms))
            {
                var pr = new PemReader(sr);
                Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters rsa = (Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters)pr.ReadObject();
                RSAParameters r = new RSAParameters();
                r.Modulus = rsa.Modulus.ToByteArrayUnsigned();
                r.Exponent = rsa.Exponent.ToByteArrayUnsigned();
                RSA rsakey = RSA.Create(r);
                PublicKey = rsakey;
            }
        }

        public override string ToString() => _printer.PrintObject(this);
    }
}

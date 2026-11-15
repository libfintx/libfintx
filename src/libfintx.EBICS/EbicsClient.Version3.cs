using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using libfintx.EBICS.Responses;
using libfintx.EBICS.Version3;

namespace libfintx.EBICS
{
    public partial class EbicsClient
    {
        private static T CopyStatus<T>(Response source) where T : Response, new() => new T
        { TechnicalReturnCode = source.TechnicalReturnCode, BusinessReturnCode = source.BusinessReturnCode, ReportText = source.ReportText };
        private static string DecodeData(byte[] data, BtfService service)
        {
            if (data.Length == 0) return "";
            if (service.ContainerType != "ZIP") return Encoding.UTF8.GetString(data);
            using var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);
            var output = new StringBuilder();
            foreach (var entry in zip.Entries)
            {
                if (entry.Length + output.Length > 128 * 1024 * 1024) throw new InvalidDataException("Container exceeds size limit.");
                using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
                var buffer = new char[8192]; int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                {
                    if (output.Length + count > 128 * 1024 * 1024) throw new InvalidDataException("Container exceeds size limit.");
                    output.Append(buffer, 0, count);
                }
            }
            return output.ToString();
        }
        private static void ValidatePayloadService(XDocument payload, BtfService service)
        {
            var ns = payload.Root.Name.NamespaceName;
            var name = service.MessageName + "." + service.MessageVariant + "." + service.MessageVersion;
            if (!ns.EndsWith(":" + name, StringComparison.Ordinal))
                throw new ArgumentException("The legacy payment builder's payload version does not match BtfService. Use Version3.BTUAsync with the bank-required ISO 20022 document.");
        }
        public void Dispose() { _version3?.Dispose(); _httpClient?.Dispose(); }
    }
}

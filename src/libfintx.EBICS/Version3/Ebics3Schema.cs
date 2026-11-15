using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace libfintx.EBICS.Version3
{
    internal static class Ebics3Schema
    {
        private static readonly Lazy<XmlSchemaSet> Schemas = new Lazy<XmlSchemaSet>(() =>
        {
            var schemas = new XmlSchemaSet { XmlResolver = new ResourceResolver() };
            using (var ds = typeof(Ebics3Schema).Assembly.GetManifestResourceStream("Ebics3Schemas.xmldsig-core-schema.xsd"))
            using (var dsReader = XmlReader.Create(ds, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }))
                schemas.Add(null, dsReader);
            using var source = typeof(Ebics3Schema).Assembly.GetManifestResourceStream("Ebics3Schemas.ebics_H005.xsd");
            using var reader = XmlReader.Create(source, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }, "https://schemas.invalid/ebics_H005.xsd");
            schemas.Add(null, reader);
            using (var hev = typeof(Ebics3Schema).Assembly.GetManifestResourceStream("Ebics3Schemas.ebics_hev.xsd"))
            using (var hevReader = XmlReader.Create(hev)) schemas.Add(null, hevReader);
            schemas.Compile(); return schemas;
        });
        internal static void Validate(XmlDocument document)
        {
            if (document.DocumentElement?.NamespaceURI != Ebics3Client.Namespace &&
                document.DocumentElement?.NamespaceURI != Ebics3Client.SignatureNamespace &&
                document.DocumentElement?.NamespaceURI != "http://www.ebics.org/H000") throw new XmlSchemaValidationException("Expected an EBICS H005, H000 or S002 namespace.");
            // Validation must not add schema defaults to the signed document (Common IG 2.3.6).
            var copy = (XmlDocument)document.CloneNode(true);
            copy.Schemas = Schemas.Value;
            copy.Validate((_, args) => { throw new XmlSchemaValidationException(args.Message, args.Exception); });
        }
        private sealed class ResourceResolver : XmlResolver
        {
            public override System.Net.ICredentials Credentials { set { } }
            public override object GetEntity(Uri uri, string role, Type ofObjectToReturn) =>
                typeof(Ebics3Schema).Assembly.GetManifestResourceStream("Ebics3Schemas." + Path.GetFileName(uri.AbsolutePath))
                ?? throw new XmlException("Unknown embedded EBICS schema.");
        }
    }
}

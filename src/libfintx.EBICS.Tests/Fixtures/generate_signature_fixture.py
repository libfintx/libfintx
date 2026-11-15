"""Independent X002 fixture generator (lxml C14N + cryptography/OpenSSL).
Only the public certificate and signed XML are saved; no private key is persisted.
Run from this directory. Runtime tests consume committed fixtures, not Python.
"""
from lxml import etree as E
from cryptography.hazmat.primitives.asymmetric import rsa, padding
from cryptography.hazmat.primitives import hashes, serialization
from cryptography import x509
from cryptography.x509.oid import NameOID
from datetime import datetime, timezone, timedelta
from pathlib import Path
import base64, hashlib
n='urn:org:ebics:H005'; d='http://www.w3.org/2000/09/xmldsig#'
key=rsa.generate_private_key(public_exponent=65537,key_size=2048)
name=x509.Name([x509.NameAttribute(NameOID.COMMON_NAME,'Independent test vector')])
now=datetime(2026,1,1,tzinfo=timezone.utc)
cert=x509.CertificateBuilder().subject_name(name).issuer_name(name).public_key(key.public_key()).serial_number(1).not_valid_before(now).not_valid_after(now+timedelta(days=3650)).sign(key,hashes.SHA256())
root=E.fromstring(f'''<ebicsResponse xmlns="{n}" xmlns:ds="{d}" xmlns:extra="urn:test:extra" Version="H005" Revision="1"><header authenticate="true"><static><TransactionID>0123456789ABCDEF0123456789ABCDEF</TransactionID></static><mutable><TransactionPhase>Receipt</TransactionPhase><ReturnCode>011000</ReturnCode><ReportText>OK</ReportText></mutable></header><AuthSignature/><body><ReturnCode authenticate="true">000000</ReturnCode></body></ebicsResponse>''')
# Materialise the namespace context before canonicalising a subtree.
can=lambda el:E.tostring(E.fromstring(E.tostring(el)),method='c14n',exclusive=False,with_comments=False)
digest=hashlib.sha256(b''.join(can(el) for el in root.xpath('//*[@authenticate="true"]'))).digest()
auth=root.find('{'+n+'}AuthSignature')
si=E.SubElement(auth,'{'+d+'}SignedInfo')
E.SubElement(si,'{'+d+'}CanonicalizationMethod',Algorithm='http://www.w3.org/TR/2001/REC-xml-c14n-20010315')
E.SubElement(si,'{'+d+'}SignatureMethod',Algorithm='http://www.w3.org/2001/04/xmldsig-more#rsa-sha256')
r=E.SubElement(si,'{'+d+'}Reference',URI="#xpointer(//*[@authenticate='true'])")
ts=E.SubElement(r,'{'+d+'}Transforms');E.SubElement(ts,'{'+d+'}Transform',Algorithm='http://www.w3.org/TR/2001/REC-xml-c14n-20010315')
E.SubElement(r,'{'+d+'}DigestMethod',Algorithm='http://www.w3.org/2001/04/xmlenc#sha256')
E.SubElement(r,'{'+d+'}DigestValue').text=base64.b64encode(digest).decode()
E.SubElement(auth,'{'+d+'}SignatureValue').text=base64.b64encode(key.sign(can(si),padding.PKCS1v15(),hashes.SHA256())).decode()
Path('x002-response.xml').write_bytes(E.tostring(root))
Path('x002-certificate.cer').write_bytes(cert.public_bytes(serialization.Encoding.DER))

# Separate vector: inherited xml:lang / xml:space and a shortened RSA integer.
# Copy the inherited XML attributes required by inclusive C14N explicitly;
# serialising a standalone lxml subtree only materialises namespace declarations.
def inherited_can(el):
    clone=E.fromstring(E.tostring(el))
    for parent in el.iterancestors():
        for name,value in parent.attrib.items():
            if name.startswith('{http://www.w3.org/XML/1998/namespace}') and name not in clone.attrib:
                clone.set(name,value)
    return E.tostring(clone,method='c14n',exclusive=False,with_comments=False)
root.set('{http://www.w3.org/XML/1998/namespace}lang','de')
auth.set('{http://www.w3.org/XML/1998/namespace}space','preserve')
for i in range(10000):
    root.find('.//{'+n+'}ReportText').text='Independent short integer vector '+str(i)
    digest=hashlib.sha256(b''.join(inherited_can(el) for el in root.xpath('//*[@authenticate="true"]'))).digest()
    r.find('{'+d+'}DigestValue').text=base64.b64encode(digest).decode()
    signature=key.sign(inherited_can(si),padding.PKCS1v15(),hashes.SHA256())
    if signature[0]==0:
        auth.find('{'+d+'}SignatureValue').text=base64.b64encode(signature.lstrip(b'\x00')).decode()
        break
else:
    raise RuntimeError('No shortened signature generated')
Path('x002-inherited-short-response.xml').write_bytes(E.tostring(root))

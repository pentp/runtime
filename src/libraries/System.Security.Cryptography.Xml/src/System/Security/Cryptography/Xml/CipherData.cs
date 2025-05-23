// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public sealed class CipherData
    {
        private XmlElement? _cachedXml;
        private CipherReference? _cipherReference;
        private byte[]? _cipherValue;

        public CipherData() { }

        public CipherData(byte[] cipherValue)
        {
            CipherValue = cipherValue;
        }

        public CipherData(CipherReference cipherReference)
        {
            CipherReference = cipherReference;
        }

        [MemberNotNullWhen(true, nameof(_cachedXml))]
        private bool CacheValid
        {
            get
            {
                return (_cachedXml != null);
            }
        }

        [DisallowNull]
        public CipherReference? CipherReference
        {
            get { return _cipherReference; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (CipherValue != null)
                    throw new CryptographicException(SR.Cryptography_Xml_CipherValueElementRequired);

                _cipherReference = value;
                _cachedXml = null;
            }
        }

        [DisallowNull]
        public byte[]? CipherValue
        {
            get { return _cipherValue; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (CipherReference != null)
                    throw new CryptographicException(SR.Cryptography_Xml_CipherValueElementRequired);

                _cipherValue = (byte[])value.Clone();
                _cachedXml = null;
            }
        }

        public XmlElement GetXml()
        {
            if (CacheValid) return _cachedXml;

            XmlDocument document = new XmlDocument();
            document.PreserveWhitespace = true;
            return GetXml(document);
        }

        internal XmlElement GetXml(XmlDocument document)
        {
            // Create the CipherData element
            XmlElement cipherDataElement = (XmlElement)document.CreateElement("CipherData", EncryptedXml.XmlEncNamespaceUrl);
            if (CipherValue != null)
            {
                XmlElement cipherValueElement = document.CreateElement("CipherValue", EncryptedXml.XmlEncNamespaceUrl);
                cipherValueElement.AppendChild(document.CreateTextNode(Convert.ToBase64String(CipherValue)));
                cipherDataElement.AppendChild(cipherValueElement);
            }
            else
            {
                // No CipherValue specified, see if there is a CipherReference
                if (CipherReference == null)
                    throw new CryptographicException(SR.Cryptography_Xml_CipherValueElementRequired);
                cipherDataElement.AppendChild(CipherReference.GetXml(document));
            }
            return cipherDataElement;
        }

        [RequiresDynamicCode(CryptoHelpers.XsltRequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
        public void LoadXml(XmlElement value)
        {
            ArgumentNullException.ThrowIfNull(value);

            XmlNamespaceManager nsm = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            nsm.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);

            XmlNode? cipherValueNode = value.SelectSingleNode("enc:CipherValue", nsm);
            XmlNode? cipherReferenceNode = value.SelectSingleNode("enc:CipherReference", nsm);
            if (cipherValueNode != null)
            {
                if (cipherReferenceNode != null)
                    throw new CryptographicException(SR.Cryptography_Xml_CipherValueElementRequired);
                _cipherValue = Convert.FromBase64String(Utils.DiscardWhiteSpaces(cipherValueNode.InnerText));
            }
            else if (cipherReferenceNode != null)
            {
                _cipherReference = new CipherReference();
                _cipherReference.LoadXml((XmlElement)cipherReferenceNode);
            }
            else
            {
                throw new CryptographicException(SR.Cryptography_Xml_CipherValueElementRequired);
            }

            // Save away the cached value
            _cachedXml = value;
        }
    }
}

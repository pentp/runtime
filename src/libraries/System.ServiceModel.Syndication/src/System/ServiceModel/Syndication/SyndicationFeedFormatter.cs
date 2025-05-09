// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    public delegate bool TryParseDateTimeCallback(XmlDateTimeData data, out DateTimeOffset dateTimeOffset);
    public delegate bool TryParseUriCallback(XmlUriData data, out Uri uri);

    [DataContract]
    public abstract class SyndicationFeedFormatter
    {
        private SyndicationFeed _feed;

        protected SyndicationFeedFormatter()
        {
            _feed = null;
            DateTimeParser = GetDefaultDateTimeParser();
        }

        protected SyndicationFeedFormatter(SyndicationFeed feedToWrite)
        {
            ArgumentNullException.ThrowIfNull(feedToWrite);

            _feed = feedToWrite;
            DateTimeParser = GetDefaultDateTimeParser();
        }

        public SyndicationFeed Feed => _feed;

        public TryParseUriCallback UriParser { get; set; } = DefaultUriParser;

        // Different DateTimeParsers are needed for Atom and Rss so can't set inline
        public TryParseDateTimeCallback DateTimeParser { get; set; }

        internal virtual TryParseDateTimeCallback GetDefaultDateTimeParser() => NotImplementedDateTimeParser;

        private bool NotImplementedDateTimeParser(XmlDateTimeData XmlDateTimeData, out DateTimeOffset dateTimeOffset)
        {
            dateTimeOffset = default;
            return false;
        }

        public abstract string Version { get; }

        public abstract bool CanRead(XmlReader reader);

        public abstract void ReadFrom(XmlReader reader);

        public override string ToString() => $"{GetType()}, SyndicationVersion={Version}";

        public abstract void WriteTo(XmlWriter writer);

        protected internal static SyndicationCategory CreateCategory(SyndicationFeed feed)
        {
            ArgumentNullException.ThrowIfNull(feed);

            return GetNonNullValue(feed.CreateCategory(), SR.FeedCreatedNullCategory);
        }

        protected internal static SyndicationCategory CreateCategory(SyndicationItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return GetNonNullValue(item.CreateCategory(), SR.ItemCreatedNullCategory);
        }

        protected internal static SyndicationItem CreateItem(SyndicationFeed feed)
        {
            ArgumentNullException.ThrowIfNull(feed);

            return GetNonNullValue(feed.CreateItem(), SR.FeedCreatedNullItem);
        }

        protected internal static SyndicationLink CreateLink(SyndicationFeed feed)
        {
            ArgumentNullException.ThrowIfNull(feed);

            return GetNonNullValue(feed.CreateLink(), SR.FeedCreatedNullPerson);
        }

        protected internal static SyndicationLink CreateLink(SyndicationItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return GetNonNullValue(item.CreateLink(), SR.ItemCreatedNullPerson);
        }

        protected internal static SyndicationPerson CreatePerson(SyndicationFeed feed)
        {
            ArgumentNullException.ThrowIfNull(feed);

            return GetNonNullValue(feed.CreatePerson(), SR.FeedCreatedNullPerson);
        }

        protected internal static SyndicationPerson CreatePerson(SyndicationItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return GetNonNullValue(item.CreatePerson(), SR.ItemCreatedNullPerson);
        }

        protected internal static void LoadElementExtensions(XmlReader reader, SyndicationFeed feed, int maxExtensionSize)
        {
            ArgumentNullException.ThrowIfNull(feed);

            feed.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected internal static void LoadElementExtensions(XmlReader reader, SyndicationItem item, int maxExtensionSize)
        {
            ArgumentNullException.ThrowIfNull(item);

            item.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected internal static void LoadElementExtensions(XmlReader reader, SyndicationCategory category, int maxExtensionSize)
        {
            ArgumentNullException.ThrowIfNull(category);

            category.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected internal static void LoadElementExtensions(XmlReader reader, SyndicationLink link, int maxExtensionSize)
        {
            ArgumentNullException.ThrowIfNull(link);

            link.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected internal static void LoadElementExtensions(XmlReader reader, SyndicationPerson person, int maxExtensionSize)
        {
            ArgumentNullException.ThrowIfNull(person);

            person.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected internal static bool TryParseAttribute(string name, string ns, string value, SyndicationFeed feed, string version)
        {
            ArgumentNullException.ThrowIfNull(feed);

            if (FeedUtils.IsXmlns(name, ns))
            {
                return true;
            }
            return feed.TryParseAttribute(name, ns, value, version);
        }

        protected internal static bool TryParseAttribute(string name, string ns, string value, SyndicationItem item, string version)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (FeedUtils.IsXmlns(name, ns))
            {
                return true;
            }
            return item.TryParseAttribute(name, ns, value, version);
        }

        protected internal static bool TryParseAttribute(string name, string ns, string value, SyndicationCategory category, string version)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (FeedUtils.IsXmlns(name, ns))
            {
                return true;
            }
            return category.TryParseAttribute(name, ns, value, version);
        }

        protected internal static bool TryParseAttribute(string name, string ns, string value, SyndicationLink link, string version)
        {
            ArgumentNullException.ThrowIfNull(link);

            if (FeedUtils.IsXmlns(name, ns))
            {
                return true;
            }
            return link.TryParseAttribute(name, ns, value, version);
        }

        protected internal static bool TryParseAttribute(string name, string ns, string value, SyndicationPerson person, string version)
        {
            ArgumentNullException.ThrowIfNull(person);

            if (FeedUtils.IsXmlns(name, ns))
            {
                return true;
            }
            return person.TryParseAttribute(name, ns, value, version);
        }

        protected internal static bool TryParseContent(XmlReader reader, SyndicationItem item, string contentType, string version, out SyndicationContent content)
        {
            return item.TryParseContent(reader, contentType, version, out content);
        }

        protected internal static bool TryParseElement(XmlReader reader, SyndicationFeed feed, string version)
        {
            ArgumentNullException.ThrowIfNull(feed);

            return feed.TryParseElement(reader, version);
        }

        protected internal static bool TryParseElement(XmlReader reader, SyndicationItem item, string version)
        {
            ArgumentNullException.ThrowIfNull(item);

            return item.TryParseElement(reader, version);
        }

        protected internal static bool TryParseElement(XmlReader reader, SyndicationCategory category, string version)
        {
            ArgumentNullException.ThrowIfNull(category);

            return category.TryParseElement(reader, version);
        }

        protected internal static bool TryParseElement(XmlReader reader, SyndicationLink link, string version)
        {
            ArgumentNullException.ThrowIfNull(link);

            return link.TryParseElement(reader, version);
        }

        protected internal static bool TryParseElement(XmlReader reader, SyndicationPerson person, string version)
        {
            ArgumentNullException.ThrowIfNull(person);

            return person.TryParseElement(reader, version);
        }

        protected internal static void WriteAttributeExtensions(XmlWriter writer, SyndicationFeed feed, string version)
        {
            ArgumentNullException.ThrowIfNull(feed);

            feed.WriteAttributeExtensions(writer, version);
        }

        protected internal static void WriteAttributeExtensions(XmlWriter writer, SyndicationItem item, string version)
        {
            ArgumentNullException.ThrowIfNull(item);

            item.WriteAttributeExtensions(writer, version);
        }

        protected internal static void WriteAttributeExtensions(XmlWriter writer, SyndicationCategory category, string version)
        {
            ArgumentNullException.ThrowIfNull(category);

            category.WriteAttributeExtensions(writer, version);
        }

        protected internal static void WriteAttributeExtensions(XmlWriter writer, SyndicationLink link, string version)
        {
            ArgumentNullException.ThrowIfNull(link);

            link.WriteAttributeExtensions(writer, version);
        }

        protected internal static void WriteAttributeExtensions(XmlWriter writer, SyndicationPerson person, string version)
        {
            ArgumentNullException.ThrowIfNull(person);

            person.WriteAttributeExtensions(writer, version);
        }

        protected internal static void WriteElementExtensions(XmlWriter writer, SyndicationFeed feed, string version)
        {
            ArgumentNullException.ThrowIfNull(feed);

            feed.WriteElementExtensions(writer, version);
        }

        protected internal static void WriteElementExtensions(XmlWriter writer, SyndicationItem item, string version)
        {
            ArgumentNullException.ThrowIfNull(item);

            item.WriteElementExtensions(writer, version);
        }

        protected internal static void WriteElementExtensions(XmlWriter writer, SyndicationCategory category, string version)
        {
            ArgumentNullException.ThrowIfNull(category);

            category.WriteElementExtensions(writer, version);
        }

        protected internal static void WriteElementExtensions(XmlWriter writer, SyndicationLink link, string version)
        {
            ArgumentNullException.ThrowIfNull(link);

            link.WriteElementExtensions(writer, version);
        }

        protected internal static void WriteElementExtensions(XmlWriter writer, SyndicationPerson person, string version)
        {
            ArgumentNullException.ThrowIfNull(person);

            person.WriteElementExtensions(writer, version);
        }

        protected internal virtual void SetFeed(SyndicationFeed feed)
        {
            ArgumentNullException.ThrowIfNull(feed);

            _feed = feed;
        }

        internal Uri UriFromString(string uriString, UriKind uriKind, string localName, string namespaceURI, XmlReader reader)
        {
            return UriFromString(UriParser, uriString, uriKind, localName, namespaceURI, reader);
        }

        internal static Uri UriFromString(TryParseUriCallback uriParser, string uriString, UriKind uriKind, string localName, string namespaceURI, XmlReader reader)
        {
            Uri uri = null;
            var elementQualifiedName = new XmlQualifiedName(localName, namespaceURI);
            var xmlUriData = new XmlUriData(uriString, uriKind, elementQualifiedName);
            object[] args = new object[] { xmlUriData, uri };
            try
            {
                foreach (Delegate parser in uriParser.GetInvocationList())
                {
                    if ((bool)parser.Method.Invoke(parser.Target, args))
                    {
                        uri = (Uri)args[args.Length - 1];
                        return uri;
                    }
                }
            }
            catch (Exception e)
            {
                throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingUri), e);
            }

            DefaultUriParser(xmlUriData, out uri);
            return uri;
        }

        internal DateTimeOffset DateFromString(string dateTimeString, XmlReader reader)
        {
            try
            {
                DateTimeOffset dateTimeOffset = default;
                var elementQualifiedName = new XmlQualifiedName(reader.LocalName, reader.NamespaceURI);
                var xmlDateTimeData = new XmlDateTimeData(dateTimeString, elementQualifiedName);
                object[] args = new object[] { xmlDateTimeData, dateTimeOffset };
                foreach (Delegate dateTimeParser in DateTimeParser.GetInvocationList())
                {
                    if ((bool)dateTimeParser.Method.Invoke(dateTimeParser.Target, args))
                    {
                        dateTimeOffset = (DateTimeOffset)args[args.Length - 1];
                        return dateTimeOffset;
                    }
                }
            }
            catch (Exception e)
            {
                throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingDateTime), e);
            }

            throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingDateTime));
        }

        internal static bool DefaultUriParser(XmlUriData XmlUriData, out Uri uri)
        {
            uri = new Uri(XmlUriData.UriString, XmlUriData.UriKind);
            return true;
        }

        internal static void CloseBuffer(XmlBuffer buffer, XmlDictionaryWriter extWriter)
        {
            if (buffer == null)
            {
                return;
            }
            extWriter.WriteEndElement();
            buffer.CloseSection();
            buffer.Close();
        }

        internal static void CreateBufferIfRequiredAndWriteNode(ref XmlBuffer buffer, ref XmlDictionaryWriter extWriter, XmlReader reader, int maxExtensionSize)
        {
            if (buffer == null)
            {
                buffer = new XmlBuffer(maxExtensionSize);
                extWriter = buffer.OpenSection(XmlDictionaryReaderQuotas.Max);
                extWriter.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
            }
            extWriter.WriteNode(reader, false);
        }

        internal static SyndicationFeed CreateFeedInstance(Type feedType)
        {
            if (feedType.Equals(typeof(SyndicationFeed)))
            {
                return new SyndicationFeed();
            }
            else
            {
                return (SyndicationFeed)Activator.CreateInstance(feedType);
            }
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationFeed feed)
        {
            ArgumentNullException.ThrowIfNull(feed);

            CloseBuffer(buffer, writer);
            feed.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationItem item)
        {
            Debug.Assert(item != null);

            CloseBuffer(buffer, writer);
            item.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationCategory category)
        {
            Debug.Assert(category != null);

            CloseBuffer(buffer, writer);
            category.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationLink link)
        {
            Debug.Assert(link != null);

            CloseBuffer(buffer, writer);
            link.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationPerson person)
        {
            Debug.Assert(person != null);

            CloseBuffer(buffer, writer);
            person.LoadElementExtensions(buffer);
        }

        internal static void MoveToStartElement(XmlReader reader)
        {
            Debug.Assert(reader != null, "reader != null");
            if (!reader.IsStartElement())
            {
                XmlExceptionHelper.ThrowStartElementExpected(XmlDictionaryReader.CreateDictionaryReader(reader));
            }
        }

        protected abstract SyndicationFeed CreateFeedInstance();

        private static T GetNonNullValue<T>(T value, string errorMsg)
        {
            if (value == null)
            {
                throw new InvalidOperationException(errorMsg);
            }

            return value;
        }

        private static class XmlExceptionHelper
        {
            private static void ThrowXmlException(XmlDictionaryReader reader, string res, string arg1)
            {
                string s = SR.Format(res, arg1);
                if (reader is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
                {
                    s += " " + SR.Format(SR.XmlLineInfo, lineInfo.LineNumber, lineInfo.LinePosition);
                }

                throw new XmlException(s);
            }

            private static string GetName(string prefix, string localName)
            {
                if (prefix.Length == 0)
                    return localName;
                else
                    return string.Concat(prefix, ":", localName);
            }

            private static string GetWhatWasFound(XmlDictionaryReader reader)
            {
                if (reader.EOF)
                    return SR.XmlFoundEndOfFile;
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        return SR.Format(SR.XmlFoundElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                    case XmlNodeType.EndElement:
                        return SR.Format(SR.XmlFoundEndElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        return SR.Format(SR.XmlFoundText, reader.Value);
                    case XmlNodeType.Comment:
                        return SR.Format(SR.XmlFoundComment, reader.Value);
                    case XmlNodeType.CDATA:
                        return SR.Format(SR.XmlFoundCData, reader.Value);
                }
                return SR.Format(SR.XmlFoundNodeType, reader.NodeType);
            }

            public static void ThrowStartElementExpected(XmlDictionaryReader reader)
            {
                ThrowXmlException(reader, SR.XmlStartElementExpected, GetWhatWasFound(reader));
            }
        }
    }
}

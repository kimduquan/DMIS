using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using ContentRepository.Schema;
using ContentRepository.Storage;
using ContentRepository.Storage.Search;
using System.Linq;
using System.IO;
using Diagnostics;
using System.ComponentModel;
using System.Globalization;
using ContentRepository.Fields;
using System.Xml.XPath;
using System.Web.Configuration;
using ApplicationModel;
using Search;

namespace ContentRepository
{
    public class SearchFolder : FeedContent
    {
        private ContentQuery _contentQuery;

        public IEnumerable<Node> Children { get; private set; }

        private SearchFolder() { }

        public static SearchFolder Create(ContentQuery query)
        {
            var folder = new SearchFolder
            {
                _contentQuery = query,
                Children = query.Execute().Nodes.ToArray()
            };
            return folder;
        }

        public static SearchFolder Create(IEnumerable<Node> nodes)
        {
            return new SearchFolder { Children = nodes };
        }

        protected override void WriteXml(XmlWriter writer, bool withChildren, SerializationOptions options)
        {
            const string thisName = "SearchFolder";
            const string thisPath = "/Root/SearchFolder";

            writer.WriteStartElement("Content");
            base.WriteHead(writer, thisName, thisName, thisName, thisPath, true);

            if (withChildren && Children != null)
            {
                writer.WriteStartElement("Children");
                this.WriteXml(Children, writer, options);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        protected override void WriteXml(XmlWriter writer, string referenceMemberName, SerializationOptions options)
        {
            WriteXml(writer, false, options);
        }
    }

}

using System;
using System.Collections.Generic;
using System.Text;
using  ContentRepository.Schema;
using System.Xml;
using System.Xml.XPath;
using Search;
using Search.Indexing;

namespace ContentRepository.Fields
{
	public class NullFieldSetting : FieldSetting
	{
        private static readonly List<string> NotIndexedList = new List<string>(new string[]
        {
            "UrlList", "Color", "Image", "Lock", "Security", "SiteRelativeUrl", "WhoAndWhen"/*, "NodeType", "Version"*/
        });
        protected override FieldIndexHandler CreateDefaultIndexFieldHandler()
        {
            if (this.ShortName == "Boolean")
                return new BooleanIndexHandler();
            if (NotIndexedList.Contains(this.ShortName))
                return new NotIndexedIndexFieldHandler();
            return base.CreateDefaultIndexFieldHandler();
        }

        protected override void WriteConfiguration(XmlWriter writers)
        {
        }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Schema;

namespace ContentRepository.Fields
{
    [ShortName("Xml")]
    [DataSlot(0, RepositoryDataType.Text, typeof(string))]
    [DefaultFieldSetting(typeof(XmlFieldSetting))]
    [DefaultFieldControl("Portal.UI.Controls.LongText")]
    public class XmlField : Field, ContentRepository.Xpath.IRawXmlContainer
    {
        protected override bool HasExportData { get { return true; } }
        protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
        {
            writer.WriteRaw(GetRawXml());
        }
        protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
        {
            this.SetData(fieldNode.InnerXml);
        }

        public string GetRawXml() //---- IRawXmlContainer Member
        {
            return (string)GetData();
        }

        protected override bool ParseValue(string value)
        {
            this.SetData(value);
            return true;
        }
    }
}

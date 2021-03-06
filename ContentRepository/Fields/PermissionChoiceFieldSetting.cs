using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Storage;
using ContentRepository.Storage.Schema;
using System.Xml.XPath;
using ContentRepository.Schema;

namespace ContentRepository.Fields
{
    public class PermissionChoiceFieldSetting : ChoiceFieldSetting
    {
        public const string VisiblePermissionCountName = "VisiblePermissionCount";
        private const int DefaultVisiblePermissionCount = 15;

        private int? _visiblePermissionCount;
        public int? VisiblePermissionCount
        {
            get
            {
                if (_visiblePermissionCount.HasValue)
                    return _visiblePermissionCount;

                return this.ParentFieldSetting == null ? null :
                    ((PermissionChoiceFieldSetting)this.ParentFieldSetting).VisiblePermissionCount;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting VisiblePermissionCount is not allowed within readonly instance.");
                _visiblePermissionCount = value;
            }
        }

        protected override void ParseConfiguration(System.Xml.XPath.XPathNavigator configurationElement, System.Xml.IXmlNamespaceResolver xmlNamespaceResolver, Schema.ContentType contentType)
        {
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);

            foreach (XPathNavigator node in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (node.LocalName)
                {
                    case VisiblePermissionCountName:
                        int visiblePermissionCount;
                        if (Int32.TryParse(node.InnerXml, out visiblePermissionCount))
                            _visiblePermissionCount = visiblePermissionCount;
                        break;
                }
            }

            _options = ActiveSchema.PermissionTypes.Select(t => new ChoiceOption(t.Id.ToString(), "$ Portal, Permission_" + t.Name)).Take(VisiblePermissionCount ?? DefaultVisiblePermissionCount).ToList();
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _visiblePermissionCount = GetConfigurationNullableValue<int>(info, VisiblePermissionCountName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(VisiblePermissionCountName, _visiblePermissionCount);
            return result;
        }
    }
}

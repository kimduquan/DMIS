using System;
using System.Globalization;
using System.Web.UI.WebControls.WebParts;
using ContentRepository.i18n;
using Diagnostics;

namespace Portal.UI.PortletFramework
{
    public class LocalizedWebDescriptionAttribute : WebDescriptionAttribute
    {
        private readonly string _className;
        private readonly string _key;

        public LocalizedWebDescriptionAttribute(string displayName) : base(displayName)
        {
        }
        public LocalizedWebDescriptionAttribute(string className, string key)
        {
            _className = className;
            _key = key;
        }

        public override string Description
        {
            get
            {
                if (!string.IsNullOrEmpty(_className) && !string.IsNullOrEmpty(_key))
                    DescriptionValue = ResourceManager.Current.GetString(_className, _key);
                
                return base.Description;
            }
        }
    }
}
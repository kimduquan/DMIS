using System;
using System.Globalization;
using System.Web.UI.WebControls.WebParts;
using ContentRepository.i18n;
using Diagnostics;
using System.Linq;

namespace Portal.UI.PortletFramework
{
    public sealed class LocalizedWebDisplayNameAttribute : WebDisplayNameAttribute
    {
        private readonly string _className;
        private readonly string _key;

        public LocalizedWebDisplayNameAttribute(string displayName) : base(displayName)
        {
        }

        public LocalizedWebDisplayNameAttribute(string className, string key)
        {
            _className = className;
            _key = key;
        }

        public override string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(_className) && !string.IsNullOrEmpty(_key))
                    DisplayNameValue = ResourceManager.Current.GetString(_className, _key);
                
                return base.DisplayName;
            }
        }
    }
}
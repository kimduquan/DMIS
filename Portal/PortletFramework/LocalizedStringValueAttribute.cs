using System;
using System.Globalization;
using ContentRepository.i18n;

namespace Portal.UI.PortletFramework
{
    /// <summary>
    /// This attribute is used to represent a string value
    /// for a value in an enum.
    /// </summary>
    public class LocalizedStringValueAttribute : Attribute
    {
        private readonly string _className;
        private readonly string _key;

        public LocalizedStringValueAttribute(string value)
        {
            this.StringValueInternal = value;
        }

        public LocalizedStringValueAttribute(string className, string key)
        {
            _className = className;
            _key = key;
        }

        protected string StringValueInternal { get; set; }

        public virtual string StringValue
        {
            get
            {
                if (!string.IsNullOrEmpty(_className) && !string.IsNullOrEmpty(_key))
                    StringValueInternal = ResourceManager.Current.GetString(_className, _key);
                
                return StringValueInternal;
            }
        }
    }
}
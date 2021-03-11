using System;
using System.Globalization;
using System.Resources;
using System.Web.Compilation;

namespace ContentRepository.i18n
{
    public class ResourceProvider : IResourceProvider
    {

        private string _className;

        public ResourceProvider(string className)
        {
            _className = className;
        }


        public object GetObject(string resourceKey, CultureInfo culture)
        {
            if (culture == null)
                culture = CultureInfo.CurrentUICulture;

            return ResourceManager.Current.GetObject(_className, resourceKey, culture);
        }

        public IResourceReader ResourceReader
        {
            get { throw new NotImplementedException(); }
        }

    }
}
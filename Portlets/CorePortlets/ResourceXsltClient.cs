using System;
using System.Globalization;
using Diagnostics;

namespace Portal.Portlets
{
    public class ResourceXsltClient
    {
        public string GetString(string className, string name)
        {
            var rm = ContentRepository.i18n.ResourceManager.Current;
            return rm.GetString(className, name);
        }

        public string GetString(string className, string name, string cultureName)
        {
            var rm = ContentRepository.i18n.ResourceManager.Current;
            CultureInfo cultureInfo = null;
            try
            {
                cultureInfo = CultureInfo.CreateSpecificCulture(cultureName);
            }
            catch (ArgumentException e) //logged
            {
                Logger.WriteException(e);
                return rm.GetString(className, name);
            }
            return rm.GetString(className, name, cultureInfo);
        }

    }
}

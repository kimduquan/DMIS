using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Compilation;

namespace ContentRepository.i18n
{
    public class ResourceProviderFactory : System.Web.Compilation.ResourceProviderFactory
    {
        /// <summary>
        /// Creates a resourceprovider with the specified classkey.
        /// </summary>
        /// <param name="classKey">Classkey holds the name of the resourcekey.</param>
		/// <returns>New ResourceProvider instance.</returns>
        public override IResourceProvider CreateGlobalResourceProvider(string classKey)
        {
            return new ResourceProvider(classKey);
        }
        /// <summary>
        /// Creates a resourceprovider with the specified virtualpath.
        /// </summary>
        /// <param name="virtualPath">Virtualpath holds the name of the virtualpath which is localized.</param>
		/// <returns>New ResourceProvider instance.</returns>
        public override IResourceProvider CreateLocalResourceProvider(string virtualPath)
        {
            return new ResourceProvider(virtualPath);
        }
    }
}
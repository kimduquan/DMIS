using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Preview
{
    public abstract class PreviewImageGenerator : IPreviewImageGenerator
    {
        public abstract string[] KnownExtensions { get; }
        public abstract void GeneratePreview(Stream docStream, IPreviewGenerationContext context);
        public virtual string GetTaskNameByExtension(string extension)
        {
            // means default
            return null;
        }
        public virtual string[] GetSupportedTaskNames()
        {
            // fallback to default task name, defined by the preview provider
            return null;
        }

        //====================================================================================================================

        private static object _loaderSync = new object();
        private static Dictionary<string, IPreviewImageGenerator> __providers;
        private static Dictionary<string, IPreviewImageGenerator> Providers
        {
            get
            {
                if (__providers == null)
                    lock (_loaderSync)
                        if (__providers == null)
                            __providers = CreateProviderPrototypes();
                return __providers;
            }
        }
        private static Dictionary<string, IPreviewImageGenerator> CreateProviderPrototypes()
        {
            var providers = new Dictionary<string, IPreviewImageGenerator>();
            var providerTypesA = TypeResolver.GetTypesByInterface(typeof(IPreviewImageGenerator));
            foreach (var providerType in providerTypesA)
            {
                if (providerType.IsAbstract)
                    continue;

                var provider = (IPreviewImageGenerator)Activator.CreateInstance(providerType);
                foreach (var extension in provider.KnownExtensions)
                {
                    IPreviewImageGenerator existing;
                    var ext = extension.ToLowerInvariant();
                    if (!providers.TryGetValue(ext, out existing))
                    {
                        if (providerType.IsInstanceOfType(existing))
                            continue;
                    }
                    providers[ext] = provider;
                }
            }
            return providers;
        }

        public static string GetTaskNameByFileNameExtension(string extension)
        {
            IPreviewImageGenerator provider = null;
            if (!Providers.TryGetValue(extension.ToLowerInvariant(), out provider))
                throw new ApplicationException(SR.F(SR.UnknownProvider_1, extension));
            return provider.GetTaskNameByExtension(extension);
        }

        public static string[] GetSupportedCustomTaskNames()
        {
            // collect all suppotred task names from the different generator implementations
            return Providers.Values.Select(pig => pig.GetSupportedTaskNames())
                .Where(tnames => tnames != null)
                .SelectMany(tnames => tnames)
                .Where(tn => !string.IsNullOrEmpty(tn))
                .Distinct().OrderBy(tn => tn).ToArray();
        }

        public static bool IsSupportedExtension(string extension)
        {
            return Providers.ContainsKey(extension.ToLowerInvariant());
        }

        public static void GeneratePreview(string extension, Stream docStream, IPreviewGenerationContext context)
        {
            IPreviewImageGenerator provider = null;
            if (!Providers.TryGetValue(extension.ToLowerInvariant(), out provider))
                throw new ApplicationException(SR.F(SR.UnknownProvider_1, extension));
            provider.GeneratePreview(docStream, context);
        }

    }
}

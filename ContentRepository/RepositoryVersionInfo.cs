using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using System.Text;
using ContentRepository.Storage;
using System.Reflection;
using ContentRepository.Storage.Data;
using Communication.Messaging;

namespace ContentRepository
{
    public class AssemblyDetails
    {
        public AssemblyInfo[] DMIS { get; set; }
        public AssemblyInfo[] Plugins { get; set; }
        public AssemblyInfo[] GAC { get; set; }
        public AssemblyInfo[] Other { get; set; }
        public AssemblyInfo[] Dynamic { get; set; }
    }
    public class RepositoryVersionInfo
    {

        public ApplicationInfo OfficialVersion { get; private set; }
        public IEnumerable<ApplicationInfo> Applications { get; private set; }
        public AssemblyDetails Assemblies { get; private set; }
        public IEnumerable<Package> InstalledPackages{ get; private set;}

        //============================================================== Static part

        private static RepositoryVersionInfo __instance;
        private static object _instanceLock = new object();
        public static RepositoryVersionInfo Instance
        {
            get
            {
                if (__instance == null)
                    lock (_instanceLock)
                        if (__instance == null)
                            __instance = Create();
                return __instance;
            }
        }

        private static RepositoryVersionInfo Create()
        {
            return Create(
                DataProvider.Current.LoadOfficialVersion(),
                DataProvider.Current.LoadInstalledApplications(),
                DataProvider.Current.LoadInstalledPackages());
        }

        private static RepositoryVersionInfo Create(ApplicationInfo productVersion, IEnumerable<ApplicationInfo> applicationVersions, IEnumerable<Package> packages)
        {
            var asms = TypeHandler.GetAssemblyInfo();

            var sncr = Assembly.GetExecutingAssembly();
            var binPath = IO.Path.GetDirectoryName(TypeHandler.GetCodeBase(sncr));

            var asmDyn = asms.Where(a => a.IsDynamic).ToArray();
            asms = asms.Except(asmDyn).ToArray();
            var asmInBin = asms.Where(a => a.CodeBase.StartsWith(binPath)).ToArray();
            asms = asms.Except(asmInBin).ToArray();
            var asmInGac = asms.Where(a => a.CodeBase.Contains("\\GAC")).ToArray();
            asms = asms.Except(asmInGac).ToArray();

            var asmSn = asmInBin.Where(a => a.Name.StartsWith("")).ToArray();
            var plugins = asmInBin.Except(asmSn).ToArray();

            return new RepositoryVersionInfo
            {
                OfficialVersion = productVersion,
                Applications = applicationVersions,
                Assemblies = new AssemblyDetails
                {
                    DMIS = asmSn,
                    Plugins = plugins,
                    GAC = asmInGac,
                    Other = asms,
                    Dynamic = asmDyn,
                },
                InstalledPackages = packages
            };
        }

        public static void SetInitialVersion(ApplicationInfo productVersion)
        {
            // create an in-memory, initial version
            __instance = Create(productVersion, new List<ApplicationInfo>(), new List<Package>());
        }

        public static void Reset()
        {
            new RepositoryVersionInfoResetDistributedAction().Execute();
        }
        private static void ResetPrivate()
        {
            __instance = null;
        }

        [Serializable]
        internal sealed class RepositoryVersionInfoResetDistributedAction : DistributedAction
        {
            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                RepositoryVersionInfo.ResetPrivate();
            }
        }
    }
}

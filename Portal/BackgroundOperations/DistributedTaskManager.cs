using System.Collections.Generic;
using System.IO;
using System;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using ContentRepository;
using ContentRepository.Storage;
using ContentRepository.Storage.Data;
using Diagnostics;
using Version = System.Version;

namespace BackgroundOperations
{
    internal class DistributedTaskManager : TaskManagerBase
    {
        // This class uses the same logic for communication with the agents (implemented 
        // in the base class) as the local provider. It is responsible for creating an
        // automatic update package for Task Management agents if there is a new version.

        public override bool Distributed { get { return true; } }
        public override void Start() { }
        public override void Stop() { }

        private static readonly string TASKEXECUTORS_FOLDERNAME = "TaskExecutors";

        private static Dictionary<string, Version> _executorVersions;
        private static object _executorLock = new object();
        protected static Dictionary<string, Version> TaskExecutorVersions
        {
            get
            {
                if (_executorVersions == null)
                {
                    lock (_executorLock)
                    {
                        if (_executorVersions == null)
                        {
                            var versions = new Dictionary<string, Version>();
                            var executorsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TASKMANAGEMENT_FOLDERNAME, TASKEXECUTORS_FOLDERNAME);

                            foreach (var directory in Directory.GetDirectories(executorsPath))
                            {
                                var executorName = Path.GetFileName(directory);
                                var executorPath = Path.Combine(directory, executorName + ".exe");
                                if (!System.IO.File.Exists(executorPath))
                                    continue;

                                try
                                {
                                    // load the executor assembly name and get the version
                                    var an = AssemblyName.GetAssemblyName(executorPath);

                                    versions.Add(executorName, an.Version);
                                }
                                catch
                                {
                                    // error loading an executor, simply leave it out
                                }
                            }

                            _executorVersions = versions;
                        }
                    }
                }

                return _executorVersions;
            }
        }

        public override bool IsUpdateAvailable(Version currentVersion, Dictionary<string, Version> executorVersions)
        {
            // in most cases there will be no new version
            if (RepositoryVersionInfo.Instance.OfficialVersion.Version.CompareTo(currentVersion) <= 0 && !IsNewExecutorAvailable(executorVersions))
                return false;

            // if a package content exists with the correct version number, it means it is ready to be downloaded
            if (PackageExists())
                return true;

            // There is no package content or it has an old, obsolete version number. We need to generate 
            // a new package, but only on a background thread so that the next update checker call will 
            // have the new package file ready.
            // ------------------------------------------------------------------------------------------
            // We MUST NOT return 'true', until we are certain that the package is ready to be downloaded.

            System.Threading.Tasks.Task.Run(() => CreatePackageContent());

            return false;
        }

        private static bool IsNewExecutorAvailable(Dictionary<string, Version> executorVersions)
        {
            // Do not take the count of executors into account: it is possible that an agent does not
            // have all the executors installed. In that case those executors will not be distributed
            // on those agents.

            //if (executorVersions.Count < TaskExecutorVersions.Count)
            //    return true;

            foreach (var ev in executorVersions)
            {
                Version version;
                if (TaskExecutorVersions.TryGetValue(ev.Key, out version) && version > ev.Value)
                    return true;
            }

            return false;
        }

        private static object _packageLock = new object();

        private static void CreatePackageContent()
        {
            try
            {
                // condition-lock-condition pattern
                if (PackageExists())
                    return;

                lock (_packageLock)
                {
                    if (PackageExists())
                        return;

                    var version = RepositoryVersionInfo.Instance.OfficialVersion.Version.ToString();

                    Logger.WriteInformation(EventId.UpdateGeneral, "TaskManagement update package generation STARTED for version " + version, 
                        properties: new Dictionary<string, object> { { "Path", AgentManager.UPDATER_PACKAGEPATH} });

                    // load or create parent
                    var packageParent = Content.Load(AgentManager.UPDATER_PACKAGEPARENTPATH) ??
                                        Tools.CreateStructure(AgentManager.UPDATER_PACKAGEPARENTPATH, "SystemFolder");

                    // overwrite previous package if exists
                    var packageContent = Content.Load(AgentManager.UPDATER_PACKAGEPATH);
                    if (packageContent != null)
                    {
                        if (!packageContent.ContentType.IsInstaceOfOrDerivedFrom("File"))
                        {
                            // cleanup a wrongly type content if there is one
                            packageContent.ForceDelete();
                            packageContent = null;

                            Logger.WriteWarning(EventId.UpdateGeneral,
                                "Previous package file deleted because of a content type mismatch. The package has to be a file.");
                        }
                        else if (packageContent.ContentHandler.SavingState != ContentSavingState.Finalized)
                        {
                            // cleanup the content if it is partially fininshed
                            packageContent.ForceDelete();
                            packageContent = null;
                        }
                    }

                    if (packageContent == null)
                        packageContent = Content.CreateNew("File", packageParent.ContentHandler, AgentManager.UPDATER_PACKAGENAME);

                    // we store the version information in the Description field
                    packageContent["Description"] = GetVersionHash(_executorVersions);

                    // building and saving a package may be a time- and memory-consuming process, we do it in chunks
                    packageContent.Save(SavingMode.StartMultistepSave);
                    
                    SavePackageToContent(packageContent.Id);

                    packageContent.FinalizeContent();

                    Logger.WriteInformation(EventId.UpdateGeneral, "TaskManagement update package generation FINISHED for version " + version, 
                        properties: new Dictionary<string, object> { { "Path", AgentManager.UPDATER_PACKAGEPATH} });
                }
            }
            catch(Exception ex)
            {
                Logger.WriteError(EventId.UpdateError, "Error during Task Management update package generation. " + ex);
            }
        }

        private static void SavePackageToContent(int contentId)
        {
            var chunkToken = BinaryData.StartChunk(contentId);
            long packageStreamSize = 0;

            using (var packageStream = GeneratePackageStream())
            {
                if (packageStream == null)
                    return;

                packageStreamSize = packageStream.Length;
                if (packageStreamSize == 0)
                    return;

                long savedByteCount = 0;
                long chunkSize = RepositoryConfiguration.BinaryChunkSize;

                while (savedByteCount < packageStreamSize)
                {
                    //the last part may be smaller
                    if (savedByteCount + chunkSize > packageStreamSize)
                        chunkSize = packageStreamSize - savedByteCount;

                    // always allocate a buffer of a correct size, instead of reusing one
                    var buffer = new byte[chunkSize];

                    packageStream.Read(buffer, 0, (int)chunkSize);

                    BinaryData.WriteChunk(contentId, chunkToken, packageStreamSize, buffer, savedByteCount);

                    savedByteCount += chunkSize;
                }
            }

            BinaryData.CommitChunk(contentId, chunkToken, packageStreamSize, binaryMetadata: new BinaryData { FileName = AgentManager.UPDATER_PACKAGENAME });
        }

        private static Stream GeneratePackageStream()
        {
            var taskManagementPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TASKMANAGEMENT_FOLDERNAME);

            // no task management folder found under the web folder
            if (!Directory.Exists(taskManagementPath))
                return null;

            using (var outputStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(outputStream, ZipArchiveMode.Create))
                {
                    CompressDirectory(taskManagementPath, archive, taskManagementPath);
                }

                // do not use stream.Copy here, because the ZipArchive entry would not be finalized
                return new MemoryStream(outputStream.ToArray());
            }
        }

        private static void CompressDirectory(string directory, ZipArchive archive, string rootFolderPath)
        {
            // iterate through files in the directory
            CompressFiles(Directory.GetFiles(directory), archive, rootFolderPath);

            foreach (var subDirectory in Directory.GetDirectories(directory))
            {
                // recursive call to subdirectories
                CompressDirectory(subDirectory, archive, rootFolderPath);
            }
        }

        private static void CompressFiles(IEnumerable<string> files, ZipArchive archive, string rootFolderPath)
        {
            foreach (var filePath in files.Where(f => AllowedFile(f)))
            {
                // entry name is package-relative: 'folder1\file.txt'
                var entry = archive.CreateEntry(filePath.Substring(rootFolderPath.Length + 1));

                using (var entryStream = entry.Open())
                {
                    using (var fileStream = System.IO.File.OpenRead(filePath))
                    {
                        fileStream.CopyTo(entryStream);
                    }
                }
            }
        }

        private static bool AllowedFile(string path)
        {
            var fileName = Path.GetFileName(path);

            // skip files related to the updater tool
            if (fileName.StartsWith(AgentManager.UPDATER_PROCESSNAME, StringComparison.OrdinalIgnoreCase))
                return false;

            // skip the package file itself, if it exists in the task management folder (e.g. because
            // distributed task management is switched on in a local environment)
            if (string.Compare(fileName, AgentManager.UPDATER_PACKAGENAME, StringComparison.OrdinalIgnoreCase) == 0)
                return false;

            return true;
        }

        private static bool PackageExists()
        {
            var packageContent = Node.Load<GenericContent>(AgentManager.UPDATER_PACKAGEPATH);
            var versionHash = GetVersionHash(TaskExecutorVersions);

            // a package 'exists' if the content is finalized and contains the correct version numbers in the Description field
            return packageContent != null && 
                packageContent.SavingState == ContentSavingState.Finalized &&
                string.Compare(packageContent.Description, versionHash, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static string GetVersionHash(Dictionary<string, Version> executorVersions)
        {
            // compile product version, executor names and versions into a single string: '6.3.1.1234;PreviewGenerator,1.1.0;...'
            return string.Format("{0};{1}", 
                RepositoryVersionInfo.Instance.OfficialVersion.Version,
                string.Join(";", executorVersions.OrderBy(ev => ev.Key).Select(ev => string.Format("{0},{1}", ev.Key, ev.Value))));
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using Ionic.Zip;
using System.Configuration;
using System.Xml;

namespace Tools.SnAdmin
{
    class SnAdmin
    {
        #region Constants
        private const string RUNTIMEEXENAME = "SnAdminRuntime.exe";
        private const string SANDBOXDIRECTORYNAME = "run";
        private static string ToolTitle = "Admin ";
        private static string ToolName = "SnAdmin";
        internal static readonly string ParameterRegex = @"^(PHASE\d+\.)?(STEP\d+)(\.[\w_]+)?:";

        private static string CR = Environment.NewLine;
        private static string UsageScreen = String.Concat(
            //         1         2         3         4         5         6         7         8
            //12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
            CR,
            "Usage:", CR,
            ToolName, " <package> [<target>]", CR,
            CR,
            "Parameters:", CR,
            "<package>: File contains a package (*.zip or directory).", CR,
            "<target>: Directory contains web folder of a stopped  instance.", CR
        );
        #endregion

        static int Main(string[] args)
        {
            ToolTitle += Assembly.GetExecutingAssembly().GetName().Version;
            if (args.FirstOrDefault(a => a.ToUpper() == "-WAIT") != null)
            {
                Console.WriteLine("Running in wait mode - now you can attach to the process with a debugger.");
                Console.WriteLine("Press ENTER to continue.");
                Console.ReadLine();
            }

            string packagePath;
            string targetDirectory;
            string logFilePath;
            LogLevel logLevel;
            bool help;
            bool schema;
            bool wait;
            string[] parameters;

            if (!ParseParameters(args, out packagePath, out targetDirectory/*, out phase*/, out parameters, out logFilePath, out logLevel, out help, out schema, out wait))
                return -1;
            if (!CheckTargetDirectory(targetDirectory))
                return -1;

            if (!CheckPackage(ref packagePath))
                return -1;

            Logger.PackageName = Path.GetFileName(packagePath);

            Logger.Create(logLevel, logFilePath);
            Debug.WriteLine("##> " + Logger.Level);

            return ExecuteGlobal(packagePath, targetDirectory, parameters, help, schema, wait);
        }
        private static bool ParseParameters(string[] args, out string packagePath, out string targetDirectory, out string[] parameters, out string logFilePath, out LogLevel logLevel, out bool help, out bool schema, out bool wait)
        {
            packagePath = null;
            targetDirectory = null;
            logFilePath = null;
            wait = false;
            help = false;
            schema = false;
            logLevel = LogLevel.Default;
            var prms = new List<string>();

            foreach (var arg in args)
            {
                if (IsValidParameter(arg))
                {
                    prms.Add(arg);
                    continue;
                }

                if (arg.StartsWith("-"))
                {
                    var verb = arg.Substring(1).ToUpper();
                    switch (verb)
                    {
                        case "?": help = true; break;
                        case "HELP": help = true; break;
                        case "SCHEMA": schema = true; break;
                        case "WAIT": wait = true; break;
                    }
                }
                else if (arg.StartsWith("LOG:", StringComparison.OrdinalIgnoreCase))
                {
                    logFilePath = arg.Substring(4);
                }
                else if (arg.StartsWith("LOGLEVEL:", StringComparison.OrdinalIgnoreCase))
                {
                    logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), arg.Substring(9));
                }
                else if (packagePath == null)
                {
                    packagePath = arg;
                }
                else
                {
                    targetDirectory = arg;
                }
            }
            if (targetDirectory == null)
                targetDirectory = SearchTargetDirectory();
            parameters = prms.ToArray();
            return true;
        }
        private static bool IsValidParameter(string parameter)
        {
            return System.Text.RegularExpressions.Regex.Match(parameter, ParameterRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success;
        }
        private static bool CheckTargetDirectory(string targetDirectory)
        {
            if (Directory.Exists(targetDirectory))
                return true;
            PrintParameterError("Given target directory does not exist: " + targetDirectory);
            return false;
        }
        private static bool CheckPackage(ref string packagePath)
        {
            if (packagePath == null)
            {
                PrintParameterError("Missing package");
                return false;
            }

            if (!Path.IsPathRooted(packagePath))
                packagePath = Path.Combine(DefaultPackageDirectory(), packagePath);

            if (packagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                if (!System.IO.File.Exists(packagePath))
                {
                    PrintParameterError("Given package file does not exist: " + packagePath);
                    return false;
                }
            }
            else
            {
                if (!Directory.Exists(packagePath))
                {
                    var packageZipPath = packagePath + ".zip";
                    if (!System.IO.File.Exists(packageZipPath))
                    {
                        PrintParameterError("Given package zip file or directory does not exist: " + packagePath);
                        return false;
                    }
                    else
                    {
                        packagePath = packageZipPath;
                    }
                }
            }
            return true;
        }
        private static void PrintParameterError(string message)
        {
            Console.WriteLine(ToolTitle);
            Console.WriteLine(message);
            Console.WriteLine(UsageScreen);
            Console.WriteLine("Aborted.");
        }

        private static int ExecuteGlobal(string packagePath, string targetDirectory, string[] parameters, bool help, bool schema, bool wait)
        {
            Console.WriteLine();

            Logger.LogTitle(ToolTitle);
            Logger.LogWriteLine("Start at {0}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            Logger.LogWriteLine("Target:  " + targetDirectory);
            Logger.LogWriteLine("Package: " + packagePath);

            packagePath = Unpack(packagePath);

            var result = 0;
            var phase = 0;
            var errors = 0;
            while (true)
            {
                var workerExe = CreateSandbox(targetDirectory, Path.GetDirectoryName(packagePath));
                var appBasePath = Path.GetDirectoryName(workerExe);
                var workerDomain = AppDomain.CreateDomain(ToolName + "WorkerDomain" + phase, null, appBasePath, null, false);

                var phaseParameter = "PHASE:" + phase.ToString();
                var logParameter = "LOG:" + Logger.GetLogFileName();
                var logLevelParameter = "LOGLEVEL:" + Logger.Level.ToString();

                var prms = new List<string> { packagePath, targetDirectory, phaseParameter, logParameter, logLevelParameter };
                prms.AddRange(parameters);
                if (help)
                    prms.Add("-HELP");
                if (wait)
                    prms.Add("-WAIT");
                if (schema)
                    prms.Add("-SCHEMA");
                 
                var processArgs =  string.Join(" ", prms);
                var startInfo = new ProcessStartInfo(workerExe, processArgs)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(workerExe),
                    CreateNoWindow = false,
                };

                Process process;
                try
                {
                    process = Process.Start(startInfo);
                    process.WaitForExit();
                    result = process.ExitCode;
                }
                catch (Exception e)
                {
                    var preExMessage = GetPackagePreconditionExceptionMessage(e);
                    if (preExMessage != null)
                    {
                        Logger.LogWriteLine("PRECONDITION FAILED:");
                        Logger.LogWriteLine(preExMessage);
                    }
                    else
                    {
                        var pkgExMessage = GetInvalidPackageExceptionMessage(e);
                        if (pkgExMessage != null)
                        {
                            Logger.LogWriteLine("INVALID PACKAGE:");
                            Logger.LogWriteLine(pkgExMessage);
                        }
                        else
                        {
                            Logger.LogWriteLine("#### UNHANDLED EXCEPTION:");
                            Logger.LogException(e);
                        }
                    }
                    result = -1;
                }
                if (result > 0)
                {
                    errors += (result & -2) / 2;
                    result = result & 1;
                }

                if (result < 1)
                    break;

                phase++;

                // wait for the file system to release everything
                Thread.Sleep(2000);
            }


            Logger.LogWriteLine("===============================================================================");
            if (result == -1)
                Logger.LogWriteLine(ToolName + " terminated with warning.");
            else if (result < -1)
                Logger.LogWriteLine(ToolName + " stopped with error.");
            else if (errors == 0)
                Logger.LogWriteLine(ToolName + " has been successfully finished.");
            else
                Logger.LogWriteLine(ToolName + " has been finished with {0} errors.", errors);

            var msgLevel = MessageLevel.Success;
            if (result == -1)
                msgLevel = MessageLevel.Warning;
            else if (result < -1 || errors != 0)
                msgLevel = MessageLevel.Error;
            WriteMessage(packagePath, msgLevel);

            Console.WriteLine("See log file: {0}", Logger.GetLogFileName());
            if (Debugger.IsAttached)
            {
                Console.Write("[press any key] ");
                Console.ReadKey();
                Console.WriteLine();
            }
            return result;
        }
        private static string GetPackagePreconditionExceptionMessage(Exception e)
        {
            if (e.GetType().Name == "PackagePreconditionException")
                return e.Message;
            e = e.InnerException;
            if (e.GetType().Name == "PackagePreconditionException")
                return e.Message;
            return null;
        }
        private static string GetInvalidPackageExceptionMessage(Exception e)
        {
            if (e.GetType().Name == "InvalidPackageException")
                return e.Message;
            e = e.InnerException;
            if (e.GetType().Name == "InvalidPackageException")
                return e.Message;
            return null;
        }

        private enum MessageLevel { Success, Warning, Error }

        private static void WriteMessage(string packagePath, MessageLevel level)
        {
            var files = Directory.GetFiles(packagePath);
            if (files.Length != 1)
                return;

            var manifestXml = new XmlDocument();
            manifestXml.Load(files[0]);

            var elementName = string.Empty;
            switch (level)
            {
                case MessageLevel.Success: elementName = "SuccessMessage"; break;
                case MessageLevel.Warning: elementName = "WarningMessage"; break;
                case MessageLevel.Error: elementName = "ErrorMessage"; break;
                default: throw new NotSupportedException("Unknown level: " + level);
            }
            var msgElement = (XmlElement)manifestXml.DocumentElement.SelectSingleNode(elementName);
            if (msgElement == null)
                return;

            var msg = msgElement.InnerText;

            var backgroundColorBackup = Console.BackgroundColor;
            var foregroundColorBackup = Console.ForegroundColor;
            switch (level)
            {
                case MessageLevel.Success:
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case MessageLevel.Warning:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case MessageLevel.Error:
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                default:
                    throw new NotSupportedException("Unknown level: " + level);
            }

            Logger.LogWrite(msg);
            Console.BackgroundColor = backgroundColorBackup;
            Console.ForegroundColor = foregroundColorBackup;
            Logger.LogWriteLine(string.Empty);
        }

        private static string SearchTargetDirectory()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDirectory"];
            if (!string.IsNullOrEmpty(targetDir))
                return targetDir;

            // default location: ..\webfolder\Admin\bin
            var workerExe = Assembly.GetExecutingAssembly().Location;
            var path = workerExe;

            // go up on the parent chain
            path = Path.GetDirectoryName(path);
            path = Path.GetDirectoryName(path);

            // get the name of the container directory (should be 'Admin')
            var adminDirName = Path.GetFileName(path);
            path = Path.GetDirectoryName(path);

            if (string.Compare(adminDirName, "Admin", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // look for the web.config
                if (System.IO.File.Exists(Path.Combine(path, "web.config")))
                    return path;
            }
            throw new ApplicationException("Configure the TargetPath. This path does not exist or not a valid target: " + path);
        }
        private static string DefaultPackageDirectory()
        {
            var pkgDir = ConfigurationManager.AppSettings["PackageDirectory"];
            if (!string.IsNullOrEmpty(pkgDir))
                return pkgDir;
            var workerExe = Assembly.GetExecutingAssembly().Location;
            pkgDir = Path.GetDirectoryName(Path.GetDirectoryName(workerExe));
            return pkgDir;
        }

        private static string CreateSandbox(string targetDirectory, string packageDirectory)
        {
            var sandboxPath = EnsureEmptySandbox(packageDirectory);
            var webBinPath = Path.Combine(targetDirectory, "bin");

            // #1 copy assemblies from webBin to sandbox
            var paths = GetRelevantFiles(webBinPath);
            foreach (var filePath in paths)
                File.Copy(filePath, Path.Combine(sandboxPath, Path.GetFileName(filePath)));

            // #2 copy missing files from Tools directory
            var toolsDir = Path.Combine(targetDirectory, "Tools");
            var toolPaths = GetRelevantFiles(toolsDir);
            var missingNames = toolPaths.Select(p => Path.GetFileName(p))
                .Except(paths.Select(q => Path.GetFileName(q))).OrderBy(r => r)
                .Where(r => !r.ToLower().Contains(".vshost.exe"))
                .ToArray();
            foreach (var fileName in missingNames)
                File.Copy(Path.Combine(toolsDir, fileName), Path.Combine(sandboxPath, fileName));

            // #3 return with path of the worker exe
            return Path.Combine(sandboxPath, RUNTIMEEXENAME);
        }
        private static string[] _relevantExtensions = ".dll;.exe;.pdb;.config".Split(';');
        private static string[] GetRelevantFiles(string dir)
        {
            return Directory.EnumerateFiles(dir, "*.*").Where(p => _relevantExtensions.Contains(Path.GetExtension(p).ToLower())).ToArray();
        }
        private static string EnsureEmptySandbox(string packagesDirectory)
        {
            var sandboxFolder = Path.Combine(packagesDirectory, SANDBOXDIRECTORYNAME);
            if (!Directory.Exists(sandboxFolder))
                Directory.CreateDirectory(sandboxFolder);
            else
                DeleteAllFrom(sandboxFolder);
            return sandboxFolder;
        }
        private static void DeleteAllFrom(string sandboxFolder)
        {
            var sandboxInfo = new DirectoryInfo(sandboxFolder);
            foreach (FileInfo file in sandboxInfo.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in sandboxInfo.GetDirectories())
                dir.Delete(true);
        }

        private static string Unpack(string package)
        {
            if (Directory.Exists(package))
                return package;

            var pkgFolder = Path.GetDirectoryName(package);
            var zipTarget = Path.Combine(pkgFolder, Path.GetFileNameWithoutExtension(package));

            Logger.LogWriteLine("Package directory: " + zipTarget);

            if (Directory.Exists(zipTarget))
            {
                DeleteAllFrom(zipTarget);
                Logger.LogWriteLine("Old files and directories are deleted.");
            }
            else
            {
                Directory.CreateDirectory(zipTarget);
                Logger.LogWriteLine("Package directory created.");
            }

            Logger.LogWriteLine("Extracting ...");
            using (ZipFile zip = ZipFile.Read(package))
            {
                foreach (var e in zip.Entries)
                    e.Extract(zipTarget);
            }
            Logger.LogWriteLine("Ok.");

            return zipTarget;
        }

    }
}

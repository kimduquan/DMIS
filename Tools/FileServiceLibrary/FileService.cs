using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using uno;
using uno.util;
using unoidl.com.sun.star.beans;
using unoidl.com.sun.star.frame;
using unoidl.com.sun.star.lang;

namespace FileServiceLibrary
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class FileService : IFileService
    {
        public void ConvertToPDF(string inputFile, string outputFile)
        {
            if (ConvertExtensionToFilterType(Path.GetExtension(inputFile)) == null)
                throw new InvalidProgramException("Unknown file type for File Service. File = " + inputFile);

            Process p = StartFileService();
            //Get a ComponentContext
            var xLocalContext = Bootstrap.bootstrap();
            //Get MultiServiceFactory
            var xRemoteFactory = (XMultiServiceFactory)xLocalContext.getServiceManager();

            //Get a CompontLoader
            var aLoader = (XComponentLoader)xRemoteFactory.createInstance("com.sun.star.frame.Desktop");
            //Load the sourcefile

            XComponent xComponent = null;
            try
            {
                xComponent = InitDocument(aLoader,
                                          PathConverter(inputFile), "_blank");
                //Wait for loading
                while (xComponent == null)
                {
                    Thread.Sleep(1000);
                }

                // save/export the document
                SaveDocument(xComponent, inputFile, PathConverter(outputFile));
            }
            finally
            {
                if (xComponent != null) xComponent.dispose();
                p.Close();
                //p.Kill();
                //p.Dispose();
            }
        }

        protected static string ConvertExtensionToFilterType(string extension)
        {
            switch (extension)
            {
                case ".doc":
                case ".docx":
                case ".txt":
                case ".rtf":
                case ".html":
                case ".htm":
                case ".xml":
                case ".odt":
                case ".wps":
                case ".wpd":
                    return "writer_pdf_Export";
                case ".xls":
                case ".xlsb":
                case ".xlsx":
                case ".ods":
                    return "calc_pdf_Export";
                case ".ppt":
                case ".pptx":
                case ".odp":
                    return "impress_pdf_Export";

                default:
                    return null;
            }
        }
        private static void SaveDocument(XComponent xComponent, string sourceFile, string destinationFile)
        {

            var propertyValues = new PropertyValue[2];
            // Setting the flag for overwriting
            propertyValues[1] = new PropertyValue { Name = "Overwrite", Value = new Any(true) };
            //// Setting the filter name
            propertyValues[0] = new PropertyValue
            {
                Name = "FilterName",
                Value = new Any(ConvertExtensionToFilterType(Path.GetExtension(sourceFile)))
            };
            ((XStorable)xComponent).storeToURL(destinationFile, propertyValues);
        }
        protected static XComponent InitDocument(XComponentLoader aLoader, string file, string target)
        {
            var openProps = new PropertyValue[1];
            openProps[0] = new PropertyValue { Name = "Hidden", Value = new Any(true) };

            var xComponent = aLoader.loadComponentFromURL(
                file, target, 0,
                openProps);

            return xComponent;
        }
        private static Process StartFileService()
        {
            var ps = Process.GetProcessesByName("soffice.exe");
            if (ps.Length != 0)
                throw new InvalidProgramException("File Service not found.  Is File Service installed?");
            if (ps.Length > 0)
                return null;
            var p = new Process
            {
                StartInfo =
                {
                    Arguments = "-headless -nologo -nodefault -invisible -nofirststartwizard -norestore -accept=socket,host=127.0.0.1,port=8100;urp",
                    FileName = FileServicePath,
                    CreateNoWindow = true
                }
            };
            var result = p.Start();
            if (result == false)
            {
                throw new InvalidProgramException("File Service failed to start.");
            }

            return p;
        }
        private static string PathConverter(string file)
        {
            if (string.IsNullOrEmpty(file))
                throw new NullReferenceException("Null or empty path passed to File Service");

            return String.Format("file:///{0}", file.Replace(@"\", "/"));
        }

        private static string FileServicePath { get; set; }
        public static void InitializeSettingValues()
        {
            FileServicePath = ConfigurationManager.AppSettings["FileServicePath"];
        }


        public bool IsSupportedExtension(string extension)
        {
            switch (extension)
            {
                case ".doc":
                case ".docx":
                case ".txt":
                case ".rtf":
                case ".html":
                case ".htm":
                case ".xml":
                case ".odt":
                case ".wps":
                case ".wpd":
                case ".xls":
                case ".xlsb":
                case ".xlsx":
                case ".ods":
                case ".ppt":
                case ".pptx":
                case ".odp":
                    return true;

                default:
                    return false;
            }
        }

        public Size GetSuggestImageSize(string extension, bool isThumbnail)
        {
            string temp = ConvertExtensionToFilterType(extension);
            Size result = new Size();
            if (isThumbnail == true)
            {
                result.Width = 200;
                result.Height = 200;
            }
            else
            {
                result.Width = 1240;
                result.Height = 1754;
            }
            if (temp == null)
                return result;
            switch (temp)
            {
                case "writer_pdf_Export":
                    result.Width = 800;
                    result.Height = 870;
                    break;
                case "calc_pdf_Export":
                    result.Width = 1000;
                    result.Height = 750;
                    break;
                case "impress_pdf_Export":
                    result.Width = 1280;
                    result.Height = 960;
                    break;
                default:
                    break;
            }
            return result;
        }
    }
}

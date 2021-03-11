using ImageMagick;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;

namespace DocumentPreviewGenerator
{
    class Program
    {
        public static void ConvertToPdf(string inputFile, string outputFile)
        {

            FileService.FileServiceClient client = new FileService.FileServiceClient();
            client.ConvertToPDF(inputFile, outputFile);
            client.Close();
        }
        public static int ConvertPdfToImages(string inputFile, string outputPath, string fileNameFormat, int width, int height, bool isThumbnail)
        {
            MagickReadSettings settings = new MagickReadSettings();
            // Settings the density to 300 dpi will create an image with a better quality
            settings.Density = new PointD(300, 300);
            settings.Width = width;
            settings.Height = height;
            int page = 0;
            using (MagickImageCollection images = new MagickImageCollection())
            {
                // Add all the pages of the pdf file to the collection
                images.Read(inputFile, settings);

                foreach (MagickImage image in images)
                {
                    // Write page to file that contains the page number
                    image.Format = MagickFormat.Png;
                    image.Write(outputPath + string.Format(fileNameFormat, page + 1) + ".png");
                    page++;
                }
            }
            return page;
        }
        private static string TemporaryDirPath { get; set; }
        private static void InitializeSettingValues()
        {
            TemporaryDirPath = ConfigurationManager.AppSettings["TempDirPath"];
        }
        static void Main(string[] args)
        {
            if (!ParseParameters(args))
            {
                //Logger.WriteWarning("Task process arguments are not correct.");
                return;
            }

            try
            {

                InitializeSettingValues();
                ExecuteTask();
            }
            catch (Exception ex)
            {
                //Logger.WriteError(ex);

                // if logger writes a standard error information to the console,
                // the task finalizer receives an SnTaskError instance with the
                // type, message and stack trace of the catched exception.
                // The simplest way of the exception writing is the following line:
                //Console.WriteLine("ERROR:" + SnTaskError.Create(ex).ToString());

            }
        }
        private static void ExecuteTask()
        {
            Console.WriteLine("Progress: Execution started.");
            string lockObj = "";
            string previousLockObj = "";
            using (WebClient client = new WebClient())
            {
                try
                {
                    if (string.IsNullOrEmpty(UserName))
                    {
                        client.UseDefaultCredentials = true;
                    }
                    else
                    {
                        client.UseDefaultCredentials = false;
                        client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(UserName + ":" + Password)));
                    }
                    string metadataString = "";
                    lockObj = CreateLockObject(Id, "GetMetatadata");
                    if (IsLocked(lockObj))
                    {
                        return;
                    }
                    else
                    {
                        DoLock(lockObj);
                        metadataString = GetCachedDataFromLockObject(lockObj);
                        if (metadataString == null)
                        {
                            metadataString = client.DownloadString(Repo + ODATA_SERVICE_PATH + string.Format(GET_CONTENT_BY_ID_REQUEST_FORMAT, Id));
                            SaveCacheDataInLockObject(lockObj, metadataString);
                        }
                        else
                        {
                            DeleteCachedDataInLockObject(lockObj);
                        }
                        previousLockObj = lockObj;
                        ReleaseLock(lockObj);
                    }

                    JsonSerializerSettings settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
                    JsonSerializer serializer = JsonSerializer.Create(settings);
                    StringReader reader = new StringReader(metadataString);
                    //DeleteCachedDataInLockObject(lockObj);
                    JsonTextReader jreader = new JsonTextReader(reader);
                    StringBuilder builder = new StringBuilder();
                    StringWriter writer = new StringWriter(builder);
                    JsonTextWriter jwriter = new JsonTextWriter(writer);
                    JObject metadata = serializer.Deserialize(jreader) as JObject;

                    string documentName = metadata["d"]["Name"].ToString();
                    string fileExtension = documentName.Substring(documentName.LastIndexOf("."));

                    
                    lockObj = CreateLockObject(Id, "isSupported");
                    bool isSupportedExt = false;
                    if (IsLocked(lockObj))
                        return;
                    else
                    {
                        DoLock(lockObj);
                        DeleteCachedDataInLockObject(previousLockObj);
                        isSupportedExt = isSupported(fileExtension);
                        if (isSupportedExt == false && fileExtension.ToLower() != ".pdf")
                        {
                            ReleaseLock(lockObj);
                            return;
                        }
                        ReleaseLock(lockObj);
                    }
                   
                    string media_src = metadata["d"]["Binary"]["__mediaresource"]["media_src"].ToString();
                    string content_type = metadata["d"]["Binary"]["__mediaresource"]["content_type"].ToString();
                    IEnumerable actions = metadata["d"]["__metadata"]["actions"].Children();
                    Hashtable actionsMap = new Hashtable();
                    foreach (object action in actions)
                    {
                        JObject jObj = action as JObject;
                        actionsMap.Add(jObj["name"].ToString(), jObj);
                    }
                    lockObj = CreateLockObject(Id, "DownloadFile");
                    if (IsLocked(lockObj))
                    {
                        return;
                    }
                    else
                    {
                        DoLock(lockObj);
                        if (File.Exists(TemporaryDirPath + Id + fileExtension) == false)
                            client.DownloadFile(Repo + media_src, TemporaryDirPath + Id + fileExtension);
                        ReleaseLock(lockObj);
                    }
                    lockObj = CreateLockObject(Id, "ConvertToPdf");
                    if (IsLocked(lockObj))
                    {
                        return;
                    }
                    else
                    {
                        DoLock(lockObj);
                        if (File.Exists(TemporaryDirPath + Id + ".pdf") == false)
                            ConvertToPdf(TemporaryDirPath + Id + fileExtension, TemporaryDirPath + Id + ".pdf");
                        ReleaseLock(lockObj);
                    }
                    lockObj = CreateLockObject(Id, "DeleteDownloadedFile");
                    if (IsLocked(lockObj))
                    {
                        return;
                    }
                    else
                    {
                        DoLock(lockObj);
                        if (File.Exists(TemporaryDirPath + Id + fileExtension) && fileExtension.ToLower() != ".pdf")
                            File.Delete(TemporaryDirPath + Id + fileExtension);
                        ReleaseLock(lockObj);
                    }

                    Size previewImageSize = new Size();
                    Size previewImageThumbnailSize = new Size();
                    if (fileExtension.ToLower() == ".pdf")
                    {
                        previewImageSize.Width = 600;
                        previewImageSize.Height = 850;
                        previewImageThumbnailSize.Width = 200;
                        previewImageThumbnailSize.Height = 200;
                    }
                    else
                    {
                        lockObj = CreateLockObject(Id, "");
                        if (IsLocked(lockObj))
                            return;
                        else
                        {
                            DoLock(lockObj);
                            FileService.FileServiceClient OOClient = new FileService.FileServiceClient();
                            previewImageSize = OOClient.GetSuggestImageSize(fileExtension, false);
                            previewImageThumbnailSize = OOClient.GetSuggestImageSize(fileExtension, true);
                            OOClient.Close();
                            ReleaseLock(lockObj);
                        }
                    }
                    int numOfPages = 0;
                    lockObj = CreateLockObject(Id, "ConvertPdfToImages");
                    if (IsLocked(lockObj))
                    {
                        return;
                    }
                    else
                    {
                        DoLock(lockObj);
                        if (File.Exists(TemporaryDirPath + Id + ".png") == false && File.Exists(TemporaryDirPath + Id + ".1.png") == false)
                        {
                            numOfPages = ConvertPdfToImages(TemporaryDirPath + Id + ".pdf", TemporaryDirPath, Id + ".{0}", previewImageSize.Width, previewImageSize.Height, false);
                        }
                        ReleaseLock(lockObj);
                    }
                    lockObj = CreateLockObject(Id, "ConvertPdfToThumbnailImages");
                    if (IsLocked(lockObj))
                    {
                        return;
                    }
                    else
                    {
                        DoLock(lockObj);
                        if (File.Exists(TemporaryDirPath + Id + ".thumbnail.png") == false && File.Exists(TemporaryDirPath + Id + ".thumbnail.1.png") == false)
                        {
                            numOfPages = ConvertPdfToImages(TemporaryDirPath + Id + ".pdf", TemporaryDirPath, Id + ".thumbnail.{0}", previewImageThumbnailSize.Width, previewImageThumbnailSize.Height, true);
                        }
                        ReleaseLock(lockObj);
                    }
                    lockObj = CreateLockObject(Id, "DeletePdfFile");
                    if (IsLocked(lockObj))
                    {
                        return;
                    }
                    else
                    {
                        DoLock(lockObj);
                        if (File.Exists(TemporaryDirPath + Id + ".pdf"))
                            File.Delete(TemporaryDirPath + Id + ".pdf");
                        ReleaseLock(lockObj);
                    }

                    string getPreviewFolderRequest = Repo + ((JObject)actionsMap["GetPreviewsFolder"])["target"].ToString();
                    //File.AppendAllText(TemporaryDirPath + file2, Environment.NewLine + getPreviewFolderRequest);

                    JObject p = null;
                    string responseString = "";
                    p = new JObject();
                    p["empty"] = true;
                    lockObj = CreateLockObject(Id, "GetPreviewFolder");
                    if (IsLocked(lockObj))
                    {
                        return;
                    }
                    else
                    {
                        DoLock(lockObj);
                        responseString = GetCachedDataFromLockObject(lockObj);
                        if (responseString == null)
                        {
                            serializer.Serialize(jwriter, p);
                            responseString = client.UploadString(getPreviewFolderRequest, builder.ToString());
                            SaveCacheDataInLockObject(lockObj, responseString);
                            builder.Clear();
                        }
                        else
                        {
                            DeleteCachedDataInLockObject(lockObj);
                        }
                        previousLockObj = lockObj;
                        ReleaseLock(lockObj);
                    }

                    jreader = new JsonTextReader(new StringReader(responseString));
                    //DeleteCachedDataInLockObject(lockObj);
                    JObject jsonResponse = serializer.Deserialize(jreader) as JObject;
                    string previewFolderId = jsonResponse["Id"].ToString();
                    string previewFolderPath = jsonResponse["Path"].ToString();

                    string uploadRequest = Repo + ODATA_SERVICE_PATH + string.Format(GET_CONTENT_BY_ID_REQUEST_FORMAT, previewFolderId) + "/Upload";
                    NameValueCollection arguments = new NameValueCollection();
                    string setPageCountRequest = Repo + ((JObject)actionsMap["SetPageCount"])["target"].ToString();
                    /*lockObj = CreateLockObject(Id, "SetPageCount");
                    if (IsLocked(lockObj))
                    {
                        return;
                    }
                    else
                    {
                        DoLock(lockObj);
                        DeleteCachedDataInLockObject(previousLockObj);
                        p = new JObject();
                        p["pageCount"] = 0;
                        arguments.Add("pageCount", 0.ToString());
                        serializer.Serialize(jwriter, p);
                        client.UploadString(setPageCountRequest, builder.ToString());
                        //client.UploadValues(setPageCountRequest, arguments);
                        arguments.Clear();
                        builder.Clear();
                        ReleaseLock(lockObj);
                    }*/
                    for (int i = 0; i < numOfPages; i++)
                    {
                        string uploadFileName = string.Format(PREVIEW_IMAGE_FORMAT, (i + 1).ToString());
                        string fullInputFilePath = TemporaryDirPath + string.Format(TEMP_PREVIEW_IMAGE_FORMAT, Id, (i + 1));

                        lockObj = CreateLockObject(Id, "UploadPreviewImage" + (i + 1));
                        if (IsLocked(lockObj))
                            continue;
                        else
                        {
                            DoLock(lockObj);
                            DeleteCachedDataInLockObject(previousLockObj);
                            if (File.Exists(fullInputFilePath))
                                UploadFile(uploadRequest, fullInputFilePath, uploadFileName, "PreviewImage");
                            ReleaseLock(lockObj);
                        }
                        lockObj = CreateLockObject(Id, "DeletePreviewImage" + (i + 1));
                        if (IsLocked(lockObj))
                            continue;
                        else
                        {
                            DoLock(lockObj);
                            if (File.Exists(fullInputFilePath))
                                File.Delete(fullInputFilePath);
                            ReleaseLock(lockObj);
                        }
                        string uploadThumbnailFileName = string.Format(PREVIEW_IMAGE_THUMBNAIL_FORMAT, (i + 1).ToString());
                        string fullInputThumbnailFilePath = TemporaryDirPath + string.Format(TEMP_PREVIEW_IMAGE_THUMBNAIL_FORMAT, Id, (i + 1));

                        lockObj = CreateLockObject(Id, "UploadPreviewImageThumbnail" + (i + 1));
                        if (IsLocked(lockObj))
                            continue;
                        else
                        {
                            DoLock(lockObj);
                            if (File.Exists(fullInputThumbnailFilePath))
                                UploadFile(uploadRequest, fullInputThumbnailFilePath, uploadThumbnailFileName, "PreviewImage");
                            ReleaseLock(lockObj);
                        }
                        lockObj = CreateLockObject(Id, "DeletePreviewImageThumbnail" + (i + 1));
                        if (IsLocked(lockObj))
                            continue;
                        else
                        {
                            DoLock(lockObj);
                            if (File.Exists(fullInputThumbnailFilePath))
                                File.Delete(fullInputThumbnailFilePath);
                            ReleaseLock(lockObj);
                        }
                        string path = previewFolderPath;
                        if (path.EndsWith("/"))
                            path = path.Substring(0, path.Length - 1);
                        string setInitialPreviewPropertiesRequest = Repo + path + "('" + uploadFileName + "')/SetInitialPreviewProperties";
                        lockObj = CreateLockObject(Id, "SetInitialPreviewProperties" + (i + 1));
                        if (IsLocked(lockObj))
                        {
                            continue;
                        }
                        else
                        {
                            DoLock(lockObj);
                            p = new JObject();
                            serializer.Serialize(jwriter, p);
                            client.UploadString(setInitialPreviewPropertiesRequest, builder.ToString());
                            builder.Clear();
                            ReleaseLock(lockObj);
                        }

                        setInitialPreviewPropertiesRequest = Repo + path + "('" + uploadThumbnailFileName + "')/SetInitialPreviewProperties";
                        lockObj = CreateLockObject(Id, "SetInitialPreviewPropertiesThumbnail" + (i + 1));
                        if (IsLocked(lockObj))
                        {
                            continue;
                        }
                        else
                        {
                            DoLock(lockObj);
                            p = new JObject();
                            serializer.Serialize(jwriter, p);
                            client.UploadString(setInitialPreviewPropertiesRequest, builder.ToString());
                            builder.Clear();
                            ReleaseLock(lockObj);
                        }

                        lockObj = CreateLockObject(Id, "SetPageCount"+(i+1));
                        if (IsLocked(lockObj))
                        {
                            continue;
                        }
                        else
                        {
                            DoLock(lockObj);
                            DeleteCachedDataInLockObject(previousLockObj);
                            p = new JObject();
                            p["pageCount"] = (i + 1);
                            arguments.Add("pageCount", (i + 1).ToString());
                            serializer.Serialize(jwriter, p);
                            client.UploadString(setPageCountRequest, builder.ToString());
                            //client.UploadValues(setPageCountRequest, arguments);
                            arguments.Clear();
                            builder.Clear();
                            ReleaseLock(lockObj);
                        }
                    }
                    
                    /*string setPreviewStatusRequest = Repo + ((JObject)actionsMap["SetPreviewStatus"])["target"].ToString();

                    lockObj = CreateLockObject(Id, "SetPreviewStatus");
                    if (IsLocked(lockObj))
                        return;
                    else
                    {
                        DoLock(lockObj);
                        p = new JObject();
                        p["status"] = "Ready";
                        serializer.Serialize(jwriter, p);
                        arguments.Add("status", "Ready");
                        client.UploadString(setPreviewStatusRequest, builder.ToString().Replace("\"Ready\"", "Ready"));
                        //client.UploadValues(setPreviewStatusRequest, arguments);
                        arguments.Clear();
                        builder.Clear();
                        ReleaseLock(lockObj);
                    }*/

                    
                }
                catch (Exception ex)
                {
                    if (string.IsNullOrEmpty(lockObj) == false)
                    {
                        DeleteCachedDataInLockObject(lockObj);
                    }
                    SaveExceptionForLockObject(lockObj, ex);
                    ReleaseLock(lockObj);
                }
            }
            /*for (var i = 0; i < 10; i++)
            {
                //do stuff...
                Console.WriteLine("Progress: {0}%", i * 10);
            }*/
            
            Console.WriteLine("Progress: Execution ended.");
        }

        private static string Repo { get; set; }
        private static string UserName { get; set; }
        private static string Password { get; set; }
        private static string GET_CONTENT_BY_ID_REQUEST_FORMAT = "content({0})";
        private static string ODATA_SERVICE_PATH = "/OData.svc/";
        private static string Id { get; set; }
        private static string Version { get; set; }
        private static string PREVIEW_IMAGE_FORMAT = "preview{0}.png";
        private static string PREVIEW_IMAGE_THUMBNAIL_FORMAT = "thumbnail{0}.png";
        private static string TEMP_PREVIEW_IMAGE_FORMAT = "{0}.{1}.png";
        private static string TEMP_PREVIEW_IMAGE_THUMBNAIL_FORMAT = "{0}.thumbnail.{1}.png";
        private static readonly int CHUNK_SIZE = 2048;
        private static bool ParseParameters(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("REPO:", StringComparison.OrdinalIgnoreCase))
                {
                    //TODO: store Url
                    Repo = arg.Substring(5);
                }
                else if (arg.StartsWith("USERNAME:", StringComparison.OrdinalIgnoreCase))
                {
                    //TODO: store Username
                    UserName = arg.Split(':')[1];
                }
                else if (arg.StartsWith("PASSWORD:", StringComparison.OrdinalIgnoreCase))
                {
                    //TODO: store Password
                    Password = arg.Split(':')[1];
                }
                else if (arg.StartsWith("DATA:", StringComparison.OrdinalIgnoreCase))
                {
                    var data = GetParameterValue(arg).Replace("\"\"", "\"");

                    var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
                    var serializer = JsonSerializer.Create(settings);
                    var jreader = new JsonTextReader(new StringReader(data));
                    var taskData = serializer.Deserialize(jreader) as JObject;

                    //TODO: get values from taskData
                    JToken id = taskData.GetValue("Id");
                    Id = id.ToString();
                    JToken version = taskData.GetValue("Version");
                    Version = version.ToString();

                }
            }

            return true;
        }

        private static string GetParameterValue(string arg)
        {
            return arg.Substring(arg.IndexOf(":", StringComparison.Ordinal) + 1).TrimStart(new[] { '\'', '"' }).TrimEnd(new[] { '\'', '"' });
        }
        private static WebRequest GetChunkWebRequest(string url, long fileLength, string fileName, string token, string boundary)
        {
            var myReq = (HttpWebRequest)WebRequest.Create(new Uri(url));

            myReq.Method = "POST";
            myReq.ContentType = "multipart/form-data; boundary=" + boundary;
            myReq.KeepAlive = true;

            if (string.IsNullOrEmpty(UserName))
            {
                myReq.UseDefaultCredentials = true;
            }
            else
            {
                myReq.UseDefaultCredentials = false;
                myReq.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(UserName + ":" + Password)));
            }
            myReq.Headers.Add("Content-Disposition", "attachment; filename=\"" + fileName + "\"");

            var boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            var formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            var headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";

            var useChunk = fileLength > CHUNK_SIZE;
            var postValues = new NameValueCollection
                                 {
                                     {"ContentType", "File"},
                                     {"FileName", fileName},
                                     {"Overwrite", "true"},
                                     {"UseChunk", useChunk.ToString()},
                                     {"ChunkToken", token}
                                 };

            //we must not close the stream after this as we need to write 
            //the chunk into it in the caller method
            var reqStream = myReq.GetRequestStream();

            //write form data values
            foreach (string key in postValues.Keys)
            {
                reqStream.Write(boundarybytes, 0, boundarybytes.Length);

                var formitem = string.Format(formdataTemplate, key, postValues[key]);
                var formitembytes = Encoding.UTF8.GetBytes(formitem);

                reqStream.Write(formitembytes, 0, formitembytes.Length);
            }

            //write a boundary
            reqStream.Write(boundarybytes, 0, boundarybytes.Length);

            //write file name and content type
            var header = string.Format(headerTemplate, "files[]", fileName);
            var headerbytes = Encoding.UTF8.GetBytes(header);

            reqStream.Write(headerbytes, 0, headerbytes.Length);

            return myReq;
        }
        private static WebRequest GetInitWebRequest(string url, string fileName, long fileLength, string contentType)
        {
            var myReq = WebRequest.Create(new Uri(url + "?create=1"));
            myReq.Method = "POST";
            if (string.IsNullOrEmpty(UserName))
            {
                myReq.UseDefaultCredentials = true;
            }
            else
            {
                myReq.UseDefaultCredentials = false;
                myReq.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(UserName + ":" + Password)));
            }
            myReq.ContentType = "application/x-www-form-urlencoded";

            var useChunk = fileLength > CHUNK_SIZE;
            var postData = string.Format("ContentType="+contentType+"&FileName=" + fileName + "&Overwrite=true&UseChunk={0}", useChunk);
            var postDataBytes = Encoding.ASCII.GetBytes(postData);

            myReq.ContentLength = postDataBytes.Length;

            using (var reqStream = myReq.GetRequestStream())
            {
                reqStream.Write(postDataBytes, 0, postDataBytes.Length);
            }

            return myReq;
        }
        private static void UploadFile(string url, string inputFileName, string outputFileName, string contentType)
        {
            var fileInfo = new FileInfo(inputFileName);
            var fileLength = fileInfo.Length;
            var myReq = GetInitWebRequest(url, outputFileName, fileLength, contentType);
            string token;

            //send initial request
            var wr = myReq.GetResponse();
            using (var stream = wr.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    token = reader.ReadToEnd();
                }
            }

            var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            var trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            //open file and send subsequent requests
            using (var fileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[CHUNK_SIZE];
                int bytesRead;
                var start = 0;

                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    //get the request object for the actual chunk
                    myReq = GetChunkWebRequest(url, fileLength, outputFileName, token, boundary);
                    myReq.Headers.Set("Content-Range", string.Format("bytes {0}-{1}/{2}", start, start + bytesRead - 1, fileLength));

                    //write the chunk into the request stream
                    using (var reqStream = myReq.GetRequestStream())
                    {
                        reqStream.Write(buffer, 0, bytesRead);
                        reqStream.Write(trailer, 0, trailer.Length);
                    }

                    start += bytesRead;

                    //send the request
                    wr = myReq.GetResponse();

                    //optional: get the response (contains the content path, etc.)
                    using (var stream = wr.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var response = reader.ReadToEnd();
                        }
                    }
                }
            }
        }
        private static bool isSupported(string extension)
        {
            FileService.FileServiceClient client = new FileService.FileServiceClient();
            bool b = client.IsSupportedExtension(extension);
            client.Close();
            return b;
        }

        // methods for locking machine
        private static string CreateLockObject(string obj, string method)
        {
            return TemporaryDirPath + obj + "." + method + ".lock";
        }
        private static void DoLock(string lockObj)
        {
            if(File.Exists(lockObj) == false)
                File.Create(lockObj);
        }
        private static bool IsLocked(string lockObj)
        {
            return File.Exists(lockObj);
        }
        private static void ReleaseLock(string lockObj)
        {
            while (File.Exists(lockObj))
            {
                try
                {
                    File.Delete(lockObj);
                }
                catch (Exception ex)
                {
                }
            }
        }
        private static void SaveCacheDataInLockObject(string lockObj, string data)
        {
            File.WriteAllText(lockObj + ".cache.txt", data);
        }
        private static string GetCachedDataFromLockObject(string lockObj)
        {
            if (File.Exists(lockObj + ".cache.txt"))
                return File.ReadAllText(lockObj + ".cache.txt");
            return null;
        }
        private static void DeleteCachedDataInLockObject(string lockObj)
        {
            while (File.Exists(lockObj + ".cache.txt"))
            {
                try
                {
                    File.Delete(lockObj + ".cache.txt");
                }
                catch (Exception)
                {
                    
                }
            }
        }
        private static void SaveExceptionForLockObject(string lockObj, Exception ex)
        {
            File.WriteAllText(lockObj + ".error.txt", ex.ToString());
        }
    }
}

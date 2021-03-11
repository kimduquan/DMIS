using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using IO = System.IO;
using System.Linq;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ApplicationModel;
using ContentRepository;
using ContentRepository.Storage.Data;
using ContentRepository.Storage.Events;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Search;
using ContentRepository.Versioning;
using Diagnostics;
using ContentRepository.Storage;
using ContentRepository.Storage.Security;
using BackgroundOperations;
using Newtonsoft.Json.Converters;

namespace Preview
{
    public enum WatermarkPosition { BottomLeftToUpperRight, UpperLeftToBottomRight, Top, Bottom, Center }
    public enum DocumentFormat { NonDefined, Doc, Docx, Pdf, Ppt, Pptx, Xls, Xlsx }

    [Flags]
    public enum RestrictionType
    {
        NoAccess = 1,
        NoRestriction = 2,
        Redaction = 4,
        Watermark = 8
    }

    public enum PreviewStatus
    {
        NoProvider = -5,
        Postponed = -4,
        Error = -3,
        NotSupported = -2,
        InProgress = -1,
        EmptyDocument = 0,
        Ready = 1
    }

    public class WatermarkDrawingInfo
    {
        private readonly System.Drawing.Image _image;
        private readonly System.Drawing.Graphics _context;

        public string WatermarkText { get; set; }
        public System.Drawing.Font Font { get; set; }
        public WatermarkPosition Position { get; set; }
        public System.Drawing.Color Color { get; set; }

        public WatermarkDrawingInfo(System.Drawing.Image image, System.Drawing.Graphics context)
        {
            if (image == null) 
                throw new ArgumentNullException("image");
            if (context == null)
                throw new ArgumentNullException("context");

            _image = image;
            _context = context;
        }

        public System.Drawing.Image Image
        {
            get { return _image; }
        }

        public System.Drawing.Graphics DrawingContext
        {
            get { return _context; }
        }
    }


    public abstract class DocumentPreviewProvider : IPreviewProvider
    {
        public static readonly string DOCUMENTPREVIEW_SETTINGS = "DocumentPreview";
        public static readonly string WATERMARK_TEXT = "WatermarkText";
        public static readonly string WATERMARK_ENABLED = "WatermarkEnabled";
        public static readonly string WATERMARK_FONT = "WatermarkFont";
        public static readonly string WATERMARK_BOLD = "WatermarkBold";
        public static readonly string WATERMARK_ITALIC = "WatermarkItalic";
        public static readonly string WATERMARK_FONTSIZE = "WatermarkFontSize";
        public static readonly string WATERMARK_POSITION = "WatermarkPosition";
        public static readonly string WATERMARK_OPACITY = "WatermarkOpacity";
        public static readonly string WATERMARK_COLOR = "WatermarkColor";
        public static readonly string MAXPREVIEWCOUNT = "MaxPreviewCount";
        public static readonly string PREVIEWS_FOLDERNAME = "Previews";
        public static readonly string PREVIEW_THUMBNAIL_REGEX = "(preview|thumbnail)(?<page>\\d+).png";
        public static readonly string THUMBNAIL_REGEX = "thumbnail(?<page>\\d+).png";

        protected static readonly float THUMBNAIL_PREVIEW_WIDTH_RATIO = Common.THUMBNAIL_WIDTH / (float)Common.PREVIEW_WIDTH;
        protected static readonly float THUMBNAIL_PREVIEW_HEIGHT_RATIO = Common.THUMBNAIL_HEIGHT / (float)Common.PREVIEW_HEIGHT;

        protected static readonly int PREVIEW_PDF_WIDTH = 600;
        protected static readonly int PREVIEW_PDF_HEIGHT = 850;
        protected static readonly int PREVIEW_WORD_WIDTH = 800;
        protected static readonly int PREVIEW_WORD_HEIGHT = 870;
        protected static readonly int PREVIEW_EXCEL_WIDTH = 1000;
        protected static readonly int PREVIEW_EXCEL_HEIGHT = 750;

        protected static readonly int WATERMARK_MAXLINECOUNT = 3;


        //============================================================================== Configuration

        private const string DEFAULT_DOCUMENTPREVIEWPROVIDER_CLASSNAME = "Preview.DefaultDocumentPreviewProvider";

        private const string DOCUMENTPREVIEWPROVIDERCLASSNAMEKEY = "DocumentPreviewProvider";
        private static string DocumentPreviewProviderClassName
        {
            get
            {
                return ConfigurationManager.AppSettings[DOCUMENTPREVIEWPROVIDERCLASSNAMEKEY];
            }
        }

        //===================================================================================================== Static provider instance

        private static DocumentPreviewProvider _current;
        private static readonly object _lock = new object();
        private static bool _isInitialized;
        public static DocumentPreviewProvider Current
        {
            get
            {
                //This property has a duplicate in the Storage layer in the PreviewProvider
                //class. If you change this, please propagate changes there.

                if ((_current == null) && (!_isInitialized))
                {
                    lock (_lock)
                    {
                        if ((_current == null) && (!_isInitialized))
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(DocumentPreviewProviderClassName))
                                    _current = (DocumentPreviewProvider)TypeHandler.CreateInstance(DocumentPreviewProviderClassName);
                                else
                                    _current = (DocumentPreviewProvider)TypeHandler.CreateInstance(DEFAULT_DOCUMENTPREVIEWPROVIDER_CLASSNAME);
                            }
                            catch (TypeNotFoundException) //rethrow
                            {
                                throw new ConfigurationErrorsException(String.Concat(SR.Exceptions.Configuration.Msg_DocumentPreviewProviderImplementationDoesNotExist, ": ", DocumentPreviewProviderClassName));
                            }
                            catch (InvalidCastException) //rethrow
                            {
                                throw new ConfigurationErrorsException(String.Concat(SR.Exceptions.Configuration.Msg_InvalidDocumentPreviewProviderImplementation, ": ", DocumentPreviewProviderClassName));
                            }
                            finally
                            {
                                _isInitialized = true;
                            }

                            if (_current == null)
                                Logger.WriteInformation(Logger.EventId.NotDefined, "DocumentPreviewProvider not present.");
                            else
                                Logger.WriteInformation(Logger.EventId.NotDefined, "DocumentPreviewProvider created: " + _current.GetType().FullName, properties: new Dictionary<string, object>
                                {
                                    { "SupportedTaskNames", string.Join(", ", _current.GetSupportedTaskNames())}
                                });
                        }
                    }
                }
                return _current;
            }
        }

        //===================================================================================================== Helper methods

        protected static string GetPreviewsSubfolderName(Node content)
        {
            return content.Version.ToString();
        }
        
        protected static string GetPreviewNameFromPageNumber(int page)
        {
            return string.Format(Common.PREVIEW_IMAGENAME, page);
        }
        protected static string GetThumbnailNameFromPageNumber(int page)
        {
            return string.Format(Common.THUMBNAIL_IMAGENAME, page);
        }

        protected static bool GetDisplayWatermarkQueryParameter()
        {
            if (HttpContext.Current == null)
                return false;

            var watermarkVal = HttpContext.Current.Request["watermark"];
            if (string.IsNullOrEmpty(watermarkVal))
                return false;

            if (watermarkVal == "1")
                return true;

            bool wm;
            return bool.TryParse(watermarkVal, out wm) && wm;
        }

        protected static System.Drawing.Color ParseColor(string color)
        {
            //rgba(0,0,0,1)
            if (string.IsNullOrEmpty(color))
                return System.Drawing.Color.DarkBlue;

            var i1 = color.IndexOf('(');
            var colorVals = color.Substring(i1 + 1, color.IndexOf(')') - i1 - 1).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            return System.Drawing.Color.FromArgb(Convert.ToInt32(colorVals[3]), Convert.ToInt32(colorVals[0]),
                                  Convert.ToInt32(colorVals[1]), Convert.ToInt32(colorVals[2]));
        }

        protected static System.Drawing.Image ResizeImage(System.Drawing.Image image, int maxWidth, int maxHeight)
        {
            if (image == null)
                return null;

            //do not scale up the image
            if (image.Width < maxWidth && image.Height < maxHeight)
                return image;

            int newWidth;
            int newHeight;

            ComputeResizedDimensions(image.Width, image.Height, maxWidth, maxHeight, out newWidth, out newHeight);

            try
            {
                var newImage = new System.Drawing.Bitmap(newWidth, newHeight);
                using (var graphicsHandle = System.Drawing.Graphics.FromImage(newImage))
                {
                    graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
                }

                return newImage;
            }
            catch (OutOfMemoryException omex)
            {
                Logger.WriteException(omex);
                return null;
            }
        }

        protected static void ComputeResizedDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight, out int newWidth, out int newHeight)
        {
            // do not scale up the image
            if (originalWidth <= maxWidth && originalHeight <= maxHeight)
            {
                newWidth = originalWidth;
                newHeight = originalHeight;
                return;
            }

            var percentWidth = (float)maxWidth / (float)originalWidth;
            var percentHeight = (float)maxHeight / (float)originalHeight;

            // determine which dimension scale should we use (the smaller)
            var percent = percentHeight < percentWidth ? percentHeight : percentWidth;

            // compute new width and height, based on the final scale
            newWidth = (int)(originalWidth * percent);
            newHeight = (int)(originalHeight * percent);
        }

        protected static void SavePageCount(File file, int pageCount)
        {
            if (file == null || file.PageCount == pageCount)
                return;

            using (new SystemAccount())
            {
                file.PageCount = pageCount;
                file.DisableObserver(typeof(DocumentPreviewObserver));
                file.DisableObserver(TypeHandler.GetType(NodeObserverNames.NOTIFICATION));
                file.DisableObserver(TypeHandler.GetType(NodeObserverNames.WORKFLOWNOTIFICATION));

                file.Save(SavingMode.KeepVersion);
            }
        }

        protected static VersionNumber GetVersionFromPreview(NodeHead previewHead)
        {
            if (previewHead == null)
                return null;

            // Expected structure: /Root/.../DocumentLibrary/doc1.docx/Previews/V1.2.D/preview1.png
            var parentName = RepositoryPath.GetFileName(RepositoryPath.GetParentPath(previewHead.Path));
            VersionNumber version;

            return !VersionNumber.TryParse(parentName, out version) ? null : version;
        }

        protected static File GetDocumentForPreviewImage(NodeHead previewHead)
        {
            using (new SystemAccount())
            {
                var document = Node.GetAncestorOfType<File>(Node.LoadNode(previewHead.ParentId));

                // we need to load the appropriate document version for this preview image
                var version = GetVersionFromPreview(previewHead);
                if (version != null && version.VersionString != document.Version.VersionString)
                    document = Node.Load<File>(document.Id, version);

                return document;
            }
        }

        /// <summary>
        /// This method ensures the existence of all the preview images in a range. 
        /// It synchronously waits for all the images to be created.
        /// </summary>
        protected static void CheckPreviewImages(Content content, int start, int end)
        {
            if (content == null)
                throw new PreviewNotAvailableException("Content deleted.", -1, 0);

            var pc = (int)content["PageCount"];
            if (pc < 0)
                throw new PreviewNotAvailableException("Preview not available. State: " + pc + ".", -1, pc);
            if (end < 0)
                throw new PreviewNotAvailableException("Invalid 'end' value: " + end, -1, pc);

            Image image;
            var missingIndexes = new List<int>();
            for (var i = start; i <= end; i++)
            {
                AssertResultIsStillRequired();
                image = DocumentPreviewProvider.Current.GetPreviewImage(content, i);
                if (image == null || image.Index < 1)
                    missingIndexes.Add(i);
            }
            foreach (var i in missingIndexes)
            {
                do
                {
                    AssertResultIsStillRequired();

                    //this call will start a preview workflow if the image does not exist
                    image = DocumentPreviewProvider.Current.GetPreviewImage(content, i);
                    if (image == null || image.Index < 1)
                    {
                        // document was deleted in the meantime
                        if (!Node.Exists(content.Path))
                            throw new PreviewNotAvailableException("Content deleted.", -1, 0);

                        Thread.Sleep(1000);
                    }

                } while (image == null);
            }
        }

        protected static IEnumerable<Node> QueryPreviewImages(string path)
        {
            return NodeQuery.QueryNodesByTypeAndPath(NodeType.GetByName("PreviewImage"), false, path + "/", false)
                .Identifiers
                .Select(i => NodeHead.Get(i))
                .Where(h => h.Name.StartsWith("preview", StringComparison.OrdinalIgnoreCase))
                .Select(h => Node.LoadNode(h));
        }

        protected static void AssertResultIsStillRequired()
        {
            if (HttpContext.Current != null && !HttpContext.Current.Response.IsClientConnected)
            {
                //TODO: create a new exception class for this
                throw new Exception("Client is disconnected");
            }
        }

        protected static void DrawLines(IList<string> lines, WatermarkDrawingInfo info)
        {
            var numberOfLines = lines.Count(x => !string.IsNullOrEmpty(x));
            var blockHeight = 0.0f;

            for (var j = 0; j < numberOfLines; j++)
            {
                blockHeight += info.DrawingContext.MeasureString(lines[j], info.Font).Height;
            }

            for (var j = 0; j < numberOfLines; j++)
            {
                var currentLineSize = info.DrawingContext.MeasureString(lines[j], info.Font);
                var wx = -currentLineSize.Width / 2;
                var wy = 0.0f;

                switch (info.Position)
                {
                    case WatermarkPosition.BottomLeftToUpperRight:
                    case WatermarkPosition.UpperLeftToBottomRight:
                        wy = -(blockHeight / 2) + j * (blockHeight / numberOfLines);
                        break;
                    case WatermarkPosition.Top:
                        wx = (info.Image.Width - currentLineSize.Width) / 2;
                        wy = (currentLineSize.Height - info.Font.Size) * j;
                        break;
                    case WatermarkPosition.Bottom:
                        wx = (info.Image.Width - currentLineSize.Width) / 2;
                        wy = info.Image.Height - ((currentLineSize.Height - info.Font.Size) * (numberOfLines - j));
                        break;
                    case WatermarkPosition.Center:
                        wx = (info.Image.Width - currentLineSize.Width) / 2;
                        wy = (info.Image.Height / 2.0f) - (blockHeight / 2) + j * (blockHeight / numberOfLines);
                        break;
                }

                info.DrawingContext.DrawString(lines[j], info.Font, new System.Drawing.SolidBrush(info.Color), wx, wy);
            }
        }

        protected static IEnumerable<string> BreakTextIntoLines(WatermarkDrawingInfo info, double maxTextWithOnImage, float charSize)
        {
            var maxCharNumInLine = (int)Math.Round((maxTextWithOnImage - (maxTextWithOnImage * 0.2)) / charSize);
            var words = info.WatermarkText.Trim().Split(' ');
            var lines = new string[WATERMARK_MAXLINECOUNT];

            var lineNumber = 0;
            var lineLength = 0;

            for (var j = 0; j < words.Length; j++)
            {
                if (lineNumber < WATERMARK_MAXLINECOUNT)
                {
                    if (lineLength < maxCharNumInLine && (lineLength + words[j].Length + 1) < maxCharNumInLine)
                    {
                        if (lineLength == 0)
                        {
                            lines[lineNumber] = lines[lineNumber] + words[j];
                            lineLength += words[j].Length;
                        }
                        else
                        {
                            lines[lineNumber] = lines[lineNumber] + " " + words[j];
                            lineLength += (words[j].Length + 1);
                        }
                    }
                    else
                    {
                        j--;
                        lineLength = 0;
                        lineNumber += 1;
                    }
                }
                else
                {
                    break;
                }
            }

            if (lines.Count(x => !string.IsNullOrEmpty(x)) == 0)
            {
                var charactersToSplit = maxCharNumInLine - 2;
                var maxCharNumber = WATERMARK_MAXLINECOUNT * charactersToSplit;

                if (info.WatermarkText.Length > maxCharNumber)
                {
                    info.WatermarkText = info.WatermarkText.Substring(0, maxCharNumber);
                }

                var watermarkText = string.Empty;
                for (var i = 0; i < WATERMARK_MAXLINECOUNT; i++)
                {
                    var charToSplit = (i + 1) * charactersToSplit <= info.WatermarkText.Length
                        ? charactersToSplit
                        : info.WatermarkText.Length - (i * charactersToSplit);

                    if (charToSplit <= 0)
                    {
                        break;
                    }

                    watermarkText += string.Format("{0} ", info.WatermarkText.Substring(i * charactersToSplit, charToSplit));
                }

                info.WatermarkText = watermarkText;

                return BreakTextIntoLines(info, maxTextWithOnImage, charSize);
            }

            return lines.AsEnumerable();
        }

        //===================================================================================================== Server-side interface

        public abstract bool IsContentSupported(Node content);
        public abstract string GetPreviewGeneratorTaskName(string contentPath);
        public abstract string[] GetSupportedTaskNames();

        public virtual bool IsPreviewOrThumbnailImage(NodeHead imageHead)
        {
            return (imageHead != null &&
                    imageHead.GetNodeType().IsInstaceOfOrDerivedFrom(ActiveSchema.NodeTypes["PreviewImage"]) &&
                    imageHead.Path.Contains(RepositoryPath.PathSeparator + PREVIEWS_FOLDERNAME + RepositoryPath.PathSeparator)) &&
                    new Regex(PREVIEW_THUMBNAIL_REGEX).IsMatch(imageHead.Name);
        }

        public virtual bool IsThumbnailImage(Image image)
        {
            return (image != null &&
                    image.NodeType.IsInstaceOfOrDerivedFrom(ActiveSchema.NodeTypes["PreviewImage"]) &&
                    new Regex(THUMBNAIL_REGEX).IsMatch(image.Name));
        }

        public bool HasPreviewPermission(NodeHead nodeHead)
        {
            return (GetRestrictionType(nodeHead) & RestrictionType.NoAccess) != RestrictionType.NoAccess;
        }

        public virtual bool IsPreviewAccessible(NodeHead previewHead)
        {
            if (!HasPreviewPermission(previewHead))
                return false;

            var version = GetVersionFromPreview(previewHead);

            // The image is outside of a version folder (which is not valid), we have to allow accessing the image.
            if (version == null)
                return true;

            // This method was created to check if the user has access to preview images of minor document versions,
            // so do not bother if this is a preview for a major version.
            if (version.IsMajor)
                return true;

            // Here we assume that permissions are not broken on previews! This means the current user
            // has the same permissions (e.g. OpenMinor) on the preview image as on the document (if this 
            // is a false assumption, than we need to load the document itself and check OpenMinor on it).
            return SecurityHandler.HasPermission(previewHead, PermissionType.OpenMinor);
        }

        public virtual RestrictionType GetRestrictionType(NodeHead nodeHead)
        {
            //if the lowest preview permission is not granted, the user has no access to the preview image
            if (nodeHead == null || !SecurityHandler.HasPermission(nodeHead, PermissionType.Preview))
                return RestrictionType.NoAccess;

            //has Open permission: means no restriction
            if (SecurityHandler.HasPermission(nodeHead, PermissionType.Open))
                return RestrictionType.NoRestriction;

            var seeWithoutRedaction = SecurityHandler.HasPermission(nodeHead, PermissionType.PreviewWithoutRedaction);
            var seeWithoutWatermark = SecurityHandler.HasPermission(nodeHead, PermissionType.PreviewWithoutWatermark);

            //both restrictions should be applied
            if (!seeWithoutRedaction && !seeWithoutWatermark)
                return RestrictionType.Redaction | RestrictionType.Watermark;

            if (!seeWithoutRedaction)
                return RestrictionType.Redaction;

            if (!seeWithoutWatermark)
                return RestrictionType.Watermark;

            return RestrictionType.NoRestriction;
        }

        public virtual IEnumerable<Content> GetPreviewImages(Content content)
        {
            if (content == null || !this.IsContentSupported(content.ContentHandler) || !HasPreviewPermission(NodeHead.Get(content.Id)))
                return new List<Content>();

            var pc = (int)content["PageCount"];

            while (pc == (int)PreviewStatus.InProgress || pc == (int)PreviewStatus.Postponed)
            {
                //create task if it does not exists. Otherwise page count will not be calculated.
                StartPreviewGenerationInternal(content.ContentHandler, priority: TaskPriority.Immediately);

                Thread.Sleep(4000);

                AssertResultIsStillRequired();

                content = Content.Load(content.Id);
                if (content == null)
                    throw new PreviewNotAvailableException("Content deleted.", -1, 0);

                pc = (int)content["PageCount"];
            }

            var previewPath = RepositoryPath.Combine(content.Path, PREVIEWS_FOLDERNAME, GetPreviewsSubfolderName(content.ContentHandler));

            // Elevation is OK here as we already checked that the user has preview permissions for 
            // the content. It is needed because of backward compatibility: some preview images 
            // may have been created in a versioned folder as a content with minor version (e.g. 0.1.D).
            using (new SystemAccount())
            {
                var images = QueryPreviewImages(previewPath).ToArray();

                //all preview images exist
                if (images.Length != pc)
                {
                    // check all preview images one-by-one (wait for complete)
                    CheckPreviewImages(content, 1, pc);
                    images = QueryPreviewImages(previewPath).ToArray();
                }

                return images.Select(n => Content.Create(n));
            }
        }
        
        public virtual bool HasPreviewImages(Node content)
        {
            var pageCount = (int)content["PageCount"];
            if (pageCount > 0)
                return true;

            var status = (PreviewStatus)pageCount;
            switch (status)
            {
                case PreviewStatus.Postponed:
                case PreviewStatus.InProgress:
                case PreviewStatus.Ready:
                    return true;
                default:
                    return false;
            }
        }

        public virtual Image GetPreviewImage(Content content, int page)
        {
            return GetImage(content, page, false);
        }

        public virtual Image GetThumbnailImage(Content content, int page)
        {
            return GetImage(content, page, true);
        }

        private Image GetImage(Content content, int page, bool thumbnail)
        {
            if (content == null || page < 1)
                return null;

            //invalid request: not a file or not enough pages
            var file = content.ContentHandler as File;
            if (file == null || file.PageCount < page)
                return null;

            using (new SystemAccount())
            {
                var previewName = thumbnail ? GetThumbnailNameFromPageNumber(page) : GetPreviewNameFromPageNumber(page);
                var path = RepositoryPath.Combine(content.Path, PREVIEWS_FOLDERNAME, GetPreviewsSubfolderName(content.ContentHandler), previewName);
                var img = Node.Load<Image>(path);
                if (img != null)
                    return img;

                StartPreviewGenerationInternal(file, page - 1, TaskPriority.Immediately);
            }

            return null;
        }

        public virtual IO.Stream GetRestrictedImage(Image image, string binaryFieldName = null, RestrictionType? restrictionType = null)
        {
            var previewImage = image;

            //we need to reload the image in elevated mode to have access to its properties
            if (previewImage.IsHeadOnly)
            {
                using (new SystemAccount())
                {
                    previewImage = Node.Load<Image>(image.Id);
                }
            }

            BinaryData binaryData = null;

            if (!string.IsNullOrEmpty(binaryFieldName))
            {
                var property = previewImage.PropertyTypes[binaryFieldName];
                if (property != null && property.DataType == DataType.Binary)
                    binaryData = previewImage.GetBinary(property);
            }

            if (binaryData == null)
                binaryData = previewImage.Binary;

            //if the image is not a preview, return the requested binary without changes
            if (!IsPreviewOrThumbnailImage(NodeHead.Get(previewImage.Id)))
                return binaryData.GetStream();

            var isThumbnail = IsThumbnailImage(previewImage);

            //check restriction type
            var previewHead = NodeHead.Get(previewImage.Id);
            var rt = restrictionType.HasValue ? restrictionType.Value : GetRestrictionType(previewHead);
            var displayRedaction = (rt & RestrictionType.Redaction) == RestrictionType.Redaction;
            var displayWatermark = (rt & RestrictionType.Watermark) == RestrictionType.Watermark || GetDisplayWatermarkQueryParameter();

            if (!displayRedaction && !displayWatermark)
            {
                return binaryData.GetStream();
            }

            //load the parent document in elevated mode to have access to its properties
            var document = GetDocumentForPreviewImage(previewHead);

            var shapes = document != null ? (string)document["Shapes"] : null;
            var watermark = document != null ? document.Watermark : null;

            //if local watermark is empty, look for setting
            if (string.IsNullOrEmpty(watermark))
                watermark = Settings.GetValue<string>(DOCUMENTPREVIEW_SETTINGS, WATERMARK_TEXT, image.Path);

            //no redaction/highlight data found
            if (string.IsNullOrEmpty(shapes) && string.IsNullOrEmpty(watermark))
                return binaryData.GetStream();

            //return a memory stream containing the new image
            var ms = new IO.MemoryStream();

            using (var img = System.Drawing.Image.FromStream(binaryData.GetStream()))
            {
                using (var g = System.Drawing.Graphics.FromImage(img))
                {
                    //draw redaction
                    if (displayRedaction && !string.IsNullOrEmpty(shapes))
                    {
                        var imageIndex = GetPreviewImagePageIndex(previewImage);
                        var settings = new JsonSerializerSettings();
                        var serializer = JsonSerializer.Create(settings);
                        var jreader = new JsonTextReader(new IO.StringReader(shapes));
                        var shapeCollection = (JArray)serializer.Deserialize(jreader);
                        var redactions = shapeCollection[0]["redactions"].Where(jt => (int) jt["imageIndex"] == imageIndex).ToList();

                        var realWidthRatio = THUMBNAIL_PREVIEW_WIDTH_RATIO;
                        var realHeightRatio = THUMBNAIL_PREVIEW_HEIGHT_RATIO;

                        if (redactions.Any() && isThumbnail)
                        {
                            // If this is a thumbnail, we will need the real preview image to determine 
                            // the page width and height ratios to draw redactions to the correct place.
                            var pi = GetPreviewImage(Content.Create(document), imageIndex);

                            if (pi != null)
                            {
                                // Compute the exact position of the shape based on the size ratio of 
                                // the real preview image and this thumbnail. 
                                realWidthRatio = (float) img.Width/(float) pi.Width;
                                realHeightRatio = (float) img.Height/(float) pi.Height;
                            }
                            else
                            {
                                // We could not find the main preview image that this thumbnail is 
                                // related to (maybe because it is not generated yet). Use the old 
                                // inaccurate algorithm (that builds on the default image ratios) 
                                // as a workaround.
                            }
                        }

                        foreach (var redaction in redactions)
                        {
                            //var color = ParseColor(redaction["fill"].Value<string>());
                            var color = System.Drawing.Color.Black;
                            var shapeBrush = new System.Drawing.SolidBrush(color);
                            var shapeRectangle = new System.Drawing.Rectangle(redaction["x"].Value<int>(), redaction["y"].Value<int>(),
                                                            redaction["w"].Value<int>(), redaction["h"].Value<int>());

                            //convert shape to thumbnail size if needed
                            if (isThumbnail)
                            {
                                shapeRectangle = new System.Drawing.Rectangle(
                                    (int)Math.Round(shapeRectangle.X * realWidthRatio),
                                    (int)Math.Round(shapeRectangle.Y * realHeightRatio),
                                    (int)Math.Round(shapeRectangle.Width * realWidthRatio),
                                    (int)Math.Round(shapeRectangle.Height * realHeightRatio));
                            }

                            g.FillRectangle(shapeBrush, shapeRectangle);
                        }
                    }

                    //draw watermark
                    if (displayWatermark && !string.IsNullOrEmpty(watermark))
                    {
                        watermark = TemplateManager.Replace(typeof(WatermarkTemplateReplacer), watermark, new[] { document, image });

                        //check watermark master switch in settings
                        if (Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_ENABLED, image.Path, true))
                        {
                            var fontName = Settings.GetValue<string>(DOCUMENTPREVIEW_SETTINGS, WATERMARK_FONT, image.Path) ?? "Microsoft Sans Serif";
                            var fs = System.Drawing.FontStyle.Regular;
                            if (Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_BOLD, image.Path, true))
                                fs = fs | System.Drawing.FontStyle.Bold;
                            if (Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_ITALIC, image.Path, false))
                                fs = fs | System.Drawing.FontStyle.Italic;

                            var size = Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_FONTSIZE, image.Path, 72.0f);

                            //resize font in case of thumbnails
                            if (isThumbnail)
                                size = size * THUMBNAIL_PREVIEW_WIDTH_RATIO;

                            var font = new System.Drawing.Font(fontName, size, fs);
                            var position = Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_POSITION, image.Path, WatermarkPosition.BottomLeftToUpperRight);
                            var color = System.Drawing.Color.FromArgb(Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_OPACITY, image.Path, 50),
                                       (System.Drawing.Color)(new System.Drawing.ColorConverter().ConvertFromString(Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, WATERMARK_COLOR, image.Path, "Black"))));

                            var wmInfo = new WatermarkDrawingInfo(img, g)
                            {
                                WatermarkText = watermark,
                                Font = font,
                                Color = color,
                                Position = position
                            };

                            DrawWatermark(wmInfo);
                        }
                    }

                    g.Save();
                }

                ImageFormat imgFormat;

                switch (IO.Path.GetExtension(previewImage.Path).ToLower())
                {
                    case ".png":
                        imgFormat = ImageFormat.Png;
                        break;
                    case ".jpg":
                    case ".jpeg":
                        imgFormat = ImageFormat.Jpeg;
                        break;
                    case ".bmp":
                        imgFormat = ImageFormat.Bmp;
                        break;
                    default:
                        throw new NotImplementedException("Unknown image preview type: " + previewImage.Path);
                }

                img.Save(ms, imgFormat);
            }

            ms.Seek(0, IO.SeekOrigin.Begin);

            return ms;
        }

        protected virtual void DrawWatermark(WatermarkDrawingInfo info)
        {
            if (string.IsNullOrEmpty(info.WatermarkText)) 
                return;

            var textSize = info.DrawingContext.MeasureString(info.WatermarkText, info.Font);
            var charCount = info.WatermarkText.Length;
            var charSize = textSize.Width / charCount;
            double maxTextWithOnImage = 0;

            switch (info.Position)
            {
                case WatermarkPosition.BottomLeftToUpperRight:
                    info.DrawingContext.TranslateTransform(info.Image.Width / 2.0f, info.Image.Height / 2.0f);
                    info.DrawingContext.RotateTransform(-45);
                    maxTextWithOnImage = Math.Sqrt((info.Image.Width * info.Image.Width) + (info.Image.Height * info.Image.Height)) * 0.7;
                    break;
                case WatermarkPosition.UpperLeftToBottomRight:
                    info.DrawingContext.TranslateTransform(info.Image.Width / 2.0f, info.Image.Height / 2.0f);
                    info.DrawingContext.RotateTransform(45);
                    maxTextWithOnImage = Math.Sqrt((info.Image.Width * info.Image.Width) + (info.Image.Height * info.Image.Height)) * 0.7;
                    break;
                case WatermarkPosition.Top:
                    maxTextWithOnImage = info.Image.Width;
                    break;
                case WatermarkPosition.Bottom:
                    maxTextWithOnImage = info.Image.Width;
                    break;
                case WatermarkPosition.Center:
                    maxTextWithOnImage = info.Image.Width;
                    break;
                default:
                    info.DrawingContext.RotateTransform(45);
                    break;
            }

            var lines = BreakTextIntoLines(info, maxTextWithOnImage, charSize).ToList();

            DrawLines(lines, info);
        }

        public IO.Stream GetPreviewImagesDocumentStream(Content content, DocumentFormat? documentFormat = null, RestrictionType? restrictionType = null)
        {
            if (!documentFormat.HasValue)
                documentFormat = DocumentFormat.NonDefined;

            var pImages = GetPreviewImages(content);
            return GetPreviewImagesDocumentStream(content, pImages.AsEnumerable().Select(c => c.ContentHandler as Image), documentFormat.Value, restrictionType);
        }

        protected virtual IO.Stream GetPreviewImagesDocumentStream(Content content, IEnumerable<Image> previewImages, DocumentFormat documentFormat, RestrictionType? restrictionType = null)
        {
            throw new NotImplementedException("Please implement PDF generator mechanism in your custom preview provider.");
        }

        protected virtual int GetPreviewImagePageIndex(Image image)
        {
            if (image == null)
                return 0;

            // preview5.png --> 5
            var r = new Regex(PREVIEW_THUMBNAIL_REGEX);
            var m = r.Match(image.Name);

            return m.Success ? Convert.ToInt32(m.Groups["page"].Value) : 0;
        }

        protected virtual Node GetPreviewsFolder(Node content)
        {
            var previewsFolderPath = RepositoryPath.Combine(content.Path, PREVIEWS_FOLDERNAME);
            var previewsFolder = Node.Load<GenericContent>(previewsFolderPath);
            if (previewsFolder == null)
            {
                using (new SystemAccount())
                {
                    try
                    {
                        // preview folders and images should not be versioned
                        previewsFolder = new SystemFolder(content)
                        {
                            Name = PREVIEWS_FOLDERNAME,
                            VersioningMode = VersioningType.None,
                            InheritableVersioningMode = InheritableVersioningType.None
                        };

                        previewsFolder.DisableObserver(TypeHandler.GetType(NodeObserverNames.NOTIFICATION));
                        previewsFolder.DisableObserver(TypeHandler.GetType(NodeObserverNames.WORKFLOWNOTIFICATION));
                        previewsFolder.Save();
                    }
                    catch (NodeAlreadyExistsException)
                    {
                        // no problem, reload to have the correct node
                        previewsFolder = Node.Load<GenericContent>(previewsFolderPath);
                    }
                }
            }

            var previewSubfolderName = GetPreviewsSubfolderName(content);
            var previewsSubfolderPath = RepositoryPath.Combine(previewsFolderPath, previewSubfolderName);
            var previewsSubfolder = Node.LoadNode(previewsSubfolderPath);
            if (previewsSubfolder == null)
            {
                using (new SystemAccount())
                {
                    try
                    {
                        previewsSubfolder = new SystemFolder(previewsFolder) { Name = previewSubfolderName };
                        previewsSubfolder.DisableObserver(TypeHandler.GetType(NodeObserverNames.NOTIFICATION));
                        previewsSubfolder.DisableObserver(TypeHandler.GetType(NodeObserverNames.WORKFLOWNOTIFICATION));
                        previewsSubfolder.Save();
                    }
                    catch (NodeAlreadyExistsException)
                    {
                        // no problem, reload to have the correct node
                        previewsSubfolder = Node.LoadNode(previewsSubfolderPath);
                    }
                }
            }

            return previewsSubfolder;
        }
        protected virtual Node EmptyPreviewsFolder(Node previews)
        {
            var gc = previews as GenericContent;
            if (gc == null)
                return null;

            using (new SystemAccount())
            {
                var parent = previews.Parent;
                var name = previews.Name;
                var type = previews.NodeType.Name;

                previews.DisableObserver(TypeHandler.GetType(NodeObserverNames.NOTIFICATION));
                previews.ForceDelete();

                var content = Content.CreateNew(type, parent, name);
                content.ContentHandler.DisableObserver(TypeHandler.GetType(NodeObserverNames.NOTIFICATION));
                content.ContentHandler.DisableObserver(TypeHandler.GetType(NodeObserverNames.WORKFLOWNOTIFICATION));
                content.Save();

                return Node.LoadNode(content.Id);
            }
        }

        /// <summary>
        /// Starts copying previously generated preview images on a new thread under a newly created version.
        /// </summary>
        /// <param name="previousVersion">Previous version.</param>
        /// <param name="currentVersion">Current version that has no preview images yet.</param>
        /// <returns>True if preview images were found and the copy operation started.</returns>
        protected internal virtual bool StartCopyingPreviewImages(Node previousVersion, Node currentVersion)
        {
            // do not throw an error here: this is an internal speedup feature
            if (previousVersion == null || currentVersion == null)
                return false;
            
            var prevFolder = GetPreviewsFolder(previousVersion);
            var currentFolder = GetPreviewsFolder(currentVersion);

            if (prevFolder == null || currentFolder == null)
                return false;

            // collect all preview and thumbnail images
            var previewIds = NodeQuery.QueryNodesByTypeAndPath(NodeType.GetByName("PreviewImage"), false, prevFolder.Path + RepositoryPath.PathSeparator, true).Identifiers.ToList();

            if (previewIds.Count == 0) 
                return false;

            // copy images on a new thread
            System.Threading.Tasks.Task.Run(() =>
            {
                // make sure that there is no existing preview image in the target folder
                currentFolder = EmptyPreviewsFolder(currentFolder);

                var errors = new List<Exception>();

                Node.Copy(previewIds, currentFolder.Path, ref errors);
            });

            return true;
        }

        /// <summary>
        /// Deletes preview images for one version or for all of them asynchronously.
        /// </summary>
        /// <param name="nodeId">Id of the content.</param>
        /// <param name="version">Version that needs preview cleanup.</param>
        /// <param name="allVersions">Whether to cleanup all preview images or just for one version.</param>
        public virtual System.Threading.Tasks.Task RemovePreviewImagesAsync(int nodeId, VersionNumber version = null, bool allVersions = false)
        {
            if (!allVersions && version == null)
                throw  new ArgumentNullException("version");

            return System.Threading.Tasks.Task.Run(() =>
            {
                using (new SystemAccount())
                {
                    var head = NodeHead.Get(nodeId);
                    if (head == null)
                        return;

                    // collect deletable versions
                    var versions = allVersions
                        ? head.Versions.Select(nv => nv.VersionNumber)
                        : new[] { version };

                    // delete previews folders
                    foreach (var nodeVersion in versions)
                    {
                        // simulate the behavior of the GetPreviewsSubfolderName method
                        var previewsPath = RepositoryPath.Combine(head.Path, PREVIEWS_FOLDERNAME, nodeVersion.ToString());

                        try
                        {
                            // existence check to avoid exceptions
                            if (Node.Exists(previewsPath))
                                Node.ForceDelete(previewsPath);
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteException(ex);
                        }
                    }
                }
            });
        }

        //===================================================================================================== Static access

        public static void StartPreviewGeneration(Node node, TaskPriority priority = TaskPriority.Normal)
        {
            var previewProvider = DocumentPreviewProvider.Current;
            if (previewProvider == null)
                return;

            //check if the feature is enabled on the content type
            var content = Content.Create(node);
            if (!content.ContentType.Preview)
                return;

            // check if content is supported by the provider. if not, don't bother starting the preview generation)
            if (!previewProvider.IsContentSupported(node) || previewProvider.IsPreviewOrThumbnailImage(NodeHead.Get(node.Id)))
            {
                DocumentPreviewProvider.SetPreviewStatus(node as File, PreviewStatus.NotSupported);
                return;
            }

            DocumentPreviewProvider.StartPreviewGenerationInternal(node, priority: priority);
        }

        private static void StartPreviewGenerationInternal(Node relatedContent, int startIndex = 0, TaskPriority priority = TaskPriority.Normal)
        {
            if (DocumentPreviewProvider.Current == null || DocumentPreviewProvider.Current is DefaultDocumentPreviewProvider)
            {
                Logger.WriteVerbose("Preview image generation is available only in the enterprise edition. No document preview provider is present.");
                return;
            }

            string previewData;
            var maxPreviewCount = Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, MAXPREVIEWCOUNT, relatedContent.Path, 10);
            var roundedStartIndex = startIndex - startIndex % maxPreviewCount;

            // serialize data for preview generator task (json format)
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());

            using (var sw = new IO.StringWriter())
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, new
                    {
                        Id = relatedContent.Id,
                        Version = relatedContent.Version.ToString(),
                        StartIndex = roundedStartIndex,
                        MaxPreviewCount = maxPreviewCount
                    });
                }

                previewData = sw.GetStringBuilder().ToString();
            }

            // start generating previews only if there is a task type defined
            var taskName = DocumentPreviewProvider.Current.GetPreviewGeneratorTaskName(relatedContent.Path);
            if (!string.IsNullOrEmpty(taskName))
                TaskManager.RegisterTask(taskName, priority, previewData);
        }

        public static void SetPreviewStatus(File file, PreviewStatus status)
        {
            if (file == null)
                return;

            if (status == PreviewStatus.Ready)
                throw new NotSupportedException("Setting preview status to Ready is not supported. This scenario is handled by the document preview provider itself.");

            try
            {
                SavePageCount(file, (int)status);
            }
            catch (Exception ex)
            {
                Logger.WriteWarning(ContentRepository.EventId.Preview.PreviewGenerationStatusError, "Error setting preview status. " + ex,
                                  properties: new Dictionary<string, object>
                                                  {
                                                      {"Path", file.Path},
                                                      {"Status", Enum.GetName(typeof (PreviewStatus), status)}
                                                  });
            }
        }
        
        //===================================================================================================== OData interface

        [ODataFunction]
        public static IEnumerable<Content> GetPreviewImagesForOData(Content content)
        {
            return Current != null ? Current.GetPreviewImages(content) : null;
        }

        [ODataFunction]
        public static object PreviewAvailable(Content content, int page)
        {
            var thumb = Current != null ? Current.GetThumbnailImage(content, page) : null;
            if (thumb != null)
            {
                var pi = Current != null ? Current.GetPreviewImage(content, page) : null;
                if (pi != null)
                {
                    return new
                    {
                        PreviewAvailable = pi.Path,
                        Width = (int)pi["Width"],
                        Height = (int)pi["Height"]
                    };
                }
            }

            return new { PreviewAvailable = (string)null };
        }

        [ODataAction]
        public static int GetPageCount(Content content)
        {
            var pageCount = (int)content["PageCount"];
            var file = content.ContentHandler as File;

            //default DocumentPreviewProvider is the current provider --> set status to noprovider
            if (DocumentPreviewProvider.Current is DefaultDocumentPreviewProvider && pageCount == -4)
            {
                pageCount = (int)PreviewStatus.NoProvider;
            }
            else
            {
                if (pageCount == -4)
                {
                    //status is postponed --> set status to inprogress and start preview generation
                    SetPreviewStatus(file, PreviewStatus.InProgress);

                    pageCount = (int)PreviewStatus.InProgress;
                    StartPreviewGeneration(file, TaskPriority.Immediately);
                }
                else if (pageCount == -1)
                {
                    StartPreviewGeneration(file, TaskPriority.Immediately);
                }
            }
            return pageCount;
        }

        [ODataAction]
        public static object GetPreviewsFolder(Content content, bool empty)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var previewsFolder = DocumentPreviewProvider.Current.GetPreviewsFolder(content.ContentHandler);

            if (empty)
                previewsFolder = DocumentPreviewProvider.Current.EmptyPreviewsFolder(previewsFolder);

            return new
            {
                Id = previewsFolder.Id,
                Path = previewsFolder.Path
            };
        }

        [ODataAction]
        public static void SetPreviewStatus(Content content, PreviewStatus status)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            SetPreviewStatus(content.ContentHandler as File, status);
        }

        [ODataAction]
        public static void SetPageCount(Content content, int pageCount)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            SavePageCount(content.ContentHandler as File, pageCount);
        }

        [ODataAction]
        public static void SetInitialPreviewProperties(Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var previewImage = content.ContentHandler as Image;
            if (previewImage == null)
                throw new InvalidOperationException("This content is not an image.");

            var document = GetDocumentForPreviewImage(NodeHead.Get(content.Id));
            if (document == null)
                throw new InvalidOperationException("Document not found for preview image: " + content.Path);

            var realCreatorUser = document.CreatedBy;

            // set the creator/modifier user of the preview image: it should be 
            // the owner of the document, instead of admin
            previewImage.CreatedBy = realCreatorUser;
            previewImage.ModifiedBy = realCreatorUser;
            previewImage.VersionCreatedBy = realCreatorUser;
            previewImage.VersionModifiedBy = realCreatorUser;
            previewImage.Index = DocumentPreviewProvider.Current.GetPreviewImagePageIndex(previewImage);

            previewImage.Save(SavingMode.KeepVersion);
        }

        [ODataAction]
        public static void RegeneratePreviews(Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            // Regardless of the current status, generate preview images again
            // (e.g. because previously there was an error).
            SetPreviewStatus(content.ContentHandler as File, PreviewStatus.InProgress);
            StartPreviewGeneration(content.ContentHandler, TaskPriority.Immediately);
        }
    }

    public sealed class DefaultDocumentPreviewProvider : DocumentPreviewProvider
    {
        public override string GetPreviewGeneratorTaskName(string contentPath)
        {
            return string.Empty;
        }

        public override string[] GetSupportedTaskNames()
        {
            return new string[0];
        }

        public override bool IsContentSupported(Node content)
        {
            //in community edition support only files stored in libraries
            return content != null && content is File && content.ContentListId > 0;
        }
    }
}
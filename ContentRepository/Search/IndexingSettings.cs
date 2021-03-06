using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ContentRepository;
using ContentRepository.Fields;
using ContentRepository.Schema;
using ContentRepository.Storage;
using ContentRepository.Storage.Events;
using Diagnostics;

namespace Search
{
    [ContentHandler]
    public class IndexingSettings : Settings
    {
        //================================================================================= Constructors

        public IndexingSettings(Node parent) : this(parent, null) { }
        public IndexingSettings(Node parent, string nodeTypeName) : base(parent, nodeTypeName) {}
        protected IndexingSettings(NodeToken nt) : base(nt) { }

        //================================================================================= Properties

        private const string ASPOSE_PDF_TEXTEXTRACTOR_NAME = "AsposePreviewProvider.AsposePdfTextExtractor";
        private const string TEXTEXTRACTORS_TEXTFIELDNAME = "TextExtractors";
        internal const string TEXTEXTRACTORS_PROPERTYNAME = "TextExtractorInstances";
        internal const string SETTINGSNAME = "Indexing";

        private static object _extractorLock = new object();
        private ReadOnlyDictionary<string, ITextExtractor> _textExtractors;
        public ReadOnlyDictionary<string, ITextExtractor> TextExtractorInstances
        {
            get
            {
                if (_textExtractors == null)
                {
                    lock (_extractorLock)
                    {
                        if (_textExtractors == null)
                        {
                            _textExtractors = new ReadOnlyDictionary<string, ITextExtractor>(LoadTextExtractors());

                            SetCachedData(TEXTEXTRACTORS_CACHEKEY, _textExtractors);

                            Logger.WriteInformation(Logger.EventId.NotDefined, "Text extractors were created.", properties: _textExtractors.ToDictionary(t => t.Key, t => (object)t.Value.GetType().FullName));
                        }
                    }
                }

                return _textExtractors;
            }
        }

        /// <summary>
        /// This method collects all the available text extractors. First it builds a list of the built-in
        /// extractors than adds the dynamic ones from the setting (they can override the built-in ones).
        /// </summary>
        private Dictionary<string, ITextExtractor> LoadTextExtractors()
        {
            var extractors = new Dictionary<string, ITextExtractor>
                            {
                                {"contenttype", new XmlTextExtractor()},
                                {"xml", new XmlTextExtractor()},
                                {"doc", new DocTextExtractor()},
                                {"xls", new XlsTextExtractor()},
                                {"xlb", new XlbTextExtractor()},
                                {"msg", new MsgTextExtractor()},
                                {"pdf", new PdfTextExtractor()},
                                {"docx", new DocxTextExtractor()},
                                {"docm", new DocxTextExtractor()},
                                {"xlsx", new XlsxTextExtractor()},
                                {"xlsm", new XlsxTextExtractor()},
                                {"pptx", new PptxTextExtractor()},
                                {"txt", new PlainTextExtractor()},
                                {"rtf", new RtfTextExtractor()}
                            };

            var isCommunity = RepositoryVersionInfo.Instance != null &&
                RepositoryVersionInfo.Instance.OfficialVersion != null &&
                RepositoryVersionInfo.Instance.OfficialVersion.Edition == "Community";

            // add text extractors available only in the Enterprise Edition
            if (!isCommunity)
            {
                try
                {
                    extractors["pdf"] = (ITextExtractor)TypeHandler.CreateInstance(ASPOSE_PDF_TEXTEXTRACTOR_NAME);
                }
                catch (Exception ex)
                {
                    Logger.WriteWarning(EventId.Indexing.TextExtractorError, "Text extractor type could not be instatiated (Aspose Pdf extractor). " + ex);
                }
            }

            // load text extractor settings (they may override the defaults listed above)
            foreach (var field in this.Content.Fields.Values.Where(field => field.Name.StartsWith(TEXTEXTRACTORS_TEXTFIELDNAME + ".")))
            {
                var extractorName = field.GetData() as string;
                if (string.IsNullOrEmpty(extractorName))
                    continue;

                extractorName = extractorName.Trim('.', ' ');

                // make sure that only Enterprise customers can use the Aspose provider
                if (isCommunity && string.Compare(extractorName, ASPOSE_PDF_TEXTEXTRACTOR_NAME, StringComparison.InvariantCulture) == 0)
                    continue;

                try
                {
                    var extension = field.Name.Substring(field.Name.LastIndexOf('.')).Trim('.', ' ').ToLower();

                    extractors[extension] = (ITextExtractor)TypeHandler.CreateInstance(extractorName);
                }
                catch (Exception ex)
                {
                    Logger.WriteWarning(EventId.Indexing.TextExtractorError, "Text extractor type could not be instatiated: " + extractorName + " " + ex);
                }
            }

            return extractors;
        }

        //================================================================================= Overrides

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case TEXTEXTRACTORS_PROPERTYNAME:
                    return this.TextExtractorInstances;
                default:
                    return base.GetProperty(name);
            }
        }

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case TEXTEXTRACTORS_PROPERTYNAME:
                    // this is a readonly property
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        //================================================================================= Cached data

        private const string TEXTEXTRACTORS_CACHEKEY = "CachedTextExtractors";

        protected override void OnLoaded(object sender, NodeEventArgs e)
        {
            base.OnLoaded(sender, e);

            _textExtractors = (ReadOnlyDictionary<string, ITextExtractor>)GetCachedData(TEXTEXTRACTORS_CACHEKEY);
        }
    }

    [ShortName("TextExtractors")]
    [DataSlot(0, RepositoryDataType.NotDefined, typeof(ReadOnlyDictionary<string, ITextExtractor>))]
    [DefaultFieldSetting(typeof(NullFieldSetting))]
    [DefaultFieldControl("Portal.UI.Controls.ShortText")]
    public class TextExtractorsField : Field
    {
        protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
        {
            throw new NotSupportedException("The ImportData operation is not supported on TextExtractorsField.");
        }

        protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
        {
            // do not export this field, it is autogenerated in the contetn handler
        }
    }
}

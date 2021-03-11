using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Preview
{
    public class Common
    {
        public static readonly string LICENSEPATH = "Aspose.Total.lic";

        public static readonly string PREVIEW_IMAGENAME = "preview{0}.png";
        public static readonly int PREVIEW_WIDTH = 1240;
        public static readonly int PREVIEW_HEIGHT = 1754;
        public static readonly int PREVIEW_POWERPOINT_WIDTH = 1280;
        public static readonly int PREVIEW_POWERPOINT_HEIGHT = 960;

        public static readonly string THUMBNAIL_IMAGENAME = "thumbnail{0}.png";
        public static readonly int THUMBNAIL_WIDTH = 200;
        public static readonly int THUMBNAIL_HEIGHT = 200;

        public static readonly System.Drawing.Imaging.ImageFormat PREVIEWIMAGEFORMAT = System.Drawing.Imaging.ImageFormat.Png;



        public static readonly string[] WORD_EXTENSIONS = new[] { ".doc", ".docx", ".odt", ".rtf", ".txt", ".xml", ".csv" };
        public static readonly string[] DIAGRAM_EXTENSIONS = new[] { ".vdw", ".vdx", ".vsd", ".vss", ".vst", ".vsx", ".vtx" };
        public static readonly string[] IMAGE_EXTENSIONS = new[] { ".gif", ".jpg", ".jpeg", ".bmp", ".png", ".svg", ".exif", ".icon" };
        public static readonly string[] TIFF_EXTENSIONS = new[] { ".tif", ".tiff" };
        public static readonly string[] WORKBOOK_EXTENSIONS = new[] { ".ods", ".xls", ".xlsm", ".xlsx", ".xltm", ".xltx" };
        public static readonly string[] PDF_EXTENSIONS = new[] { ".pdf" };
        public static readonly string[] PRESENTATION_EXTENSIONS = new[] { ".pot", ".pps", ".ppt" };
        public static readonly string[] PRESENTATIONEX_EXTENSIONS = new[] { ".potx", ".ppsx", ".pptx", ".odp" };
        public static readonly string[] PROJECT_EXTENSIONS = new[] { ".mpp" };
        public static readonly string[] EMAIL_EXTENSIONS = new[] { ".msg" };

        //missing: ".emf", ".pcl", ".wmf", ".xps"
        private static readonly string[] SUPPORTED_EXTENSIONS =
            WORD_EXTENSIONS
            .Concat(DIAGRAM_EXTENSIONS)
            .Concat(IMAGE_EXTENSIONS)
            .Concat(TIFF_EXTENSIONS)
            .Concat(WORKBOOK_EXTENSIONS)
            .Concat(PDF_EXTENSIONS)
            .Concat(PRESENTATION_EXTENSIONS)
            .Concat(PRESENTATIONEX_EXTENSIONS)
            .Concat(PROJECT_EXTENSIONS)
            .Concat(EMAIL_EXTENSIONS)
            .ToArray();

    }
}

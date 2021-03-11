using System.Drawing;
using System.ServiceModel;

namespace FileServiceLibrary
{
    [ServiceContract]
    public interface IFileService
    {
        [OperationContract]
        void ConvertToPDF(string inputFile, string outputFile);

        [OperationContract]
        bool IsSupportedExtension(string extension);

        [OperationContract]
        Size GetSuggestImageSize(string extension, bool isThumbnail);
    }
}

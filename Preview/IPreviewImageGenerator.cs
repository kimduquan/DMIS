using System.IO;

namespace Preview
{
    public interface IPreviewImageGenerator
    {
        string[] KnownExtensions { get; }
        string GetTaskNameByExtension(string extension);
        string[] GetSupportedTaskNames();
        void GeneratePreview(Stream docStream, IPreviewGenerationContext context);
    }

}

using ContentRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Preview
{
    public class DMISDocumentPreviewProvider : DocumentPreviewProvider
    {
        public override bool IsContentSupported(ContentRepository.Storage.Node content)
        {
            var previewImage = Content.Load(content.Id).ContentHandler as Image;
            if (previewImage != null)
                return false;
            return true;
        }

        public override string GetPreviewGeneratorTaskName(string contentPath)
        {
            return "DocumentPreviewGenerator";
        }

        public override string[] GetSupportedTaskNames()
        {
            string[] taskNames = new string[] { "DocumentPreviewGenerator" };
            return taskNames;
        }
    }
}

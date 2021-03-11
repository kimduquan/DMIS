using System;
using System.IO;
using System.Linq;
using System.Web;
using ContentRepository;
using ContentRepository.Storage;
using File = System.IO.File;

namespace Packaging.Steps
{
    public class ReplaceText : Step
    {
        private static readonly string CDATAStart = "<![CDATA[";
        private static readonly string CDATAEnd = "]]>";

        [DefaultProperty]
        public string Value { get; set; }
        public string Path { get; set; }
        public string Template { get; set; }
        public string Field { get; set; }

        public override void Execute(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(Path) || string.IsNullOrEmpty(Template))
                throw new PackagingException(SR.Errors.InvalidParameters);

            // if Path refers to a content
            if (RepositoryPath.IsValidPath(Path) == RepositoryPath.PathResult.Correct)
            {
                ExecuteOnContent(context);
                return;
            }

            // replace values in text files
            foreach (var targetPath in ResolveAllTargets(Path, context).Where(p => File.Exists(p)))
            {
                // we do not want to catch exceptions here: the step should fail in case of an error
                var text = File.ReadAllText(targetPath);
                text = text.Replace(Template, GetSource());
                File.WriteAllText(targetPath, text);
            }
        }

        private void ExecuteOnContent(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var content = Content.Load(Path);
            var data = content[Field ?? "Binary"];

            BinaryData binaryData = null;
            var text = data as string;
            if (text == null)
            {
                binaryData = data as BinaryData;
                if (binaryData != null)
                {
                    using (var r = new StreamReader(binaryData.GetStream()))
                        text = r.ReadToEnd();
                }
            }

            if (text == null) 
                return;

            text = text.Replace(Template, GetSource());

            if (binaryData != null)
                binaryData.SetStream(Tools.GetStreamFromString(text));
            else
                content[Field] = text;

            content.SaveSameVersion();
        }

        private string GetSource()
        {
            var text = Value ?? string.Empty;

            // CDATA workaround
            if (text.StartsWith(CDATAStart) && text.EndsWith(CDATAEnd))
                return text.Substring(CDATAStart.Length, text.Length - (CDATAStart.Length + CDATAEnd.Length));

            return text;
        }
    }
}

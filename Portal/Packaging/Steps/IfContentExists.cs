using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContentRepository;
using ContentRepository.Storage;
using Packaging;
using Packaging.Steps;

namespace Packaging.Steps
{
    public class IfContentExists : ConditionalStep
    {
        public string Path { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            return !string.IsNullOrEmpty(Path) && Node.Exists(Path);
        }
    }

    public class IfFieldExists : ConditionalStep
    {
        public string Field { get; set; }
        public string ContentType { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            if (string.IsNullOrEmpty(ContentType) || string.IsNullOrEmpty(Field))
                throw new PackagingException("ContentType or Field is empty.");

            var ct = ContentRepository.Schema.ContentType.GetByName(ContentType);
            if (ct == null)
                throw new PackagingException("ContentType not found: " + ContentType);

            return ct.FieldSettings.Any(fs => string.Compare(fs.Name, Field, StringComparison.Ordinal) == 0);
        }
    }

    public class IfFileExists : ConditionalStep
    {
        public string Path { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            return !string.IsNullOrEmpty(Path) && System.IO.File.Exists(ResolveTargetPath(Path, context));
        }
    }

    public class IfDirectoryExists : ConditionalStep
    {
        public string Path { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            return !string.IsNullOrEmpty(Path) && System.IO.Directory.Exists(ResolveTargetPath(Path, context));
        }
    }
}

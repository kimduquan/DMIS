using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Packaging.Steps
{
    class DisableIndexing : Step
    {
        public override void Execute(ExecutionContext context)
        {
            ContentRepository.Storage.StorageContext.Search.DisableOuterEngine();
        }
    }
}

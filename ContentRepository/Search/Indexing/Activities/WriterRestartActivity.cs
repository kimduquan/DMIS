using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Diagnostics;

namespace Search.Indexing.Activities
{
    [Serializable]
    internal class WriterRestartActivity : DistributedLuceneActivity
    {
        internal override void Execute()
        {
            using (var optrace = new OperationTrace("WriterRestartActivity Execute"))
            {
                LuceneManager.Restart();
                optrace.IsSuccessful = true;
            }
        }
    }
}

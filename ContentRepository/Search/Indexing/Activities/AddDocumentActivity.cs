using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Index;
using Diagnostics;
using ContentRepository.Storage;
using Lucene.Net.Documents;
using ContentRepository.Storage.Search;
using System.Threading;
using Search.Indexing;

namespace Search.Indexing.Activities
{
    [Serializable]
    internal class AddDocumentActivity : LuceneDocumentActivity
    {
        internal override void Execute()
        {
            try
            {
                if (Document != null)
                {
                    if (true == this.SingleVersion)
                        LuceneManager.AddCompleteDocument(Document, this.ActivityId, this.FromExecutingUnprocessedActivities);
                    else
                        LuceneManager.AddDocument(Document, this.ActivityId, this.FromExecutingUnprocessedActivities);
                }
            }
            finally
            {
                base.Execute();
            }
        }
    }
}
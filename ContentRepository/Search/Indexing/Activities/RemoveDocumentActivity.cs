using System;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Search.Indexing.Activities
{
    [Serializable]
    internal class RemoveDocumentActivity : LuceneDocumentActivity
    {
        internal override void Execute()
        {
            try
            {
                var delTerm = new Term(LuceneManager.KeyFieldName, NumericUtils.IntToPrefixCoded(this.VersionId));
                LuceneManager.DeleteDocuments(new[] { delTerm }, MoveOrRename, this.ActivityId, this.FromExecutingUnprocessedActivities);

#if INDEX
                LuceneManager.LogVersionFlagConsistency(this.NodeId, "RemoveDocumentActivity.DeleteDocuments", activityId: this.ActivityId);
#endif
            }
            finally
            {
                base.Execute();
            }
        }

        public override Lucene.Net.Documents.Document CreateDocument()
        {
            throw new InvalidOperationException();
        }
    }
}
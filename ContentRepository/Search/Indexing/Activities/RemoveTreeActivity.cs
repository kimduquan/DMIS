using System;
using Lucene.Net.Index;

namespace Search.Indexing.Activities
{
    [Serializable]
    internal class RemoveTreeActivity : LuceneTreeActivity
    {
        internal override void Execute()
        {
            try
            {
                var terms = new[] { new Term("InTree", TreeRoot), new Term("Path", TreeRoot) };
                LuceneManager.DeleteDocuments(terms, MoveOrRename, this.ActivityId, this.FromExecutingUnprocessedActivities);
#if INDEX
                LuceneManager.LogVersionFlagConsistency(this.NodeId, "RemoveTreeActivity.DeleteDocuments", activityId: this.ActivityId);
#endif
            }
            finally
            {
                base.Execute();
            }
        }
    }
}
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
using ContentRepository.Storage.Data;

namespace Search.Indexing.Activities
{
    [Serializable]
    internal class AddTreeActivity : LuceneTreeActivity
    {
        private Document[] Documents { get; set; }

        private IEnumerable<Node> GetVersions(Node node)
        {
            var versionNumbers = Node.GetVersionNumbers(node.Id);
            var versions = from versionNumber in versionNumbers select Node.LoadNode(node.Id, versionNumber);
            var versionsArray = versions.ToArray();
            return versionsArray;
        }

        internal override void Execute()
        {
            try
            {
                LuceneManager.AddTree(TreeRoot, this.ActivityId, this.FromExecutingUnprocessedActivities);
            }
            finally
            {
                base.Execute();
            }
        }
    }
}

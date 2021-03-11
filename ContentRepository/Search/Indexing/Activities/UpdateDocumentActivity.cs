using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Index;
using Diagnostics;
using System.Diagnostics;
using ContentRepository.Storage;
using Lucene.Net.Documents;
using Lucene.Net.Util;

namespace Search.Indexing.Activities
{
    [Serializable]
    internal class UpdateDocumentActivity : LuceneDocumentActivity
    {
        internal static Term GetIdTerm(Document document)
        {
            var versionID =
                 NumericUtils.IntToPrefixCoded(Int32.Parse((string)document.GetFieldable(LuceneManager.KeyFieldName).StringValue()));
            if (string.IsNullOrEmpty(versionID))
                throw new ApplicationException("VersionID field missing");

            var term = new Term(LuceneManager.KeyFieldName, versionID);
            return term;
        }

        internal override void Execute()
        {
            try
            {
                if (Document != null)
                    LuceneManager.UpdateDocument(GetIdTerm(Document), Document, this.ActivityId, this.FromExecutingUnprocessedActivities);
            }
            finally
            {
                base.Execute();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Lucene.Net.Documents;
using ContentRepository.Storage;
using ContentRepository.Storage.Data;
using Search.Indexing.Activities;

namespace Search.Indexing
{
    public enum IndexingActivityType
    {
        AddDocument = 1,
        AddTree = 2,
        UpdateDocument = 3,
        RemoveDocument = 4,
        RemoveTree = 5
    }

    public partial class IndexingActivity : INotifyPropertyChanging, INotifyPropertyChanged
    {
        public IndexDocumentData IndexDocumentData;
        public bool FromExecutingUnprocessedActivities { get; internal set; }

        internal LuceneIndexingActivity CreateLuceneActivity()
        {
            return IndexingActivityManager.CreateLucActivity(this);
        }
        partial void OnCreated()
        {
            this.CreationDate = DateTime.UtcNow;
        }
    }
    
}

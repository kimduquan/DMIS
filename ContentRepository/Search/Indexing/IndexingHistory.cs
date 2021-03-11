using System;
using System.Collections.Generic;
using System.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;
using ContentRepository.Storage.Data;

namespace Search.Indexing
{
    internal class Timestamps : IComparable<Timestamps>
    {
        public static readonly Timestamps MaxValue = new Timestamps(long.MaxValue, long.MaxValue);
        public static readonly Timestamps MinValue = new Timestamps(long.MinValue, long.MinValue);

        public int OwnerId { get; internal set; }

        public readonly long NodeTimestamp;
        public readonly long VersionTimestamp;

        public Timestamps(long nodeTimestamp, long versionTimestamp)
        {
            NodeTimestamp = nodeTimestamp;
            VersionTimestamp = versionTimestamp;

            // Owner means that a particular thread added or modified this entry. We check
            // this when it is important that the entry was not modified by another thread.
            OwnerId = Thread.CurrentThread.ManagedThreadId;
        }

        public static bool operator ==(Timestamps x, Timestamps y) { return Compare(x, y) == 0; }
        public static bool operator !=(Timestamps x, Timestamps y) { return Compare(x, y) != 0; }
        public static bool operator >(Timestamps x, Timestamps y) { return Compare(x, y) > 0; }
        public static bool operator <(Timestamps x, Timestamps y) { return Compare(x, y) < 0; }
        public static bool operator >=(Timestamps x, Timestamps y) { return Compare(x, y) >= 0; }
        public static bool operator <=(Timestamps x, Timestamps y) { return Compare(x, y) <= 0; }

        private static int Compare(Timestamps x, Timestamps y)
        {
            var q = 0;

            // This would not be necessary in case of a struct, but this is a class. And
            // we cannot use the '==' operator for equality check because it would lead
            // to infinite recursion (stack overflow exception).
            if (object.ReferenceEquals(null, x))
                return object.ReferenceEquals(null, y) ? q : -1;

            if (object.ReferenceEquals(null, y))
                return 1;

            if ((q = x.NodeTimestamp.CompareTo(y.NodeTimestamp)) != 0)
                return q;
            return x.VersionTimestamp.CompareTo(y.VersionTimestamp);
        }
        public int CompareTo(Timestamps other)
        {
            return Compare(this, other);
        }

        public override int GetHashCode()
        {
            return (VersionTimestamp - NodeTimestamp).GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return this == (Timestamps)obj;
        }
        public override string ToString()
        {
            return "[" + NodeTimestamp + "," + VersionTimestamp + "]";
        }
    }

    internal class IndexingHistory
    {
        private object _sync = new object();

        int _limit;
        Queue<int> _queue;
        Dictionary<int, Timestamps> _storage;

        public long Count { get { return _storage.Count; } }

        public IndexingHistory()
        {
            Initialize(RepositoryConfiguration.IndexHistoryItemLimit);
        }
        private void Initialize(int size)
        {
            _limit = size;
            _queue = new Queue<int>(size);
            _storage = new Dictionary<int, Timestamps>(size);
        }

        internal int GetVersionId(Document doc)
        {
            return Int32.Parse(doc.Get(LucObject.FieldName.VersionId));
        }
        internal Timestamps GetTimestamp(Document doc)
        {
            return new Timestamps(Int64.Parse(doc.Get(LucObject.FieldName.NodeTimestamp)), Int64.Parse(doc.Get(LucObject.FieldName.VersionTimestamp)));
        }
        internal bool IsVersionCanBeAdded(int versionId, Timestamps timestamps)
        {
            //Debug.WriteLine(String.Format("##> IsVersionCanBeAdded. Id: {0}, time: {1}", versionId, timestamp));
            lock (_sync)
            {
                if (Exists(versionId)) 
                    return false;

                Add(versionId, timestamps);
                return true;
            }
        }
        internal bool IsVersionCanBeUpdated(int versionId, Timestamps timestamps)
        {
            //Debug.WriteLine(String.Format("##> IsVersionCanBeUpdated. Id: {0}, time: {1}", versionId, timestamp));
            lock (_sync)
            {
                var stored = Get(versionId);
                if (stored == null)
                {
                    Add(versionId, timestamps);
                    return true;
                }
                
                // change the owner to be the current thread
                stored.OwnerId = Thread.CurrentThread.ManagedThreadId;

                // the stored item is newer, the caller must not update this version
                if (stored >= timestamps)
                    return false;

                Update(versionId, timestamps);
                return true;
            }
        }
        internal void ProcessDelete(Term[] deleteTerms)
        {
            //Debug.WriteLine("##> ProcessDelete. Count: " + deleteTerms.Length);
            for (int i = 0; i < deleteTerms.Length; i++)
            {
                var term = deleteTerms[i];
                if (term.Field() != LucObject.FieldName.VersionId)
                    return;
                var versionId = NumericUtils.PrefixCodedToInt(term.Text());
                ProcessDelete(versionId);
            }
        }
        internal void ProcessDelete(int versionId)
        {
            lock (_sync)
            {
                if (!Exists(versionId))
                    Add(versionId, Timestamps.MaxValue);
                else
                    Update(versionId, Timestamps.MaxValue);
            }
        }
        internal void Remove(Term[] deleteTerms)
        {
            lock (_sync)
            {
                foreach (var deleteTerm in deleteTerms)
                {
                    //var executor = new QueryExecutor20100701();
                    var q = new TermQuery(deleteTerm);
                    var lucQuery = LucQuery.Create(q);
                    lucQuery.EnableAutofilters = FilterStatus.Disabled;
                    //var result = executor.Execute(lucQuery, true);
                    var result = lucQuery.Execute(true);
                    foreach (var lucObject in result)
                        _storage.Remove(lucObject.VersionId);
                }
            }
        }
        internal bool RemoveIfLast(int versionId, Timestamps timestamps)
        {
            lock (_sync)
            {
                var last = Get(versionId);
                if (last != null && last == timestamps)
                {
                    _storage.Remove(versionId);
                    return true;
                }
                return false;
            }
        }
        internal bool IsVersionChanged(int versionId, Timestamps timestamps, bool checkOwner = false)
        {
            // Checking the owner means we want to make sure that (even if the 
            // timestamps are equal) the entry was not modified by another thread.

            lock (_sync)
            {
                var lastTimestamp = Get(versionId);
                return timestamps != lastTimestamp || (checkOwner && lastTimestamp != null && lastTimestamp.OwnerId != timestamps.OwnerId);
            }
        }

        internal bool IsVersionDeleted(int versionId)
        {
            // check if a version id was marked as deleted

            lock (_sync)
            {
                return Get(versionId) == Timestamps.MaxValue;
            }
        }

        internal bool Exists(int versionId)
        {
            return _storage.ContainsKey(versionId);
        }
        internal Timestamps Get(int versionId)
        {
            Timestamps result;
            if (_storage.TryGetValue(versionId, out result))
                return result;
            return null;
        }
        internal void Add(int versionId, Timestamps timestamps)
        {
            _storage.Add(versionId, timestamps);
            _queue.Enqueue(versionId);
            if (_queue.Count <= _limit)
                return;
            var k = _queue.Dequeue();
            _storage.Remove(k);
        }
        internal void Update(int versionId, Timestamps timestamps)
        {
            _storage[versionId] = timestamps;
        }
    }
}

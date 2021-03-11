using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;
using ContentRepository;
using ContentRepository.Storage.Security;
using Utilities;
using ContentRepository.Storage.Data;
using System.Diagnostics;
using ContentRepository.Storage;
using System.Threading;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Diagnostics;
using Communication.Messaging;
//using ContentRepository;
using Search.Indexing.Activities;
using Search.Indexing.Configuration;
using Lucene.Net.Util;
using Field = Lucene.Net.Documents.Field;
using ContentRepository.Storage.Diagnostics;

namespace Search.Indexing
{
    public enum CommitHint { AddNew, AddNewVersion, Update, Rename, Move, Delete } ;

    public static class LuceneManager
    {
        public static readonly Lucene.Net.Util.Version LuceneVersion = Lucene.Net.Util.Version.LUCENE_29;

        internal static IndexingHistory _history = new IndexingHistory();

        /* =========================================================================== TEST COMMIT */
        // This is a code for commit testing. 
        // Put <add key="CommitLogPath" value="c:\\commitlog-1-{0}.csv"/> in the webconfig, name should reflect the actual web node (use "1" for 1st node, "2" for 2nd node, etc.)
        private static bool? _commitLogEnabled;
        private static object CommitLogEnabledSync = new object();
        private static bool CommitLogEnabled
        {
            get
            {
                if (!_commitLogEnabled.HasValue)
                {
                    lock (CommitLogEnabledSync)
                    {
                        if (!_commitLogEnabled.HasValue)
                        {
                            _commitLogEnabled = StartCommitLog();
                        }
                    }
                }
                return _commitLogEnabled.Value;
            }
        }
        private static object CommitLogSync = new object();
        private static System.Timers.Timer CommitLogTimer;
        private static Stopwatch _commitStopper;
        private static string CommitLogPath;
        private static string CommitLog;
        private static void WriteCommitLog(string status, int? gapSize = null, int? gapSizePeak = null, int? activities = null, int? delaycycle = null, string gapString = null, int? maxActivityId = null)
        {
            if (!CommitLogEnabled)
                return;

            long elapsed = 0;
            if (status == "Start" && _commitStopper != null)
                _commitStopper.Restart();
            if ((status == "Committed" || status == "Stop") && _commitStopper != null)
            {
                elapsed = _commitStopper.ElapsedMilliseconds;
                _commitStopper.Restart();
            }

            var line = DateTime.UtcNow.Ticks.ToString() + ";" + DateTime.UtcNow.Hour.ToString() + ";" + DateTime.UtcNow.Minute.ToString() + ";" + DateTime.UtcNow.Second.ToString() + ";";
            if (status == "Info")
            {
                line += status + ";0;" + gapSize.Value.ToString() + ";" + gapSizePeak.Value.ToString() + ";" + activities.Value.ToString() + ";" + delaycycle.Value.ToString() + ";;;" + Environment.NewLine;
            }
            else
            {
                line += status + ";" + elapsed + ";0;0;0;0;" + (maxActivityId.HasValue ? maxActivityId.Value.ToString() : string.Empty) + ";" + gapString + ";" + Environment.NewLine;
            }
            lock (CommitLogSync)
            {
                CommitLog += line;
            }
        }
        private static bool StartCommitLog()
        {
            // eg  "c:\\commitlog-1-{0}.csv"
            var appSettingsCommitLogPath = System.Configuration.ConfigurationManager.AppSettings["CommitLogPath"];
            if (string.IsNullOrEmpty(appSettingsCommitLogPath))
                return false;

            CommitLogPath = string.Format(appSettingsCommitLogPath, DateTime.UtcNow.ToString("yyyyMMddhhmmss"));
            using (var fs = new System.IO.FileStream(CommitLogPath, System.IO.FileMode.Create))
            {
                using (var wr = new System.IO.StreamWriter(fs))
                {
                    wr.WriteLine("ticks;hour;minute;second;status;elapsed;gapsize;gapsizepeak;activities;delaycycle;maxActivityId;gapString");
                }
            }
            _commitStopper = Stopwatch.StartNew();
            CommitLog = string.Empty;
            CommitLogTimer = new System.Timers.Timer(10000.0);
            CommitLogTimer.Elapsed += new System.Timers.ElapsedEventHandler(CommitLogTimer_Elapsed);
            CommitLogTimer.Start();
            return true;
        }
        private static void CommitLogTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (CommitLogSync)
            {
                using (var writer = new System.IO.StreamWriter(CommitLogPath, true))
                {
                    writer.Write(CommitLog);
                }
                CommitLog = string.Empty;
            }
        }
        // =========================================================================== TEST COMMIT END


        public static readonly string KeyFieldName = "VersionId";
        internal static IndexWriter _writer;
        internal static IndexReader _reader;

        public static int IndexCount { get { return 1; } }
        public static int IndexedDocumentCount
        {
            get
            {
                using (var readerFrame = LuceneManager.GetIndexReaderFrame())
                {
                    var idxReader = readerFrame.IndexReader;
                    return idxReader.NumDocs();
                }
            }
        }

        internal static ReaderWriterLockSlim _writerRestartLock = new ReaderWriterLockSlim();
        private const int REOPENRETRYMAX = 2;
        internal static IndexReader IndexReader
        {
            get
            {
                using (var wrFrame = IndexWriterFrame.Get(false)) // // IndexReader getter
                {
                    if (!_reader.IsCurrent())
                        ReopenReader();
                }
                return _reader;
            }
        }
        public static IndexReaderFrame GetIndexReaderFrame()
        {
            return new IndexReaderFrame(IndexReader);
        }

        private static object _startSync = new object();
        private static bool _running;
        public static bool Running
        {
            get { return _running; }
        }

        private static bool _paused;
        internal static bool Paused
        {
            get { return _paused; }
        }

        internal static void PauseIndexing()
        {
            _indexingSemaphore.Reset();

            Commit();
            IndexWriterUsage.WaitForRunOutAllWriters();
            _paused = true;
        }
        internal static void ContinueIndexing()
        {
            if (Running)
                using (new SystemAccount())
                    ExecuteUnprocessedIndexingActivities(null, false);

            _indexingSemaphore.Set();
        }
        internal static Exception GetPausedException(string msg = null)
        {
            if (msg == null)
                msg = "Cannot use the IndexReader if the indexing is paused (i.e. LuceneManager.Paused = true)";
            return new InvalidOperationException(msg);
        }

        internal static ManualResetEventSlim _indexingSemaphore = new ManualResetEventSlim(true);

        internal static void WaitIfIndexingPaused()
        {
            if (!_indexingSemaphore.Wait(Repository.IndexingPausedTimeout * 1000))
                throw new TimeoutException("Operation timed out, indexing is paused.");
        }

        [Obsolete("Use Start(System.IO.TextWriter) instead.")]
        public static void Start()
        {
            Start(null);
        }
        public static void Start(System.IO.TextWriter consoleOut)
        {
            if (!_running)
            {
                lock (_startSync)
                {
                    if (!_running)
                    {
                        Startup(consoleOut);
                        _running = true;
                    }
                }
            }
        }
        private static void Startup(System.IO.TextWriter consoleOut)
        {
            try
            {
                //We can't handle cache invalidation as it would call into LuceneManager
                //we only process lucene messages
                var safeMessageTypes = new List<Type>();
                safeMessageTypes.Add(typeof(DistributedLuceneActivity.LuceneActivityDistributor));
                ClusterChannel.ProcessedMessageTypes = safeMessageTypes;

                //we positively start the message cluster
                int dummy = ContentRepository.DistributedApplication.Cache.Count;
                var dummy2 = ContentRepository.DistributedApplication.ClusterChannel;

                if (ContentRepository.RepositoryInstance.RestoreIndexOnStartup())
                    BackupTools.RestoreIndex(false, consoleOut);

                CreateWriterAndReader();

                using (new SystemAccount())
                    ExecuteUnprocessedIndexingActivities(consoleOut, true);

                Warmup();

                var commitStart = new ThreadStart(CommitWorker);
                var t = new Thread(commitStart);
                t.Start();
#if INDEX
                DetailedLogger.Log("LM: 'CommitWorker' thread started. ManagedThreadId: {0}", t.ManagedThreadId); // category: INDEX
#endif
            }
            finally
            {
                ClusterChannel.ProcessedMessageTypes = null;
            }
        }

        private static void CreateWriterAndReader()
        {
            var directory = FSDirectory.Open(new System.IO.DirectoryInfo(IndexDirectory.CurrentDirectory));

            _writer = new IndexWriter(directory, IndexManager.GetAnalyzer(), false);//TODO: ez obsolete, a következő sor kellene, de akkor elpattan ez a teszt: Msmq_SendLargeIndexDocument: "Assert.IsTrue failed. Lucene activity was not processed within 3 seconds"
            //_writer = new IndexWriter(directory, IndexManager.GetAnalyzer(), false, IndexWriter.MaxFieldLength.UNLIMITED);

            _writer.SetMaxMergeDocs(RepositoryConfiguration.LuceneMaxMergeDocs);
            _writer.SetMergeFactor(RepositoryConfiguration.LuceneMergeFactor);
            _writer.SetRAMBufferSizeMB(RepositoryConfiguration.LuceneRAMBufferSizeMB);
            _reader = _writer.GetReader();
        }

        internal static string[] PauseIndexingAndGetIndexFilePaths()
        {
            PauseIndexing();

            return GetIndexFilePathsInternal();
        }

        private static string[] GetIndexFilePathsInternal()
        {
            var filePaths = new List<string>();

            //in case of the index does not exist
            if (!IndexDirectory.Exists)
                return filePaths.ToArray();

            try
            {
                var di = new DirectoryInfo(IndexDirectory.CurrentDirectory);
                var files = di.GetFiles();

                filePaths.AddRange(files.Where(f => f.Name != IndexWriter.WRITE_LOCK_NAME).Select(f => f.FullName));
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }

            return filePaths.ToArray();
        }

        //========================================================================================== Start, Restart, Shutdown, Warmup

        internal static void Restart()
        {
            if (Paused)
            {
                Debug.WriteLine("##> LUCENEMANAGER RESTART called but it is not executed because indexing is paused.");
                return;
            }
            Debug.WriteLine("##> LUCENEMANAGER RESTART");
#if INDEX
            DetailedLogger.Log("LM: LUCENEMANAGER RESTART"); // category: INDEX
#endif
            using (var wrFrame = IndexWriterFrame.Get(true)) // // Restart
            {
                //_writer.Close();
                wrFrame.IndexWriter.Close();
                CreateWriterAndReader();
            }
        }
        public static void ShutDown()
        {
            ShutDown(true);
        }
        private static void ShutDown(bool log)
        {
            if (!_running)
                return;
            if (Paused)
                throw GetPausedException();

            Debug.WriteLine("##> LUCENEMANAGER SHUTDOWN");

            if (_writer != null)
            {
                _stopCommitWorker = true;
#if INDEX
                var op = DetailedLogger.CreateOperation(); // category: INDEX
                DetailedLogger.Log(op, "LM.Commit"); // category: INDEX
#endif
                lock (_commitLock)
                {
                    Commit(false);
                }
#if INDEX
                op.Finish(); // category: INDEX
#endif

#if INDEX
                op = DetailedLogger.CreateOperation(); // category: INDEX
                DetailedLogger.Log(op, "LM.CloseReaders"); // category: INDEX
#endif
                using (var wrFrame = IndexWriterFrame.Get(true)) // // ShutDown
                {
                    if (_reader != null)
                        _reader.Close();
                    if (_writer != null)
                        _writer.Close();
                    _running = false;
                }
#if INDEX
                op.Finish(); // category: INDEX
#endif
            }

            if (log)
                Logger.WriteInformation(Logger.EventId.NotDefined, "LuceneManager has stopped. Max task id: " + MissingActivityHandler.MaxActivityId);
        }
        public static void Backup()
        {
            BackupTools.SynchronousBackupIndex();
        }
        public static void BackupAndShutDown()
        {
            ShutDown();
            BackupTools.BackupIndexImmediatelly();
        }

        private static readonly int ACTIVITIESFRAGMENTSIZE = 100;
        internal static object _executingUnprocessedIndexingActivitiesLock = new object();
        private static bool _executingUnprocessedIndexingActivities;
        //---- Caller: Startup, ContinueIndexing
        internal static void ExecuteUnprocessedIndexingActivities(System.IO.TextWriter consoleOut, bool initializeGapFromIndex)
        {
            lock (_executingUnprocessedIndexingActivitiesLock)
            {
                try
                {
                    _executingUnprocessedIndexingActivities = true;

                    if (initializeGapFromIndex)
                    {
                        CommitUserData cud;
                        using (var readerFrame = LuceneManager.GetIndexReaderFrame())
                        {
                            cud = IndexManager.ReadCommitUserData(readerFrame.IndexReader);
                        }
                        MissingActivityHandler.MaxActivityId = cud.LastActivityId;
                        MissingActivityHandler.SetGap(cud.Gap);
                    }

                    var gap = MissingActivityHandler.GetGap();
                    var logProps = new Dictionary<string, object> { { "LastActivityID", MissingActivityHandler.MaxActivityId }, { "Size of gap", gap.Count } };
                    if (initializeGapFromIndex)
                    {
                        Logger.WriteInformation(EventId.Indexing.ExecuteUnprocessedActivitiesFromCommitPoint,
                            "Executing unprocessed indexing activities from the stored commit point.",
                            properties: logProps);
                    }
                    else
                    {
                        Logger.WriteInformation(EventId.Indexing.ExecuteUnprocessedActivitiesAfterPause,
                            "Executing unprocessed indexing activities after paused indexing.",
                            properties: logProps);
                    }

                    var i = 0;
                    var sumCount = 0;
                    var ghostIdRemoved = false;

                    // This loop was created to avoid loading too many activities at once that are present in the gap.
                    while (i * ACTIVITIESFRAGMENTSIZE <= gap.Count)
                    {
                        // get activities from the DB that are in the current gap fragment
                        var gapSegment = gap.Skip(i * ACTIVITIESFRAGMENTSIZE).Take(ACTIVITIESFRAGMENTSIZE).ToArray();
                        var activities = IndexingActivityManager.GetUnprocessedActivities(gapSegment);

                        ProcessTasks(activities, consoleOut);

                        //Remove ghost ids (that do not exist in the database) from the gap. This prevents ids staying in the gap forever.
                        var nonexistingIds = gapSegment.Except(activities.Select(a => a.IndexingActivityId)).ToArray();
                        foreach (var nonexistingId in nonexistingIds)
                        {
                            ghostIdRemoved = ghostIdRemoved || MissingActivityHandler.RemoveActivityFromGap(nonexistingId);
                        }

                        sumCount += Math.Max(activities.Length - nonexistingIds.Length, 0);
                        i++;
                    }

                    //if a ghost id was removed, ensure index writer is changed and changes are committed
                    if (ghostIdRemoved)
                    {
                        EnsureWriterChanged();
                        Commit();
                    }

                    // Execute activities where activity id is bigger than than our last (activity) task id
                    var fromId = MissingActivityHandler.MaxActivityId;

                    // resume indexing if it was paused. This must not preceede the above setting of fromId, since MaxActivityId would be changed due to incoming MSMQ activities
                    // we set maxidindb after (and not before) this to ensure that no gap is created if an activity arrives after maxidindb setting but before resuming indexing
                    // this may cause duplicate indexing of some activities, but that is protected with indexinghistory
                    _paused = false;

                    var maxIdInDb = 0;
                    var newtasks = IndexingActivityManager.GetUnprocessedActivities(fromId, out maxIdInDb, ACTIVITIESFRAGMENTSIZE);

                    Logger.WriteInformation(Logger.EventId.NotDefined, String.Concat("Max activity id (db:", maxIdInDb.ToString(), ", local:", fromId.ToString(), ")"));


                    while (true)
                    {
                        var lastTask = newtasks.LastOrDefault();
                        if (lastTask == null)
                            break;
                        fromId = lastTask.IndexingActivityId;
                        ProcessTasks(newtasks, consoleOut);
                        sumCount += newtasks.Length;
                        if (fromId >= maxIdInDb)
                            break;
                        int outTemp;
                        newtasks = IndexingActivityManager.GetUnprocessedActivities(fromId, out outTemp, ACTIVITIESFRAGMENTSIZE);
                    }
                    //------------------------------------

                    if (consoleOut != null)
                        consoleOut.WriteLine("ok.");

                    logProps.Add("Processed tasks", sumCount);

                    //write the latest max activity id and gap size to log
                    logProps["LastActivityID"] = MissingActivityHandler.MaxActivityId;
                    logProps["Size of gap"] = MissingActivityHandler.GetGap().Count;

                    Logger.WriteInformation(EventId.Indexing.ExecutingUnprocessedActivitiesFinished, "Executing unprocessed tasks is finished.", properties: logProps);
                }
                finally
                {
                    _executingUnprocessedIndexingActivities = false;
                }
            }
        }
        private static void ProcessTasks(IndexingActivity[] activities, System.IO.TextWriter consoleOut)
        {
            if (consoleOut != null && activities.Length != 0)
                consoleOut.Write("    Executing {0} unprocessed tasks ...", activities.Length);

            foreach (var activity in activities)
            {
                activity.FromExecutingUnprocessedActivities = true;
                IndexingActivityManager.ExecuteActivity(activity, false, false);
            }
        }
        internal static void ExecuteLostIndexingActivities()
        {
            lock (_executingUnprocessedIndexingActivitiesLock)
            {
                var gap = MissingActivityHandler.GetOldestGapAndMoveToNext();
                var ghostIdRemoved = false;

                var i = 0;
                while (i * ACTIVITIESFRAGMENTSIZE < gap.Length)
                {
                    var gapSegment = gap.Skip(i * ACTIVITIESFRAGMENTSIZE).Take(ACTIVITIESFRAGMENTSIZE).ToArray();

                    var activities = IndexingActivityManager.GetUnprocessedActivities(gapSegment);
                    if (activities.Length > 0)
                    {
                        foreach (var act in activities)
                        {
                            IndexingActivityManager.ExecuteActivity(act, false, false);
                        }
                    }

                    //Remove ghost ids (that do not exist in the database) from the gap. This prevents ids staying in the gap forever.
                    var nonexistingIds = gapSegment.Except(activities.Select(a => a.IndexingActivityId)).ToArray();
                    foreach (var nonexistingId in nonexistingIds)
                    {
                        ghostIdRemoved = ghostIdRemoved || MissingActivityHandler.RemoveActivityFromGap(nonexistingId);
                    }

                    i++;
                }

                //if a ghost id was removed, ensure index writer is changed and changes are committed
                if (ghostIdRemoved)
                {
                    EnsureWriterChanged();
                    Commit();
                }
            }
        }

        private const string COMMITFIELDNAME = "$#COMMIT";
        private const string COMMITDATEFIELDNAME = "$#DATE";
        private static void EnsureWriterChanged()
        {
            var value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff");
            var doc = new Document();
            doc.Add(new Field(COMMITFIELDNAME, COMMITFIELDNAME, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
            doc.Add(new Field(COMMITDATEFIELDNAME, value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));

#if INDEX
            DetailedLogger.Log("LM: EnsureWriterChanged."); // category: INDEX
#endif
            using (var wrFrame = IndexWriterFrame.Get(false)) // // UpdateDocument
            {
                wrFrame.IndexWriter.UpdateDocument(new Term(COMMITFIELDNAME, COMMITFIELDNAME), doc);
            }

            //UpdateDocument(new Term(COMMITFIELDNAME, COMMITFIELDNAME), doc, 0);
        }

        internal static void Warmup()
        {
            var idList = ((LuceneSearchEngine)StorageContext.Search.SearchEngine).Execute("+Id:1");
        }

        /*========================================================================================== Commit */

        internal static void UnregisterActivity(int activityId, bool fromExecutingUnprocessedActivities)
        {
            if (!IsActivityExecutable(fromExecutingUnprocessedActivities))
                return;

            if (activityId > 0)
                MissingActivityHandler.RemoveActivityAndAddGap(activityId);
            //compiler warning here is not a problem, Interlocked 
            //class can work with a volatile variable
            Interlocked.Increment(ref _activities);
        }

        internal static void CommitOrDelay()
        {
            if (Paused)
                return;

            // set gapsize perfcounters
            MissingActivityHandler.SetGapSizeCounter();

            var act = _activities;
            if (act == 0 && _delayCycle == 0)
                return;

            if (act < 2)
            {
                WriteCommitLog("Start");
                Commit();
                WriteCommitLog("Stop");
            }
            else
            {
                _delayCycle++;
                if (_delayCycle > RepositoryConfiguration.DelayedCommitCycleMaxCount)
                {
                    WriteCommitLog("Start");
                    Commit();
                    WriteCommitLog("Stop");
                }
            }

            Interlocked.Exchange(ref _activities, 0);
        }

        internal static void Commit(bool reopenReader = true)
        {
#if INDEX
            DetailedLogger.Log("LM.Commit1_getting_gap"); // category: INDEX
#endif
            var gapData = MissingActivityHandler.GetGapString();
            var gapString = gapData.Item1;
            var maxActivityId = gapData.Item2;

#if INDEX
            DetailedLogger.Log("LM.Commit2_committing_writer"); // category: INDEX
#endif
            using (var wrFrame = IndexWriterFrame.Get(false)) // // Commit
            {
                //_writer.Commit(IndexManager.CreateCommitUserData(maxActivityId, gapString));
                wrFrame.IndexWriter.Commit(IndexManager.CreateCommitUserData(maxActivityId, gapString));
                if (reopenReader)
                {
#if INDEX
                    DetailedLogger.Log("LM.Commit3_reopen_reader"); // category: INDEX
#endif
                    ReopenReader();
                }
            }

            //in case of shutdown, reopen is not needed
            WriteCommitLog("Committed", null, null, null, null, gapString, maxActivityId);

            Interlocked.Exchange(ref _activities, 0);
            _delayCycle = 0;
        }
        internal static void ReopenReader()
        {
            var retry = 0;
            Exception e = null;
            while (retry++ < REOPENRETRYMAX)
            {
                try
                {
                    Debug.WriteLine("##> REOPEN " + (retry > 1 ? retry.ToString() : ""));

                    _reader = _writer.GetReader();
                    return;
                }
                catch (AlreadyClosedException ace)
                {
                    e = ace;
                    Thread.Sleep(100);
                }
            }
            if (e != null)
                throw new ApplicationException(String.Concat("Indexwriter is closed after ", retry, " attempt."), e);
        }

        private static volatile int _activities;          //committer thread sets 0 other threads increment
        private static volatile int _delayCycle;          //committer thread uses

        private static bool _stopCommitWorker;
        private static object _commitLock = new object();
        private static void CommitWorker()
        {
            int wait = (int)(RepositoryConfiguration.CommitDelayInSeconds * 1000.0);
            for (; ; )
            {
                // check if commit worker instructed to stop
                if (_stopCommitWorker)
                {
                    _stopCommitWorker = false;
                    return;
                }

                try
                {
                    lock (_commitLock)
                    {
                        CommitOrDelay();
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteError(Logger.EventId.NotDefined, ex);
                }
                Thread.Sleep(wait);
            }
        }

        /*==================================================================== Document operations */

        internal static void RefreshDocument(int versionId, bool fromExecutingUnprocessedActivities)
        {
            // the optimistic overlapping detection algorithm here is tested in IndexingHistory_Fix**Overlap tests. change tests if this algorithm is changed.

            var node = Node.LoadNodeByVersionId(versionId);

            // if node exists, refresh index from db
            // if node does not exist, remove from index
            if (node != null)
            {
                // delete from indexhistory first, because we are trying to refresh to the last timestamp
                // also check if there is a more fresh indexing pending which would surely correct the index, and we don't need this here
                if (!_history.RemoveIfLast(versionId, new Timestamps(node.NodeTimestamp, node.VersionTimestamp)))
                    return;

#if INDEX
                DetailedLogger.Log("LM.RefreshDocument: RefreshIndex. V: {0}, VId: {1}", node.Version, node.VersionId); // category: INDEX
#endif
                StorageContext.Search.SearchEngine.GetPopulator().RefreshIndex(node, false);
            }
            else
            {
#if INDEX
                DetailedLogger.Log("LM.RefreshDocument: Delete. Node version not found. VId: {0}", versionId); // category: INDEX
#endif
                var delTerm = new Term(LuceneManager.KeyFieldName, NumericUtils.IntToPrefixCoded(versionId));
                LuceneManager.DeleteDocuments(new[] { delTerm }, false, 0, fromExecutingUnprocessedActivities);
            }
        }

        /*-----------------------------------------------------------------------------------------*/

        internal static void AddCompleteDocument(Document document, int activityId, bool fromExecutingUnprocessedActivities)
        {
            if (!IsActivityExecutable(fromExecutingUnprocessedActivities))
            {
#if INDEX
                DetailedLogger.Log("LM: AddCompleteDocument skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}", activityId, fromExecutingUnprocessedActivities); // category: INDEX
#endif
                return;
            }

            // the optimistic overlapping detection algorithm here is tested in IndexingHistory_Fix**Overlap tests. change tests if this algorithm is changed.

            var versionId = _history.GetVersionId(document);
            var timestamp = _history.GetTimestamp(document);

            if (!_history.IsVersionCanBeAdded(versionId, timestamp))
            {
#if INDEX
                DetailedLogger.Log("LM: AddCompleteDocument skipped #2. ActivityId:{0}, VersionId:{1}, ExecutingUnprocessedActivities:{2}", activityId, versionId, fromExecutingUnprocessedActivities); // category: INDEX
#endif
                return;
            }

#if INDEX
            DetailedLogger.Log("LM: AddCompleteDocument. ActivityId:{0}, {1}, ExecutingUnprocessedActivities:{2}", activityId, GetDocumentLog(document), fromExecutingUnprocessedActivities); // category: INDEX
#endif
            using (var wrFrame = IndexWriterFrame.Get(false)) // // AddCompleteDocument
            {
                if (ProtectFromDuplication())
                    wrFrame.IndexWriter.DeleteDocuments(GetVersionIdTerm(versionId));

                // make sure that we do not create duplicate documents
                if (_history.IsVersionChanged(versionId, timestamp, true))
                {
#if INDEX
                    DetailedLogger.Log("LM: AddCompleteDocument skipped #3 (version already exists with a newer timestamp). ActivityId:{0} {1}, ExecutingUnprocessedActivities:{2}", activityId, GetDocumentLog(document), fromExecutingUnprocessedActivities); // category: INDEX
#endif
                    return;
                }
#if INDEX
                DetailedLogger.Log(LOGPREFIX_FLAGS + " * AddCompleteDocument BEFORE ADD DOCUMENT * ActivityId:{0} {1}", activityId, GetDocumentLog(document));
#endif
                wrFrame.IndexWriter.AddDocument(document);
            }

#if INDEX
            LogVersionFlagConsistency(document, "AddCompleteDocument AFTER ADD DOCUMENT", activityId: activityId);
#endif

            // check if indexing has interfered with other indexing activity for the same versionid
            if (_history.IsVersionChanged(versionId, timestamp))
            {
                RefreshDocument(versionId, fromExecutingUnprocessedActivities);

#if INDEX
                LogVersionFlagConsistency(document, "AddCompleteDocument AFTER REFRESH", activityId: activityId);
#endif
            }
        }
        internal static void AddDocument(Document document, int activityId, bool fromExecutingUnprocessedActivities)
        {
            if (!IsActivityExecutable(fromExecutingUnprocessedActivities))
            {
#if INDEX
                DetailedLogger.Log("LM: AddDocument skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}", activityId, fromExecutingUnprocessedActivities); // category: INDEX
#endif
                return;
            }

            // the optimistic overlapping detection algorithm here is tested in IndexingHistory_Fix**Overlap tests. change tests if this algorithm is changed.

            var versionId = _history.GetVersionId(document);
            var timestamp = _history.GetTimestamp(document);

            if (!_history.IsVersionCanBeAdded(versionId, timestamp))
            {
#if INDEX
                DetailedLogger.Log("LM: AddDocument skipped #2. ActivityId:{0} {1}, ExecutingUnprocessedActivities:{2}", activityId, GetDocumentLog(document), fromExecutingUnprocessedActivities); // category: INDEX
#endif
                return;
            }
            
            using (var wrFrame = IndexWriterFrame.Get(false)) // // AddDocument
            {
                SetFlagsForAdd(document, activityId);
#if INDEX
                DetailedLogger.Log("LM: AddDocument. ActivityId:{0}, VersionId:{1}, ExecutingUnprocessedActivities:{2}", activityId, versionId, fromExecutingUnprocessedActivities); // category: INDEX
#endif

                if (ProtectFromDuplication())
                    wrFrame.IndexWriter.DeleteDocuments(GetVersionIdTerm(versionId));

                // make sure that we do not create duplicate documents
                if (_history.IsVersionChanged(versionId, timestamp, true))
                {
#if INDEX
                    DetailedLogger.Log("LM: AddDocument skipped #3 (version already exists with a newer timestamp). ActivityId:{0} {1}, ExecutingUnprocessedActivities:{2}", activityId, GetDocumentLog(document), fromExecutingUnprocessedActivities); // category: INDEX
#endif
                    return;
                }
#if INDEX
                DetailedLogger.Log(LOGPREFIX_FLAGS + " * AddDocument BEFORE ADD DOCUMENT * ActivityId:{0} {1}", activityId, GetDocumentLog(document));
#endif
                wrFrame.IndexWriter.AddDocument(document);
            }

#if INDEX
            LogVersionFlagConsistency(document, "AddDocument AFTER WRITE", activityId: activityId);
#endif

            // check if indexing has interfered with other indexing activity for the same versionid
            if (_history.IsVersionChanged(versionId, timestamp))
            {
                RefreshDocument(versionId, fromExecutingUnprocessedActivities);

#if INDEX
                LogVersionFlagConsistency(document, "AddDocument AFTER REFRESH", activityId: activityId);
#endif
            }
        }
        internal static void UpdateDocument(Term updateTerm, Document document, int activityId, bool fromExecutingUnprocessedActivities)
        {
            if (!IsActivityExecutable(fromExecutingUnprocessedActivities))
            {
#if INDEX
                DetailedLogger.Log("LM: UpdateDocument skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}", activityId, fromExecutingUnprocessedActivities); // category: INDEX
#endif
                return;
            }

            // the optimistic overlapping detection algorithm here is tested in IndexingHistory_Fix**Overlap tests. change tests if this algorithm is changed.

            var versionId = _history.GetVersionId(document);
            var timestamp = _history.GetTimestamp(document);

            if (!_history.IsVersionCanBeUpdated(versionId, timestamp))
            {
#if INDEX
                DetailedLogger.Log("LM: UpdateDocument skipped #2. ActivityId:{0} {1}, ExecutingUnprocessedActivities:{2}", activityId, GetDocumentLog(document), fromExecutingUnprocessedActivities); // category: INDEX
#endif
                return;
            }
            
            using (var wrFrame = IndexWriterFrame.Get(false)) // // UpdateDocument
            {
                SetFlagsForUpdate(document, timestamp, activityId);

                // make sure that we do not modify documents that were modified by another thread
                if (_history.IsVersionChanged(versionId, timestamp, true))
                {
#if INDEX
                    DetailedLogger.Log("LM: UpdateDocument skipped #3 (version already updated by somebody else). ActivityId:{0} {1}, ExecutingUnprocessedActivities:{2}", activityId, GetDocumentLog(document), fromExecutingUnprocessedActivities); // category: INDEX
#endif
                    return;
                }

#if INDEX
                DetailedLogger.Log("LM: UpdateDocument. ActivityId:{0}, VersionId:{1}, ExecutingUnprocessedActivities:{2}", activityId, versionId, fromExecutingUnprocessedActivities); // category: INDEX
                DetailedLogger.Log(LOGPREFIX_FLAGS + " * UpdateDocument BEFORE UPDATE DOCUMENT * ActivityId:{0} {1}", activityId, GetDocumentLog(document));
#endif

                wrFrame.IndexWriter.UpdateDocument(updateTerm, document);
            }

#if INDEX
            LogVersionFlagConsistency(document, "UpdateDocument AFTER UPDATE DOCUMENT", activityId: activityId);
#endif

            // check if indexing has interfered with other indexing activity for the same versionid
            if (_history.IsVersionChanged(versionId, timestamp))
            {
                RefreshDocument(versionId, fromExecutingUnprocessedActivities);

#if INDEX
                LogVersionFlagConsistency(document, "UpdateDocument AFTER REFRESH DOCUMENT", activityId: activityId);
#endif
            }
        }
        internal static void DeleteDocuments(Term[] deleteTerms, bool moveOrRename, int activityId, bool fromExecutingUnprocessedActivities)
        {
            if (!IsActivityExecutable(fromExecutingUnprocessedActivities))
            {
#if INDEX
                DetailedLogger.Log("LM: DeleteDocuments skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, MoveOrRename:{2}", activityId, fromExecutingUnprocessedActivities, moveOrRename); // category: INDEX
#endif
                return;
            }

            // the optimistic overlapping detection algorithm here is tested in IndexingHistory_Fix**Overlap tests. change tests if this algorithm is changed.

            if (moveOrRename)
                _history.Remove(deleteTerms);
            else
                _history.ProcessDelete(deleteTerms);

            SetFlagsForDelete(deleteTerms, activityId);
#if INDEX
            DetailedLogger.Log("LM: DeleteDocuments BEFORE delete. ActivityId:{0}, VersionIds:{1}; ExecutingUnprocessedActivities:{2}", activityId,
                string.Join(", ", deleteTerms.Select(t => GetIntFromPrefixCode(t.Text()))),
                fromExecutingUnprocessedActivities); // category: INDEX
#endif
            using (var wrFrame = IndexWriterFrame.Get(false)) // // DeleteDocuments
            {
                wrFrame.IndexWriter.DeleteDocuments(deleteTerms);
            }

            // don't need to check if indexing interfered here. If it did, change is detected in overlapped adddocument/updatedocument, and refresh (re-delete) is called there.
            // deletedocuments will never detect change in index, since it sets timestamp in indexhistory to maxvalue.
        }
        
        internal static void AddTree(string treeRoot, int activityId, bool fromExecutingUnprocessedActivities)
        {
            if (!IsActivityExecutable(fromExecutingUnprocessedActivities))
            {
#if INDEX
                DetailedLogger.Log("LM: AddTree skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, TreeRoot:{2}", activityId, fromExecutingUnprocessedActivities, treeRoot); // category: INDEX
#endif
                return;
            }

#if INDEX
            DetailedLogger.Log("LM: AddTree. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, TreeRoot:{2}", activityId, fromExecutingUnprocessedActivities, treeRoot); // category: INDEX
#endif
            using (var wrFrame = IndexWriterFrame.Get(false))
            {
                foreach (var docData in StorageContext.Search.LoadIndexDocumentsByPath(treeRoot))
                {
                    Document document;
                    int versionId;
                    Timestamps timestamps;

                    try
                    {
                        document = IndexDocumentInfo.GetDocument(docData);
                        if (document == null) // indexing disabled
                            continue;
                        versionId = _history.GetVersionId(document);
                        timestamps = _history.GetTimestamp(document);
                    }
                    catch (Exception e)
                    {
                        var path = docData == null ? string.Empty : docData.Path ?? string.Empty;
                        Logger.WriteException(new Exception("Error during indexing: the document data loaded from the database or the generated Lucene Document is invalid. Please save the content to regenerate the index for it. Path: " + path, e));

                        throw;
                    }

                    if (!_history.IsVersionCanBeAdded(versionId, timestamps))
                        continue;

                    if (ProtectFromDuplication())
                        wrFrame.IndexWriter.DeleteDocuments(GetVersionIdTerm(versionId));

#if INDEX
                    DetailedLogger.Log(LOGPREFIX_FLAGS + " * AddTree BEFORE ADD DOCUMENT * ActivityId:{0} {1}", activityId, GetDocumentLog(document)); // category: INDEX
#endif

                    wrFrame.IndexWriter.AddDocument(document);

                    if (_history.IsVersionChanged(versionId, timestamps))
                        RefreshDocument(versionId, fromExecutingUnprocessedActivities);
                }
            }
        }

        private static Term GetVersionIdTerm(Document doc)
        {
            return GetVersionIdTerm(Int32.Parse(doc.Get(LucObject.FieldName.VersionId)));
        }
        private static Term GetVersionIdTerm(int versionId)
        {
            return new Term(LucObject.FieldName.VersionId, Lucene.Net.Util.NumericUtils.IntToPrefixCoded(versionId));
        }

        private static bool IsActivityExecutable(bool fromExecutingUnprocessedActivities)
        {
            // if not running do nothing - except when during executing unprocessed activities
            if (!Running && !fromExecutingUnprocessedActivities)
                return false;
            // if paused do nothing - except when during executing unprocessed activities
            if (Paused && !fromExecutingUnprocessedActivities)
                return false;
            return true;
        }
        private static bool ProtectFromDuplication()
        {
            // if EUA is in progress, we protect every activity from duplication
            // (index can contain old activities but without committed maxactivityid in index - due to emergency powerdown - these activities could be duplicated on startup,
            // because there is no index history there yet
            if (_executingUnprocessedIndexingActivities)
                return true;

            return false;
        }

        //-------------------------------------------------------------------- flag setting

        private class DocumentVersionComparer : IComparer<Document>
        {
            public int Compare(Document x, Document y)
            {
                var vx = x.Get("Version").Substring(1);
                var vxa = vx.Split('.');
                var vy = y.Get("Version").Substring(1);
                var vya = vy.Split('.');

                var vxma = Int32.Parse(vxa[0]);
                var vyma = Int32.Parse(vya[0]);
                var dxma = vxma.CompareTo(vyma);
                if (dxma != 0)
                    return dxma;

                var vxmi = Int32.Parse(vxa[1]);
                var vymi = Int32.Parse(vya[1]);
                var dxmi = vxmi.CompareTo(vymi);
                if (dxmi != 0)
                    return dxmi;
                return vxa[2].CompareTo(vya[2]);
            }
        }
        private class VersionInfo
        {
            public Document Document;
            public bool IsActualDocument;
            public string Version;
            public int VersionId;
            public bool OriginalIsMajor;
            public bool OriginalIsPublic;
            public bool OriginalIsLastDraft;
            public bool OriginalIsLastPublic;
            public bool ExpectedIsMajor;
            public bool ExpectedIsPublic;
            public bool ExpectedIsLastDraft;
            public bool ExpectedIsLastPublic;
        }

        private static void SetFlagsForAdd(Document document, int activityId = 0)
        {
            VersionInfo currentInfo;
            var infoList = GetAllVersionInfo(document, out currentInfo);
            UpdateDirtyDocuments(infoList, activityId);

#if INDEX
            LogVersionFlagConsistency(document, "SetFlagsForAdd", false, activityId);
#endif

            SetDocumentFlags(currentInfo);

#if INDEX
            LogVersionInfo(currentInfo, "SetFlagsForAdd AFTER SETFLAGS", activityId);
#endif
        }
        internal static void SetFlagsForUpdate(Document document, Timestamps timestamp = null, int activityId = 0)
        {
            VersionInfo currentInfo;
            var infoList = GetAllVersionInfo(document, out currentInfo);

            UpdateDirtyDocuments(infoList,activityId);

#if INDEX
            LogVersionFlagConsistency(document, "SetFlagsForUpdate", false, activityId);
#endif

            // make sure that nobody else modified the document in the meantime
            if (timestamp != null && _history.IsVersionChanged(currentInfo.VersionId, timestamp, true))
            {
#if INDEX
                DetailedLogger.Log(LOGPREFIX_FLAGS + " SetFlagsForUpdate skipped (version was updated by another thread). ActivityId:{0} {1}", activityId, GetDocumentLog(document)); // category: INDEX
#endif
                return;
            }

            SetDocumentFlags(currentInfo);

#if INDEX
            LogVersionInfo(currentInfo, "SetFlagsForUpdate AFTER SETFLAGS", activityId);
#endif
        }
        private static void SetFlagsForDelete(Term[] deleteTerms, int activityId = 0)
        {
            foreach (var deleteTerm in deleteTerms)
                SetFlagsForDelete(deleteTerm, activityId);
        }
        internal static void SetFlagsForDelete(Term deleteTerm, int activityId = 0)
        {
            if (deleteTerm.Field() != LucObject.FieldName.VersionId)
                return;

            var versionId = NumericUtils.PrefixCodedToInt(deleteTerm.Text());
            var infoList = GetAllVersionInfoAfterDeleteVersion(versionId);
            UpdateDirtyDocuments(infoList, activityId);
        }

        private static List<VersionInfo> GetAllVersionInfo(Document document, out VersionInfo currentInfo)
        {
            //-- create current VersionInfo
            var versionstring = document.Get(LucObject.FieldName.Version);
            var version = VersionNumber.Parse(versionstring);
            var isPublic = version.Status == VersionStatus.Approved;
            currentInfo = new VersionInfo
            {
                Document = document,
                IsActualDocument = true,
                Version = versionstring,
                VersionId = Int32.Parse(document.Get(LucObject.FieldName.VersionId)),
                ExpectedIsMajor = version.IsMajor,
                ExpectedIsPublic = isPublic,
            };

            //-- create original list (include only documents that were not marked as deleted)
            var infoList = GetOriginalVersionInfoList(document).Where(vi => !_history.IsVersionDeleted(vi.VersionId)).ToList();

            //-- search existing VersionInfo
            var existingIndex = -1;
            VersionInfo existingInfo = null;
            for (int i = 0; i < infoList.Count; i++)
            {
                if (infoList[i].VersionId == currentInfo.VersionId)
                {
                    existingIndex = i;
                    existingInfo = infoList[i];
                    break;
                }
            }

            //-- positioning the current info
            if (existingInfo == null)
            {
                infoList.Add(currentInfo);
            }
            else
            {
                infoList[existingIndex] = currentInfo;
                currentInfo.OriginalIsMajor = existingInfo.OriginalIsMajor;
                currentInfo.OriginalIsPublic = existingInfo.OriginalIsPublic;
                currentInfo.OriginalIsLastPublic = existingInfo.OriginalIsLastPublic;
                currentInfo.OriginalIsLastDraft = existingInfo.OriginalIsLastDraft;
            }

            //-- set expected flags
            SetExpectedFlags(infoList);

            return infoList;
        }
        private static List<VersionInfo> GetAllVersionInfoAfterDeleteVersion(int versionId)
        {
            var d = GetDocumentByVersionId(versionId);
            if (d.Count == 0)
                //throw new ArgumentException("Lucene Index does not contain any documents by versionId " + versionId);
                return new List<VersionInfo>(0);

            var document = d[0];

            //-- create original list
            var infoList = GetOriginalVersionInfoList(document);

            //-- remove the existing VersionInfo
            var existingIndex = -1;
            for (int i = 0; i < infoList.Count; i++)
            {
                if (infoList[i].VersionId == versionId)
                {
                    existingIndex = i;
                    break;
                }
            }
            infoList.RemoveAt(existingIndex);

            //-- set expected flags
            SetExpectedFlags(infoList);

            return infoList;
        }
        private static List<VersionInfo> GetOriginalVersionInfoList(Document document)
        {
            var nodeId = document.Get(LucObject.FieldName.NodeId);
            var docs = GetDocumentsByNodeId(nodeId);
            var infoArray = new VersionInfo[docs.Count];
            for (int i = 0; i < docs.Count; i++)
            {
                var doc = docs[i];
                var versionstring = doc.Get(LucObject.FieldName.Version);
                var version = VersionNumber.Parse(versionstring);
                var isPublic = version.Status == VersionStatus.Approved;
                var info = new VersionInfo
                {
                    Document = doc,
                    IsActualDocument = false,
                    Version = versionstring,
                    VersionId = Int32.Parse(doc.Get(LucObject.FieldName.VersionId)),
                    OriginalIsMajor = doc.Get(LucObject.FieldName.IsMajor) == BooleanIndexHandler.YES,
                    OriginalIsPublic = doc.Get(LucObject.FieldName.IsPublic) == BooleanIndexHandler.YES,
                    OriginalIsLastPublic = doc.Get(LucObject.FieldName.IsLastPublic) == BooleanIndexHandler.YES,
                    OriginalIsLastDraft = doc.Get(LucObject.FieldName.IsLastDraft) == BooleanIndexHandler.YES,
                    ExpectedIsMajor = version.IsMajor,
                    ExpectedIsPublic = isPublic,
                };
                infoArray[i] = info;
            }
            return infoArray.ToList();
        }
        private static void SetExpectedFlags(List<VersionInfo> infoList)
        {
            if (infoList.Count == 0)
                return;

            //-- reset ExpectedIsLastDraft, ExpectedIsLastPublic flags
            foreach (var info in infoList)
            {
                info.ExpectedIsLastDraft = false;
                info.ExpectedIsLastPublic = false;
            }

            //-- set ExpectedIsLastDraft flag
            infoList.Last().ExpectedIsLastDraft = true;

            //-- set ExpectedIsLastPublic flag
            for (int i = infoList.Count - 1; i >= 0; i--)
            {
                var info = infoList[i];
                if (info.ExpectedIsPublic)
                {
                    info.ExpectedIsLastPublic = true;
                    break;
                }
            }
        }

        private static void SetDocumentFlags(VersionInfo info)
        {
            var doc = info.Document;
            doc.RemoveField(LucObject.FieldName.IsMajor);
            doc.RemoveField(LucObject.FieldName.IsPublic);
            doc.RemoveField(LucObject.FieldName.IsLastPublic);
            doc.RemoveField(LucObject.FieldName.IsLastDraft);
            SetDocumentFlag(doc, LucObject.FieldName.IsMajor, info.ExpectedIsMajor);
            SetDocumentFlag(doc, LucObject.FieldName.IsPublic, info.ExpectedIsPublic);
            SetDocumentFlag(doc, LucObject.FieldName.IsLastPublic, info.ExpectedIsLastPublic);
            SetDocumentFlag(doc, LucObject.FieldName.IsLastDraft, info.ExpectedIsLastDraft);
        }
        internal static void SetDocumentFlag(Document doc, string fieldName, bool value)
        {
            doc.Add(new Field(fieldName, value ? BooleanIndexHandler.YES : BooleanIndexHandler.NO, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
        }
        private static void UpdateDirtyDocuments(List<VersionInfo> infoList, int activityId = 0)
        {
            // avoid unnecessary index queries
            if (infoList == null || infoList.Count == 0)
                return;

            //-- select dirty documents
            var dirtyVersions = infoList.Where(i => !i.IsActualDocument &&
                    (i.OriginalIsPublic != i.ExpectedIsPublic || i.OriginalIsMajor != i.ExpectedIsMajor ||
                    i.OriginalIsLastDraft != i.ExpectedIsLastDraft || i.OriginalIsLastPublic != i.ExpectedIsLastPublic)).ToArray();

            //-- play dirty documents 
            var docs = IndexDocumentInfo.GetDocuments(dirtyVersions.Select(d => d.VersionId));
            foreach (var doc in docs)
            {
                var versionId = Int32.Parse(doc.Get(LucObject.FieldName.VersionId));
                foreach (var dirtyVersion in dirtyVersions)
                {
                    if (dirtyVersion.VersionId == versionId)
                    {
                        dirtyVersion.Document = doc;
                        SetDocumentFlags(dirtyVersion);
                        var delTerm = new Term(KeyFieldName, Lucene.Net.Util.NumericUtils.IntToPrefixCoded(dirtyVersion.VersionId));

                        using (var wrFrame = IndexWriterFrame.Get(false)) // // UpdateDirtyDocuments
                        {
                            //in case the system was shut down in the meantime
                            if (!LuceneManager.Running && !_executingUnprocessedIndexingActivities)
                            {
#if INDEX
                                DetailedLogger.Log("LM: Exit from UpdateDirtyDocuments. LuceneManager.Running: {0}, _executingUnprocessedIndexingActivities: {1}" // category: INDEX
                                    , LuceneManager.Running, _executingUnprocessedIndexingActivities);
#endif
                                return;
                            }
#if INDEX
                            LogVersionInfo(dirtyVersion, "UpdateDirtyDocuments BEFORE update", activityId);
#endif
                            wrFrame.IndexWriter.UpdateDocument(delTerm, dirtyVersion.Document);
                        }

                        break;
                    }
                }
            }
        }

        public static List<Document> GetDocumentsByNodeId(int nodeId)
        {
            using (var readerFrame = LuceneManager.GetIndexReaderFrame())
            {
                var termDocs = readerFrame.IndexReader.TermDocs(new Term(LucObject.FieldName.NodeId, Lucene.Net.Util.NumericUtils.IntToPrefixCoded(nodeId)));
                return GetDocumentsFromTermDocs(termDocs, readerFrame);
            }
        }
        private static List<Document> GetDocumentsByNodeId(string nodeId)
        {
            using (var readerFrame = LuceneManager.GetIndexReaderFrame())
            {
                var termDocs = readerFrame.IndexReader.TermDocs(new Term(LucObject.FieldName.NodeId, Lucene.Net.Util.NumericUtils.IntToPrefixCoded(Int32.Parse(nodeId))));
                return GetDocumentsFromTermDocs(termDocs, readerFrame);
            }
        }
        internal static List<Document> GetDocumentByVersionId(int versionId)
        {
            using (var readerFrame = LuceneManager.GetIndexReaderFrame())
            {
                var termDocs = readerFrame.IndexReader.TermDocs(new Term(LucObject.FieldName.VersionId, Lucene.Net.Util.NumericUtils.IntToPrefixCoded(versionId)));
                return GetDocumentsFromTermDocs(termDocs, readerFrame);
            }
        }
        private static List<Document> GetDocumentsFromTermDocs(TermDocs termDocs, IndexReaderFrame readerFrame)
        {
            var docs = new List<Document>();
            while (termDocs.Next())
                docs.Add(readerFrame.IndexReader.Document(termDocs.Doc()));
            docs.Sort(new DocumentVersionComparer());
            return docs;
        }

        internal static string TraceDoc(Document doc)
        {
            return string.Format("{0}, {1}, {2}, LD:{3}, LP:{4}, M:{5}, P:{6}",
                doc.Get(LucObject.FieldName.Path),
                doc.Get(LucObject.FieldName.Version),
                doc.Get(LucObject.FieldName.VersionId),
                doc.Get(LucObject.FieldName.IsLastDraft) ?? "[null]",
                doc.Get(LucObject.FieldName.IsLastPublic) ?? "[null]",
                doc.Get(LucObject.FieldName.IsMajor) ?? "[null]",
                doc.Get(LucObject.FieldName.IsPublic) ?? "[null]");
        }

        private const string LOGPREFIX_FLAGS = "LM.Flags:";
        internal static void LogVersionFlagConsistency(Document document, string tag, bool markError = true, int activityId = 0)
        {
            var nodeId = int.Parse(document.Get(LucObject.FieldName.NodeId));
            LogVersionFlagConsistency(nodeId, tag, markError, activityId);
        }
        internal static void LogVersionFlagConsistency(int nodeId, string tag, bool markError = true, int activityId = 0)
        {
            var docs = GetDocumentsByNodeId(nodeId);
            var errorText = markError ? " ERROR" : string.Empty;

            if (docs.Count == 0)
            {
                DetailedLogger.Log(LOGPREFIX_FLAGS + " * {0} * No version found for node id {1} ActivityId:{2}", tag ?? string.Empty, nodeId, activityId);
                return;
            }

            var lastDoc = docs.Last();
            var lastIsDraftFieldValue = lastDoc.Get(LucObject.FieldName.IsLastDraft);
            if (string.IsNullOrEmpty(lastIsDraftFieldValue) || String.Compare(lastIsDraftFieldValue, "yes", StringComparison.OrdinalIgnoreCase) != 0)
            {
                DetailedLogger.Log(LOGPREFIX_FLAGS + errorText + " * {0} * IsLastDraft is not set. ActivityId:{3} NodeId: {1}, Versions: {2}", tag ?? string.Empty, nodeId, GetVersionsLog(docs), activityId);
                return;
            }

            var versions = docs.Select(d => d.Get(LucObject.FieldName.Version)).ToList();
            if (versions.Count > versions.Distinct().Count())
            {
                DetailedLogger.Log(LOGPREFIX_FLAGS + errorText + " * {0} * Duplicate versions. ActivityId:{3} NodeId: {1}, Versions: {2}", tag ?? string.Empty, nodeId, GetVersionsLog(docs), activityId);
                return;
            }

            DetailedLogger.Log(LOGPREFIX_FLAGS + " * {0} * IsLastDraft is correct. ActivityId:{3} NodeId: {1}, Versions: {2}", tag ?? string.Empty, nodeId, GetVersionsLog(docs), activityId);
        }

        private static void LogVersionInfo(VersionInfo versionInfo, string tag, int activityId = 0)
        {
            DetailedLogger.Log(LOGPREFIX_FLAGS + " * {0} * ActivityId:{1} VersionId:{2} Version:{3} ExpectedIsLastDraft:{4}, ExpectedIsLastPublic:{5}", tag, activityId,
                versionInfo.VersionId,
                versionInfo.Version,
                versionInfo.ExpectedIsLastDraft,
                versionInfo.ExpectedIsLastPublic);
        }

        private static string GetVersionsLog(IEnumerable<Document> documents)
        {
            return string.Join(", ", documents.Select(d => string.Format("{0} / {1} / {2}",
                d.Get(LucObject.FieldName.VersionId),
                d.Get(LucObject.FieldName.Version).ToUpper(),
                d.Get(LucObject.FieldName.IsLastDraft))));
        }

        private static string GetDocumentLog(Document document)
        {
            return string.Format("NodeId:{0} VersionId:{1} Version:{2} IsLastDraft:{3} IsLastPublic:{4}",
                document.Get(LucObject.FieldName.NodeId),
                document.Get(LucObject.FieldName.VersionId),
                document.Get(LucObject.FieldName.Version),
                document.Get(LucObject.FieldName.IsLastDraft),
                document.Get(LucObject.FieldName.IsLastPublic));
        }

        private static int GetIntFromPrefixCode(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            try
            {
                return NumericUtils.PrefixCodedToInt(text);
            }
            catch
            {
                // we cannot do much here
            }

            return -1;
        }
    }
}

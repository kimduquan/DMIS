using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using System.Data.Common;
using System.Diagnostics;
using ContentRepository.Storage.Search;
using ContentRepository.Storage.Search.Internal;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Security;
using ContentRepository.Storage.ApplicationMessaging;
using System.Web;
using ContentRepository.Storage.Caching.Dependency;
using System.Globalization;
using Diagnostics;
using System.Text;

namespace ContentRepository.Storage.Data
{
    public class FileStreamData
    {
        public string Path { get; set; }
        public byte[] TransactionContext { get; set; }
    }

    public enum InitialCatalog
    {
        Initial,
        Master
    }

    [DebuggerDisplay("{RepositoryConfiguration.ConnectionString}", Type = "{RepositoryConfiguration.DataProviderClassName}")]
	public abstract class DataProvider
	{
        //////////////////////////////////////// Static Access ////////////////////////////////////////

        private static DataProvider _current;
        private static readonly object _lock = new object();

        public static DataProvider Current
		{
            [DebuggerStepThrough]
			get
			{
                if(_current == null)
                {
                    lock(_lock)
                    {
						if (_current == null)
						{
							try
							{
								_current = (DataProvider)TypeHandler.CreateInstance(RepositoryConfiguration.DataProviderClassName);
							}
							catch (TypeNotFoundException) //rethrow
							{
								throw new ConfigurationException(String.Concat(SR.Exceptions.Configuration.Msg_DataProviderImplementationDoesNotExist, ": ", RepositoryConfiguration.DataProviderClassName));
							}
							catch (InvalidCastException) //rethrow
							{
								throw new ConfigurationException(String.Concat(SR.Exceptions.Configuration.Msg_InvalidDataProviderImplementation, ": ", RepositoryConfiguration.DataProviderClassName));
							}
                            Logger.WriteInformation("DataProvider created.", Logger.GetDefaultProperties, _current);
                        }
                    }
                }
                return _current;
            }
		}

        //////////////////////////////////////// Initialization ////////////////////////////////////////

        internal static void InitializeForTests()
        {
            Current.InitializeForTestsPrivate();
        }
        protected abstract void InitializeForTestsPrivate();

        //////////////////////////////////////// Transactionality ////////////////////////////////////////

        public static DbTransaction GetCurrentTransaction()
        {
            return Current.GetCurrentTransactionInternal();
        }
        protected abstract DbTransaction GetCurrentTransactionInternal();

        //////////////////////////////////////// Generic Datalayer Logic ////////////////////////////////////////

        internal static bool NodeExists(string path)
        {
            return Current.NodeExistsInDatabase(path);
        }
        protected abstract bool NodeExistsInDatabase(string path);
        public abstract string GetNameOfLastNodeWithNameBase(int parentId, string namebase, string extension);

        internal void LoadNodeData(IEnumerable<NodeToken> tokens)
        {
            var buildersByVersionId = new Dictionary<int, NodeBuilder>();
            foreach (NodeToken token in tokens)
            {
                if (token.VersionId == 0)
                    throw new NotSupportedException("Cannot load a node if the versionId is 0.");
                if (!buildersByVersionId.ContainsKey(token.VersionId))
                    buildersByVersionId.Add(token.VersionId, new NodeBuilder(token));
            }
            if (buildersByVersionId.Count != 0)
                LoadNodes(buildersByVersionId);
        }

        internal void SaveNodeData(NodeData nodeData, NodeSaveSettings settings, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            lastMajorVersionId = 0;
            lastMinorVersionId = 0;

            bool isNewNode = nodeData.Id == 0; // shortcut

            var parent = NodeHead.Get(nodeData.ParentId);
            var path = RepositoryPath.Combine(parent.Path, nodeData.Name);

            Node.AssertPath(path);

            nodeData.Path = path;

            var writer = this.CreateNodeWriter();
            try
            {
                var savingAlgorithm = settings.GetSavingAlgorithm();

                writer.Open();
                if (settings.NeedToSaveData)
                {
                    SaveNodeBaseData(nodeData, savingAlgorithm, writer, settings, out lastMajorVersionId, out lastMinorVersionId);

                    BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.SaveNodeBaseData, nodeData.SavingTimer.ElapsedTicks);
                    nodeData.SavingTimer.Restart();

                    if (!isNewNode && nodeData.PathChanged && nodeData.SharedData != null)
                        writer.UpdateSubTreePath(nodeData.SharedData.Path, nodeData.Path);
                    SaveNodeProperties(nodeData, savingAlgorithm, writer, isNewNode);
                }
                else
                {
                    writer.UpdateNodeRow(nodeData);
                }
                writer.Close();

                BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.SaveNodeFlatProperties, nodeData.SavingTimer.ElapsedTicks);
                nodeData.SavingTimer.Restart();

                foreach (var versionId in settings.DeletableVersionIds)
                    DeleteVersion(versionId, nodeData, out lastMajorVersionId, out lastMinorVersionId);
            }
            catch //rethrow
            {
                if (isNewNode)
                {
                    // Failed save: set NodeId back to 0
                    nodeData.Id = 0;
                }

                throw;
            }
        }
        private static void SaveNodeBaseData(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, NodeSaveSettings settings, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            switch (savingAlgorithm)
            {
                case SavingAlgorithm.CreateNewNode:
                    //nodeData.Id = writer.InsertNodeRow(nodeData);
                    //nodeData.VersionId = writer.InsertVersionRow(nodeData);
                    writer.InsertNodeAndVersionRows(nodeData, out lastMajorVersionId, out lastMinorVersionId);
                    break;
                case SavingAlgorithm.UpdateSameVersion:
                    writer.UpdateNodeRow(nodeData);
                    writer.UpdateVersionRow(nodeData, out lastMajorVersionId, out lastMinorVersionId);
                    break;
                case SavingAlgorithm.CopyToNewVersionAndUpdate:
                    writer.UpdateNodeRow(nodeData);
                    writer.CopyAndUpdateVersion(nodeData, settings.CurrentVersionId, out lastMajorVersionId, out lastMinorVersionId);
                    break;
                case SavingAlgorithm.CopyToSpecifiedVersionAndUpdate:
                    writer.UpdateNodeRow(nodeData);
                    writer.CopyAndUpdateVersion(nodeData, settings.CurrentVersionId, settings.ExpectedVersionId, out lastMajorVersionId, out lastMinorVersionId);
                    break;
                default:
                    throw new NotImplementedException("Unknown SavingAlgorithm: " + savingAlgorithm);
            }
        }
        private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        {
            int versionId = nodeData.VersionId;
            foreach (var propertyType in nodeData.PropertyTypes)
            {
                var slotValue = nodeData.GetDynamicRawData(propertyType) ?? propertyType.DefaultValue;
                bool isModified = nodeData.IsModified(propertyType);

                if (!isModified && !isNewNode)
                    continue;

                switch (propertyType.DataType)
                {
                    case DataType.String:
                        writer.SaveStringProperty(versionId, propertyType, (string)slotValue);
                        break;
                    case DataType.DateTime:
                        writer.SaveDateTimeProperty(versionId, propertyType, (DateTime)slotValue);
                        break;
                    case DataType.Int:
                        writer.SaveIntProperty(versionId, propertyType, (int)slotValue);
                        break;
                    case DataType.Currency:
                        writer.SaveCurrencyProperty(versionId, propertyType, (decimal)slotValue);
                        break;
                    case DataType.Text:
                        writer.SaveTextProperty(versionId, propertyType, true, (string)slotValue);//TODO: ?? isLoaded property handling
                        BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.SaveNodeTextProperties, nodeData.SavingTimer.ElapsedTicks);
                        nodeData.SavingTimer.Restart();
                        break;
                    case DataType.Reference:
                        var ids = (IEnumerable<int>) slotValue;
                        if (!isNewNode || (ids != null && ids.Count() > 0))
                        {
                            var ids1 = ids.Distinct().ToList();
                            if (ids1.Count != ids.Count())
                                nodeData.SetDynamicRawData(propertyType, ids1);
                            writer.SaveReferenceProperty(versionId, propertyType, ids1);
                        }
                        BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.SaveNodeReferenceProperties, nodeData.SavingTimer.ElapsedTicks);
                        nodeData.SavingTimer.Restart();
                        break;
                    case DataType.Binary:
                        var binValue = (BinaryDataValue)slotValue;
                        if (binValue != null)
                        {
                            if (!binValue.IsEmpty)
                            {
                                var vId = nodeData.SharedData == null ? nodeData.VersionId : nodeData.SharedData.VersionId;
                                if (binValue.Stream == null && binValue.Size != -1)
                                    binValue.Stream = DataBackingStore.GetBinaryStream(vId, propertyType.Id);
                            }
                        }

                        if (binValue == null || binValue.IsEmpty)
                        {
                            writer.DeleteBinaryProperty(versionId, propertyType);
                        }
                        else if (binValue.Id == 0 || savingAlgorithm != SavingAlgorithm.UpdateSameVersion)
                        {
                            var id = writer.InsertBinaryProperty(versionId, propertyType.Id, binValue, isNewNode);
                            binValue.Id = id;
                        }
                        else
                        {
                            writer.UpdateBinaryProperty(binValue.Id, binValue);
                        }

                        BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.SaveNodeBinaries, nodeData.SavingTimer.ElapsedTicks);
                        nodeData.SavingTimer.Restart();
                        break;
                    default:
                        throw new NotSupportedException(propertyType.DataType.ToString());
                }
            }
        }

        public abstract DateTime RoundDateTime(DateTime d);

        //////////////////////////////////////// Abstract Schema Members ////////////////////////////////////////

        protected internal abstract DataSet LoadSchema();
        protected internal abstract void Reset();
		protected internal abstract SchemaWriter CreateSchemaWriter();
		public abstract Dictionary<DataType, int> ContentListMappingOffsets { get;}
        protected internal abstract int ContentListStartPage { get; }
        public static PropertyMapping GetPropertyMapping(PropertyType propType)
        {
            return Current.GetPropertyMappingInternal(propType);
        }
        protected abstract PropertyMapping GetPropertyMappingInternal(PropertyType propType);
        public abstract void AssertSchemaTimestampAndWriteModificationDate(long timestamp);

		//////////////////////////////////////// Abstract Node Members ////////////////////////////////////////

        public abstract int PathMaxLength { get; }
        public abstract DateTime DateTimeMinValue { get; }
		public abstract DateTime DateTimeMaxValue { get;}
        public abstract decimal DecimalMinValue { get; }
        public abstract decimal DecimalMaxValue { get; }

        protected internal abstract ITransactionProvider CreateTransaction();
        protected internal abstract INodeWriter CreateNodeWriter();

        protected internal abstract VersionNumber[] GetVersionNumbers(int nodeId);
        protected internal abstract VersionNumber[] GetVersionNumbers(string path);

        //protected internal abstract INodeQueryCompiler CreateNodeQueryCompiler();
        //protected internal abstract List<NodeToken> ExecuteQuery(NodeQuery query);

        // Load Nodes, Binary

        protected internal abstract void LoadNodes(Dictionary<int, NodeBuilder> buildersByVersionId);

        protected internal abstract bool IsCacheableText(string text);
        protected internal abstract string LoadTextPropertyValue(int versionId, int propertyTypeId);
        protected internal abstract BinaryDataValue LoadBinaryPropertyValue(int versionId, int propertyTypeId);
        protected internal abstract Stream LoadStream(int versionId, int propertyTypeId);

        //BIN2
        protected internal abstract BinaryCacheEntity LoadBinaryCacheEntity(int nodeVersionId, int propertyTypeId, out FileStreamData fileStreamData);
        protected internal abstract byte[] LoadBinaryFragment(int binaryPropertyId, long position, int count);
        protected internal virtual FileStreamData LoadFileStreamData(int binaryPropertyId, bool clearStream = false, int versionId = 0)
        {
            //Only SQL provider will override this, other providers may return null.
            return null;
        }

        protected internal virtual bool IsFilestreamEnabled()
        {
            return false;
        }

        //=============================================== Chunk upload

        protected internal abstract string StartChunk(int versionId, int propertyTypeId);

        protected internal abstract void WriteChunk(int versionId, string token, byte[] buffer, long offset, long fullSize);

        protected internal abstract void CommitChunk(int versionId, int propertyTypeId, string token, long fullSize, BinaryDataValue source = null);

        /////////////// Operations

        internal void MoveNode(int sourceNodeId, int targetNodeId, long sourceTimestamp, long targetTimestamp)
		{
			DataOperationResult result = MoveNodeTree(sourceNodeId, targetNodeId, sourceTimestamp, targetTimestamp);
			if (result == DataOperationResult.Successful)
				return;
			DataOperationException exc = new DataOperationException(result);
			exc.Data.Add("SourceNodeId", sourceNodeId);
			exc.Data.Add("TargetNodeId", targetNodeId);
			throw exc;
		}
		internal void DeleteNode(int nodeId)
		{
			DataOperationResult result = DeleteNodeTree(nodeId);
			if (result == DataOperationResult.Successful)
				return;
			DataOperationException exc = new DataOperationException(result);
			exc.Data.Add("NodeId", nodeId);
			throw exc;
		}
		internal void DeleteNodePsychical(int nodeId, long timestamp)
		{
			DataOperationResult result = DeleteNodeTreePsychical(nodeId, timestamp);
			if (result == DataOperationResult.Successful)
				return;
			DataOperationException exc = new DataOperationException(result);
			exc.Data.Add("NodeId", nodeId);
			throw exc;
		}
        protected internal abstract IEnumerable<NodeType> LoadChildTypesToAllow(int sourceNodeId);
        protected internal abstract DataOperationResult MoveNodeTree(int sourceNodeId, int targetNodeId, long sourceTimestamp = 0, long targetTimestamp = 0);
		protected internal abstract DataOperationResult DeleteNodeTree(int nodeId);
        protected internal abstract DataOperationResult DeleteNodeTreePsychical(int nodeId, long timestamp);
        protected internal abstract bool HasChild(int nodeId);
        protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);

        /////////////// Security

        protected internal abstract string GetPermissionLoaderScript();
		protected internal abstract Dictionary<int, List<int>> LoadMemberships();
        [Obsolete("Not performant. Use SetPermission(SecurityEntry) instead.")]
        protected internal abstract void SetPermission(int principalId, int nodeId, PermissionType permissionType, bool isInheritable, PermissionValue permissionValue);
        protected internal abstract void SetPermission(SecurityEntry entry);
        protected internal abstract void ExplicateGroupMemberships();
        protected internal abstract void ExplicateOrganizationUnitMemberships(IUser user);
        protected internal abstract void BreakInheritance(int nodeId);
        protected internal abstract void RemoveBreakInheritance(int nodeId);

        internal static bool IsInGroup(int groupId, int containerGroupId)
        {
            var membership = Current.LoadGroupMembership(containerGroupId);
            return membership.Contains(groupId);
        }
        protected internal abstract List<int> LoadGroupMembership(int groupId);
        internal abstract int LoadLastModifiersGroupId();

        /////////////// Application (former Cluster) Messaging

        protected internal abstract void PersistUploadToken(UploadToken value);
        protected internal abstract int GetUserIdByUploadGuid (Guid uploadGuid);

        //====================================================== AppModel script generator

        public static string GetAppModelScript(IEnumerable<string> paths, bool resolveAll, bool resolveChildren)
        {
            return Current.GetAppModelScriptPrivate(paths, resolveAll, resolveChildren);
        }
        protected abstract string GetAppModelScriptPrivate(IEnumerable<string> paths, bool all, bool resolveChildren);

        //====================================================== Custom database script support

        public static IDataProcedure CreateDataProcedure(string commandText, string connectionName = null, InitialCatalog initialCatalog = InitialCatalog.Initial)
        {
            return Current.CreateDataProcedureInternal(commandText, connectionName, initialCatalog);
        }
        public static IDbDataParameter CreateParameter()
        {
            return Current.CreateParameterInternal();
        }
        protected internal abstract IDataProcedure CreateDataProcedureInternal(string commandText, string connectionName = null, InitialCatalog initialCatalog = InitialCatalog.Initial);
        protected abstract IDbDataParameter CreateParameterInternal();

        public static void CheckScript(string commandText)
        {
            Current.CheckScriptInternal(commandText);
        }
        protected internal abstract void CheckScriptInternal(string commandText);
       
		//====================================================== Tools

		protected void ReadNodeTokens(DbDataReader reader, List<NodeToken> targetList)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");
			if (targetList == null)
                throw new ArgumentNullException("targetList");
			while (reader.Read())
				targetList.Add(GetNodeTokenFromReader(reader));
		}
		private static NodeToken GetNodeTokenFromReader(DbDataReader reader)
		{
			int id = reader.GetInt32(reader.GetOrdinal("NodeId"));
			int versionId = reader.GetInt32(reader.GetOrdinal("VersionId"));
			int major = reader.GetInt16(reader.GetOrdinal("MajorNumber"));
			int minor = reader.GetInt16(reader.GetOrdinal("MinorNumber"));
			int status = reader.GetInt16(reader.GetOrdinal("Status"));
			int nodeTypeId = reader.GetInt32(reader.GetOrdinal("NodeTypeId"));
            int listId = TypeConverter.ToInt32(reader.GetValue(reader.GetOrdinal("ContentListId")));
            int listTypeId = TypeConverter.ToInt32(reader.GetValue(reader.GetOrdinal("ContentListTypeId")));
			VersionNumber versionNumber = new VersionNumber(major, minor, (VersionStatus)status);

			return new NodeToken(id, nodeTypeId, listId, listTypeId, versionId, versionNumber);
		}

		public void SaveTextProperty(int versionId, PropertyType propertyType, string value)
		{
			INodeWriter writer = this.CreateNodeWriter();
			writer.SaveTextProperty(versionId, propertyType, true, value);
			writer.Close();
		}

        protected internal abstract NodeHead LoadNodeHead(string path);
        protected internal abstract NodeHead LoadNodeHead(int nodeId);
        protected internal abstract NodeHead LoadNodeHeadByVersionId(int versionId);
        protected internal abstract IEnumerable<NodeHead> LoadNodeHeads(IEnumerable<int> heads);

        protected internal abstract NodeHead.NodeVersion[] GetNodeVersions(int nodeId);

        protected internal abstract long GetTreeSize(string path, bool includeChildren);

        public static int GetNodeCount()
        {
            return Current.NodeCount(null);
        }
        public static int GetNodeCount(string path)
        {
            return Current.NodeCount(path);
        }
        public static int GetVersionCount()
        {
            return Current.VersionCount(null);
        }
        public static int GetVersionCount(string path)
        {
            return Current.VersionCount(path);
        }
        protected abstract int NodeCount(string path);
        protected abstract int VersionCount(string path);

        //====================================================== Index document save / load operations

        internal static void SaveIndexDocument(NodeData nodeData, byte[] indexDocumentBytes)
        {
            Current.UpdateIndexDocument(nodeData, indexDocumentBytes);
        }
        public static void SaveIndexDocument(int versionId, byte[] indexDocumentBytes)
        {
            Current.UpdateIndexDocument(versionId, indexDocumentBytes);
        }
        internal static IndexDocumentData LoadIndexDocument(int versionId)
        {
            return Current.LoadIndexDocumentByVersionId(versionId);
        }
        internal static IEnumerable<IndexDocumentData> LoadIndexDocument(IEnumerable<int> versionId)
        {
            return Current.LoadIndexDocumentByVersionId(versionId);
        }
        internal static IEnumerable<IndexDocumentData> LoadIndexDocumentsByPath(string path)
        {
            var proc = Current.CreateLoadIndexDocumentCollectionByPathProcedure(path);
            proc.Connection.Open();
            var reader = proc.ExecuteReader();
            return new IndexDocumentDataCollection(reader);
        }

        protected internal abstract void UpdateIndexDocument(NodeData nodeData, byte[] indexDocumentBytes);
        protected internal abstract void UpdateIndexDocument(int versionId, byte[] indexDocumentBytes);
        protected internal abstract IndexDocumentData LoadIndexDocumentByVersionId(int versionId);
        protected internal abstract IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId);

        protected internal abstract DbCommand CreateLoadIndexDocumentCollectionByPathProcedure(string path);
        protected internal abstract IndexDocumentData GetIndexDocumentDataFromReader(DbDataReader reader);

        public static IEnumerable<int> LoadIdsOfNodesThatDoNotHaveIndexDocument()
        {
            return Current.GetIdsOfNodesThatDoNotHaveIndexDocument();
        }
        protected internal abstract IEnumerable<int> GetIdsOfNodesThatDoNotHaveIndexDocument();

		//====================================================== Index backup / restore operations

        internal static Guid StoreIndexBackupToDb(string backupFilePath, IndexBackupProgress progress)
        {
            var lastBackup = Current.LoadLastBackup();
            var backupNumber = lastBackup == null ? 1 : lastBackup.BackupNumber + 1;
            var backup = Current.CreateBackup(backupNumber);
            Current.StoreBackupStream(backupFilePath, backup, progress);
            Current.SetActiveBackup(backup, lastBackup);
            return backup.RowGuid; // backup.BackupNumber;
        }
        protected internal abstract IndexBackup LoadLastBackup();
        protected internal abstract IndexBackup CreateBackup(int backupNumber);
        protected internal abstract void StoreBackupStream(string backupFilePath, IndexBackup backup, IndexBackupProgress progress);
        protected internal abstract void SetActiveBackup(IndexBackup backup, IndexBackup lastBackup);

        internal static void DeleteUnnecessaryBackups()
        {
            Current.KeepOnlyLastIndexBackup();
        }
        protected abstract void KeepOnlyLastIndexBackup();

        //------------------------------------------------------

        internal static Guid GetLastStoredBackupNumber()
        {
            return Current.GetLastIndexBackupNumber();
        }
        protected abstract Guid GetLastIndexBackupNumber();

        //------------------------------------------------------

        internal static IndexBackup RecoverIndexBackupFromDb(string backupFilePath)
        {
            return Current.RecoverIndexBackup(backupFilePath);
        }
        protected abstract IndexBackup RecoverIndexBackup(string backupFilePath);

        //------------------------------------------------------

        public abstract int GetLastActivityId();

        //====================================================== Checking  index integrity

        public abstract IDataProcedure GetTimestampDataForOneNodeIntegrityCheck(string path);
        public abstract IDataProcedure GetTimestampDataForRecursiveIntegrityCheck(string path);

        //====================================================== Database backup / restore operations

        public abstract string DatabaseName { get; }
        public abstract IEnumerable<string> GetScriptsForDatabaseBackup();

		//======================================================

        internal static long GetLongFromBytes(byte[] bytes)
        {
            var @long = 0L;
            for (int i = 0; i < bytes.Length; i++)
                @long = (@long << 8) + bytes[i];
            return @long;
        }
        internal static byte[] GetBytesFromLong(long @long)
        {
            var bytes = new byte[8];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[7 - i] = (byte)(@long & 0xFF);
                @long = @long >> 8;
            }
            return bytes;
        }

        protected internal abstract List<ContentListType> GetContentListTypesInTree(string path);

        //====================================================== NodeQuery substitutions

        protected internal abstract IEnumerable<int> GetChildrenIdentfiers(int nodeId);
        protected internal abstract int InstanceCount(int[] nodeTypeIds);
        protected internal abstract IEnumerable<int> QueryNodesByPath(string pathStart, bool orderByPath);
        protected internal abstract IEnumerable<int> QueryNodesByType(int[] typeIds);
        protected internal abstract IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string pathStart, bool orderByPath);
        protected internal abstract IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string[] pathStart, bool orderByPath);
        protected internal abstract IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string pathStart, bool orderByPath, string name);
        protected internal abstract IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name);
        protected internal abstract IEnumerable<int> QueryNodesByTypeAndPathAndProperty(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties);
        protected internal abstract IEnumerable<int> QueryNodesByReferenceAndType(string referenceName, int referredNodeId, int[] allowedTypeIds);

        //====================================================== Powershell provider

        protected internal abstract int InitializeStagingBinaryData(int versionId, int propertyTypeId, string fileName, long fileSize);
        protected internal abstract void SaveChunk(int stagingBinaryDataId, byte[] bytes, int offset);
        protected internal abstract void CopyStagingToBinaryData(int versionId, int propertyTypeId, int stagingBinaryDataId, string checksum);
        protected internal abstract void DeleteStagingBinaryData(int stagingBinaryDataId);

        //====================================================== Packaging

        public abstract ApplicationInfo CreateInitialVersion(string name, string edition, Version version, string description);
        public abstract ApplicationInfo LoadOfficialVersion();
        public abstract IEnumerable<ApplicationInfo> LoadInstalledApplications();
        public abstract IEnumerable<Package> LoadInstalledPackages();
        public abstract void SavePackage(Package package);
        public abstract void UpdatePackage(Package package);
        public abstract bool IsPackageExist(string appId, PackageType packageType, PackageLevel packageLevel, Version version);
        public abstract void DeletePackage(Package package);
        internal abstract void DeletePackagesExceptFirst();
    }

    public class QueryPropertyData
    {
        public string PropertyName { get; set; }
        public object Value { get; set; }

        private Operator _queryOperator = Operator.Equal;
        public Operator QueryOperator
        {
            get { return _queryOperator; }
            set { _queryOperator = value; }
        }
    }

    internal class IndexDocumentDataCollection : IEnumerable<IndexDocumentData>
    {
        private class IndexDocumentDataCollectionEnumerator : IEnumerator<IndexDocumentData>
        {
            DbDataReader _reader;

            public IndexDocumentDataCollectionEnumerator(DbDataReader reader)
            {
                _reader = reader;
            }

            public IndexDocumentData Current { get { return DataProvider.Current.GetIndexDocumentDataFromReader(_reader); } }
            public bool MoveNext()
            {
                return _reader.Read();
            }
            public void Reset()
            {
                throw new NotSupportedException();
            }

            object System.Collections.IEnumerator.Current { get { return Current; } }

            //=============================== IDisposable pattern

            bool _disposed;
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            private void Dispose(bool disposing)
            {
                if (!this._disposed)
                    if (disposing)
                        _reader.Dispose();
                _disposed = true;
            }
            ~IndexDocumentDataCollectionEnumerator()
            {
                Dispose(false);
            }
        }

        DbDataReader _reader;
        public IndexDocumentDataCollection(DbDataReader reader)
        {
            _reader = reader;
        }

        public IEnumerator<IndexDocumentData> GetEnumerator()
        {
            return new IndexDocumentDataCollectionEnumerator(_reader);
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [Serializable]
    public class IndexDocumentData
    {
        [NonSerialized]
        object _indexDocumentInfo;
        public object IndexDocumentInfo
        {
            get
            {
                if (_indexDocumentInfo == null)
                    _indexDocumentInfo = StorageContext.Search.SearchEngine.DeserializeIndexDocumentInfo(IndexDocumentInfoBytes);
                return _indexDocumentInfo;
            }
        }
        byte[] _indexDocumentInfoBytes;
        public byte[] IndexDocumentInfoBytes
        {
            get
            {
                if (_indexDocumentInfoBytes == null)
                {
                    using (var docStream = new MemoryStream())
                    {
                        var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        formatter.Serialize(docStream, _indexDocumentInfo);
                        docStream.Flush();
                        IndexDocumentInfoSize = docStream.Length;
                        _indexDocumentInfoBytes = docStream.GetBuffer();
                    }
                }
                return _indexDocumentInfoBytes;
            }

        }
        public long? IndexDocumentInfoSize { get; set; }

        public int NodeTypeId { get; set; }
        public int VersionId { get; set; }
        public int NodeId { get; set; }
        public string Path { get; set; }
        public int ParentId { get; set; }
        public bool IsSystem { get; set; }
        public bool IsLastDraft { get; set; }
        public bool IsLastPublic { get; set; }
        public long NodeTimestamp { get; set; }
        public long VersionTimestamp { get; set; }

        public IndexDocumentData(object indexDocumentInfo, byte[] indexDocumentInfoBytes)
        {
            _indexDocumentInfo = indexDocumentInfo;
            _indexDocumentInfoBytes = indexDocumentInfoBytes;
        }
    }

}

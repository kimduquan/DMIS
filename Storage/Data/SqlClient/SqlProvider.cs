using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ContentRepository.Storage.ApplicationMessaging;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Search;
using ContentRepository.Storage.Search.Internal;
using ContentRepository.Storage.Security;
using Diagnostics;
using Newtonsoft.Json;

namespace ContentRepository.Storage.Data.SqlClient
{
    internal static class DataReaderExtension
    {
        internal static int GetSafeInt32(this IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return 0;
            return reader.GetInt32(index);
        }
        internal static bool GetSafeBoolean(this IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return false;
            return Convert.ToBoolean(reader.GetByte(index));
        }
        internal static string GetSafeString(this IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return null;
            return reader.GetString(index);
        }

        internal static ContentSavingState GetSavingState(this IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return 0;
            return (ContentSavingState)reader.GetInt32(index);
        }
        internal static IEnumerable<ChangedData> GetChangedData(this IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return null;
            var src = reader.GetString(index);
            var data = (IEnumerable<ChangedData>)JsonConvert.DeserializeObject(src, typeof(IEnumerable<ChangedData>));
            return data;
        }
    }
    
    internal class SqlProvider : DataProvider
    {
        //////////////////////////////////////// Internal Constants ////////////////////////////////////////

        internal const int StringPageSize = 80;
        internal const int StringDataTypeSize = 450;
        internal const int IntPageSize = 40;
        internal const int DateTimePageSize = 25;
        internal const int CurrencyPageSize = 15;
        internal const int TextAlternationSizeLimit = 4000; // (Autoloaded)NVarchar -> (Lazy)NText
        internal const int CsvParamSize = 8000;
        internal const int BinaryStreamBufferLength = 32768;

        internal const string StringMappingPrefix = "nvarchar_";
        internal const string DateTimeMappingPrefix = "datetime_";
        internal const string IntMappingPrefix = "int_";
        internal const string CurrencyMappingPrefix = "money_";

        private int _contentListStartPage;
        private Dictionary<DataType, int> _contentListMappingOffsets;

        private static readonly string GET_FILESTREAM_TRANSACTION_BY_BINARYID =
                @"  SELECT FileStream.PathName() AS Path, GET_FILESTREAM_TRANSACTION_CONTEXT() AS TransactionContext          
                    FROM  dbo.BinaryProperties
                    WHERE BinaryPropertyId = @BinaryPropertyId";

        private static readonly string UPDATE_STREAM_WRITE_CHUNK_SECURITYERROR = "Version id and binary property id mismatch.";

        private static readonly string UPDATE_STREAM_WRITE_CHUNK_SECURITYCHECK = @"--SECURITY CHECK
IF (SELECT TOP(1) VersionId FROM BinaryProperties WHERE BinaryPropertyId = @BinaryPropertyId) <> @VersionId BEGIN
    RAISERROR (N'" + UPDATE_STREAM_WRITE_CHUNK_SECURITYERROR + @"', 12, 1);	
END
";
        private static readonly string UPDATE_STREAM_WRITE_CHUNK_TEMPLATE = UPDATE_STREAM_WRITE_CHUNK_SECURITYCHECK + @"
-- init for .WRITE
UPDATE BinaryProperties SET [Stream] = (CONVERT(varbinary, N'')) WHERE BinaryPropertyId = @BinaryPropertyId AND [Stream] IS NULL
-- fill to offset
DECLARE @StreamLength bigint
SELECT @StreamLength = DATALENGTH([Stream]) FROM BinaryProperties WHERE BinaryPropertyId = @BinaryPropertyId
IF @StreamLength < @Offset
	UPDATE BinaryProperties SET [Stream].WRITE(CONVERT( varbinary, REPLICATE(0x00, (@Offset - DATALENGTH([Stream])))), NULL, 0)
		WHERE BinaryPropertyId = @BinaryPropertyId
-- write payload
UPDATE BinaryProperties SET [Stream].WRITE(@Data, @Offset, DATALENGTH(@Data)){0} WHERE BinaryPropertyId = @BinaryPropertyId
";

        private readonly string UPDATE_STREAM_WRITE_CHUNK = string.Format(UPDATE_STREAM_WRITE_CHUNK_TEMPLATE, string.Empty);
        private readonly string UPDATE_STREAM_WRITE_CHUNK_FS = string.Format(UPDATE_STREAM_WRITE_CHUNK_TEMPLATE, ", [FileStream] = NULL");

        private static readonly string COMMIT_CHUNK =
                @"DELETE FROM BinaryProperties WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId AND Staging IS NULL;
                  UPDATE BinaryProperties SET [VersionId] = @VersionId, PropertyTypeId = @PropertyTypeId, [Size] = @Size, [Checksum] = @Checksum, 
                         ContentType = @ContentType, FileNameWithoutExtension = @FileNameWithoutExtension, Extension = @Extension, Staging = NULL
                  WHERE BinaryPropertyId = @BinaryPropertyId";

        private static readonly string CLEAR_STREAM_BY_BINARYID = UPDATE_STREAM_WRITE_CHUNK_SECURITYCHECK + @"UPDATE BinaryProperties
				  SET Stream = NULL, FileStream = CONVERT(varbinary, N'')
				  WHERE BinaryPropertyId = @BinaryPropertyId AND FileStream IS NULL;
        ";

        private static readonly string INSERT_STAGING_BINARY = @"DECLARE @ContentType varchar(50);
                DECLARE @FileNameWithoutExtension varchar(450);
                DECLARE @Extension varchar(50);

                DELETE FROM BinaryProperties
                WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId AND Staging IS NOT NULL;

                SELECT @ContentType = [ContentType], @FileNameWithoutExtension = [FileNameWithoutExtension], @Extension = [Extension]
                FROM BinaryProperties WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId AND Staging IS NULL;

                IF @ContentType IS NOT NULL
                    BEGIN
	                    INSERT INTO BinaryProperties
		                       ([VersionId],[PropertyTypeId],[ContentType],[FileNameWithoutExtension],[Extension],[Size],[Checksum],[CreationDate], [Staging])
	                    VALUES (@VersionId, @PropertyTypeId, @ContentType, @FileNameWithoutExtension, @Extension, 0, NULL, GETUTCDATE(), 1)
	        
                    END
                ELSE
                    BEGIN
	                    INSERT INTO BinaryProperties ([VersionId],[PropertyTypeId],[ContentType],[Extension],[Size],[CreationDate], [Staging]) VALUES (@VersionId, @PropertyTypeId, '', '', 0,GETUTCDATE(), 1)
                    END

                SELECT @@IDENTITY";

        private static readonly string LOAD_BINARY_CACHEENTITY_FORMAT =
            @"SELECT Size, BinaryPropertyId, {0}                
            FROM dbo.BinaryProperties
            WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId AND Staging IS NULL";

        private static readonly string LOAD_BINARY_FRAGMENT =
            @"SELECT SUBSTRING([Stream], @Position, @Count)
            FROM dbo.BinaryProperties
            WHERE BinaryPropertyId = @BinaryPropertyId";

        private static readonly string LOAD_BINARY_FRAGMENT_FILESTREAM =
            @"SELECT 
	            CASE WHEN bp.FileStream IS NULL
			            THEN SUBSTRING(bp.[Stream], @Position, @Count)
			            ELSE SUBSTRING(bp.[FileStream], @Position, @Count)
		            END AS Stream
            FROM dbo.BinaryProperties as bp
            WHERE BinaryPropertyId = @BinaryPropertyId";

        private static readonly string LOAD_BINARY_CACHEENTITY_COLUMNS_FORMAT_FILESTREAM =
            @"CASE  WHEN Size < {0} AND FileStream IS NOT NULL THEN FileStream
                    WHEN Size < {0} AND FileStream IS NULL THEN Stream
		            ELSE null
	            END AS Stream,
                CASE
		            WHEN FileStream IS NULL THEN 0
		            ELSE 1
	            END AS UseFileStream,
                FileStream.PathName() AS Path,
                GET_FILESTREAM_TRANSACTION_CONTEXT() AS TransactionContext";

        private static readonly string LOAD_BINARY_CACHEENTITY_COLUMNS_FORMAT =
            @"CASE  WHEN Size < {0} THEN Stream
		            ELSE null
	            END AS Stream";

        public SqlProvider()
        {
            _contentListStartPage = 10000000;
            _contentListMappingOffsets = new Dictionary<DataType, int>();
            _contentListMappingOffsets.Add(DataType.String, StringPageSize * _contentListStartPage);
            _contentListMappingOffsets.Add(DataType.Int, IntPageSize * _contentListStartPage);
            _contentListMappingOffsets.Add(DataType.DateTime, DateTimePageSize * _contentListStartPage);
            _contentListMappingOffsets.Add(DataType.Currency, CurrencyPageSize * _contentListStartPage);
            _contentListMappingOffsets.Add(DataType.Binary, 0);
            _contentListMappingOffsets.Add(DataType.Reference, 0);
            _contentListMappingOffsets.Add(DataType.Text, 0);
        }


        public override int PathMaxLength
        {
            get { return StringDataTypeSize; }
        }
        public override DateTime DateTimeMinValue
        {
            get { return SqlDateTime.MinValue.Value; }
        }
        public override DateTime DateTimeMaxValue
        {
            get { return SqlDateTime.MaxValue.Value; }
        }
        public override decimal DecimalMinValue
        {
            get { return SqlMoney.MinValue.Value; }
        }
        public override decimal DecimalMaxValue
        {
            get { return SqlMoney.MaxValue.Value; }
        }

        protected internal override ITransactionProvider CreateTransaction()
        {
            return new Transaction();
        }

        protected internal override INodeWriter CreateNodeWriter()
        {
            return new SqlNodeWriter();
        }

        protected internal override SchemaWriter CreateSchemaWriter()
        {
            return new SqlSchemaWriter();
        }

        //////////////////////////////////////// Initialization ////////////////////////////////////////

        protected override void InitializeForTestsPrivate()
        {
            var proc = CreateDataProcedure(@"
ALTER TABLE [BinaryProperties] CHECK CONSTRAINT ALL
ALTER TABLE [FlatProperties] CHECK CONSTRAINT ALL
ALTER TABLE [Nodes] CHECK CONSTRAINT ALL
ALTER TABLE [ReferenceProperties] CHECK CONSTRAINT ALL
ALTER TABLE [SecurityEntries] CHECK CONSTRAINT ALL
ALTER TABLE [SecurityCustomEntries] CHECK CONSTRAINT ALL
ALTER TABLE [SecurityMemberships] CHECK CONSTRAINT ALL
ALTER TABLE [TextPropertiesNText] CHECK CONSTRAINT ALL
ALTER TABLE [TextPropertiesNVarchar] CHECK CONSTRAINT ALL
ALTER TABLE [Versions] CHECK CONSTRAINT ALL
ALTER TABLE [StagingBinaryProperties] CHECK CONSTRAINT ALL
ALTER TABLE [VersionExtensions] CHECK CONSTRAINT ALL
");
            proc.CommandType = CommandType.Text;
            proc.ExecuteNonQuery();
        }

        //////////////////////////////////////// Transactionality ////////////////////////////////////////

        protected override System.Data.Common.DbTransaction GetCurrentTransactionInternal()
        {
            var provider = (ContentRepository.Storage.Data.SqlClient.Transaction)TransactionScope.Provider;
            if (provider == null)
                return null;
            return provider.Tran;
        }

        //////////////////////////////////////// Schema Members ////////////////////////////////////////

        protected internal override DataSet LoadSchema()
        {
            SqlConnection cn = new SqlConnection(RepositoryConfiguration.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cn;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "proc_Schema_LoadAll";
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataSet dataSet = new DataSet();

            try
            {
                cn.Open();
                adapter.Fill(dataSet);
            }
            finally
            {
                cn.Close();
            }

            dataSet.Tables[0].TableName = "SchemaModification";
            dataSet.Tables[1].TableName = "DataTypes";
            dataSet.Tables[2].TableName = "PropertySetTypes";
            dataSet.Tables[3].TableName = "PropertySets";
            dataSet.Tables[4].TableName = "PropertyTypes";
            dataSet.Tables[5].TableName = "PropertySetsPropertyTypes";
            dataSet.Tables[6].TableName = "Permissions";

            return dataSet;
        }

        protected internal override void Reset()
        {
            //TODO: Read the configuration if is exist
        }

        public override Dictionary<DataType, int> ContentListMappingOffsets
        {
            get { return _contentListMappingOffsets; }
        }

        protected internal override int ContentListStartPage
        {
            get { return _contentListStartPage; }
        }

        protected override PropertyMapping GetPropertyMappingInternal(PropertyType propType)
        {
            //internal const string StringMappingPrefix = "nvarchar_";
            //internal const string DateTimeMappingPrefix = "datetime_";
            //internal const string IntMappingPrefix = "int_";
            //internal const string CurrencyMappingPrefix = "money_";

            PropertyStorageSchema storageSchema = PropertyStorageSchema.SingleColumn;
            string tableName;
            string columnName;
            bool usePageIndex = false;
            int page = 0;

            switch (propType.DataType)
            {
                case DataType.String:
                    usePageIndex = true;
                    tableName = "FlatProperties";
                    columnName = SqlProvider.StringMappingPrefix + GetColumnIndex(propType.DataType, propType.Mapping, out page);
                    break;
                case DataType.Text:
                    usePageIndex = false;
                    tableName = "TextPropertiesNVarchar, TextPropertiesNText";
                    columnName = "Value";
                    storageSchema = PropertyStorageSchema.MultiTable;
                    break;
                case DataType.Int:
                    usePageIndex = true;
                    tableName = "FlatProperties";
                    columnName = SqlProvider.IntMappingPrefix + GetColumnIndex(propType.DataType, propType.Mapping, out page);
                    break;
                case DataType.Currency:
                    usePageIndex = true;
                    tableName = "FlatProperties";
                    columnName = SqlProvider.CurrencyMappingPrefix + GetColumnIndex(propType.DataType, propType.Mapping, out page);
                    break;
                case DataType.DateTime:
                    usePageIndex = true;
                    tableName = "FlatProperties";
                    columnName = SqlProvider.DateTimeMappingPrefix + GetColumnIndex(propType.DataType, propType.Mapping, out page);
                    break;
                case DataType.Binary:
                    usePageIndex = false;
                    tableName = "BinaryProperties";
                    columnName = "ContentType, FileNameWithoutExtension, Extension, Size, Stream";
                    storageSchema = PropertyStorageSchema.MultiColumn;
                    break;
                case DataType.Reference:
                    usePageIndex = false;
                    tableName = "ReferenceProperties";
                    columnName = "ReferredNodeId";
                    break;
                default:
                    throw new NotSupportedException("Unknown DataType" + propType.DataType);
            }
            return new PropertyMapping
            {
                StorageSchema = storageSchema,
                TableName = tableName,
                ColumnName = columnName,
                PageIndex = page,
                UsePageIndex = usePageIndex
            };
        }
        private static int GetColumnIndex(DataType dataType, int mapping, out int page)
        {
            //internal const int StringPageSize = 80;
            //internal const int StringDataTypeSize = 450;
            //internal const int IntPageSize = 40;
            //internal const int DateTimePageSize = 25;
            //internal const int CurrencyPageSize = 15;
            //internal const int TextAlternationSizeLimit = 4000; // (Autoloaded)NVarchar -> (Lazy)NText
            //internal const int CsvParamSize = 8000;
            //internal const int BinaryStreamBufferLength = 32768;
            int pageSize;
            switch (dataType)
            {
                case DataType.String: pageSize = SqlProvider.StringPageSize; break;
                case DataType.Int: pageSize = SqlProvider.IntPageSize; break;
                case DataType.DateTime: pageSize = SqlProvider.DateTimePageSize; break;
                case DataType.Currency: pageSize = SqlProvider.CurrencyPageSize; break;
                default:
                    page = 0;
                    return 0;
            }

            page = mapping / pageSize;
            int index = mapping % pageSize;
            return index + 1;
        }

        public override void AssertSchemaTimestampAndWriteModificationDate(long timestamp)
        {
            var script = @"DECLARE @Count INT
                            SELECT @Count = COUNT(*) FROM SchemaModification
                            IF @Count = 0
                                INSERT INTO SchemaModification (ModificationDate) VALUES (GETUTCDATE())
                            ELSE
                            BEGIN
                                UPDATE [SchemaModification] SET [ModificationDate] = GETUTCDATE() WHERE Timestamp = @Timestamp
                                IF @@ROWCOUNT = 0
                                    RAISERROR (N'Storage schema is out of date.', 12, 1);
                            END";

            using (var cmd = (SqlProcedure)DataProvider.CreateDataProcedure(script))
            {
                try
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add("@Timestamp", SqlDbType.Timestamp).Value = SqlProvider.GetBytesFromLong(timestamp);
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException sex) //rethrow
                {
                    throw new DataException(sex.Message, sex);
                }
            }
        }

        protected internal override IEnumerable<int> QueryNodesByPath(string pathStart, bool orderByPath)
        {
            return QueryNodesByTypeAndPath(null, pathStart, orderByPath);
        }
        protected internal override IEnumerable<int> QueryNodesByType(int[] nodeTypeIds)
        {
            return QueryNodesByTypeAndPath(nodeTypeIds, new string[0], false);
        }
        protected internal override IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string pathStart, bool orderByPath)
        {
            return QueryNodesByTypeAndPathAndName(nodeTypeIds, new[] { pathStart }, orderByPath, null);
        }
        protected internal override IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string[] pathStart, bool orderByPath)
        {
            return QueryNodesByTypeAndPathAndName(nodeTypeIds, pathStart, orderByPath, null);
        }
        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string pathStart, bool orderByPath, string name)
        {
            return QueryNodesByTypeAndPathAndName(nodeTypeIds, new[] { pathStart }, orderByPath, name);
        }
        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name)
        {
            var sql = new StringBuilder("SELECT NodeId FROM Nodes WHERE ");
            var first = true;

            if (pathStart != null && pathStart.Length > 0)
            {
                for (int i = 0; i < pathStart.Length; i++)
                    if (pathStart[i] != null)
                        pathStart[i] = pathStart[i].Replace("'", "''");

                sql.AppendLine("(");
                for (int i = 0; i < pathStart.Length; i++)
                {
                    if (i > 0)
                        sql.AppendLine().Append(" OR ");
                    sql.Append(" Path LIKE N'");
                    sql.Append(pathStart[i]);
                    if (!pathStart[i].EndsWith(RepositoryPath.PathSeparator))
                        sql.Append(RepositoryPath.PathSeparator);
                    sql.Append("%' COLLATE Latin1_General_CI_AS");
                }
                sql.AppendLine(")");
                first = false;
            }

            if (name != null)
            {
                name = name.Replace("'", "''");
                if (!first)
                    sql.Append(" AND");
                sql.Append(" Name = '").Append(name).Append("'");
                first = false;
            }

            if (nodeTypeIds != null)
            {
                if (!first)
                    sql.Append(" AND");
                sql.Append(" NodeTypeId");
                if (nodeTypeIds.Length == 1)
                    sql.Append(" = ").Append(nodeTypeIds[0]);
                else
                    sql.Append(" IN (").Append(String.Join(", ", nodeTypeIds)).Append(")");

                first = false;
            }

            if (orderByPath)
                sql.AppendLine().Append("ORDER BY Path");
            // first version
            //var cmd = new SqlProcedure { CommandText = sql.ToString(), CommandType = CommandType.Text };
            //SqlDataReader reader = null;
            //var result = new List<int>();
            //try
            //{
            //    reader = cmd.ExecuteReader();
            //    while (reader.Read())
            //        result.Add(reader.GetSafeInt32(0));
            //    return result;
            //}
            //finally
            //{
            //    if (reader != null && !reader.IsClosed)
            //        reader.Close();
            //    cmd.Dispose();
            //}

            //// second version
            //var result = new List<int>();
            //using (var conn = new SqlConnection(RepositoryConfiguration.ConnectionString))
            //{
            //    using (var cmd = new SqlCommand(sql.ToString(), conn) { CommandType = CommandType.Text })
            //    {
            //        conn.Open();
            //        using (var reader = cmd.ExecuteReader())
            //        {
            //            while (reader.Read())
            //                result.Add(reader.GetSafeInt32(0));
            //            return result;
            //        }
            //    }
            //}

            // 3th version
            var result = new List<int>();
            using (var cmd = DataProvider.CreateDataProcedure(sql.ToString()))
            {
                cmd.CommandType = CommandType.Text;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        result.Add(reader.GetSafeInt32(0));
                    return result;
                }
            }
        }
        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndProperty(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties)
        {
            var sql = new StringBuilder("SELECT NodeId FROM SysSearchWithFlatsView WHERE ");
            var first = true;

            if (pathStart != null)
            {
                pathStart = pathStart.Replace("'", "''");
                sql.Append(" Path LIKE N'");
                sql.Append(pathStart);
                if (!pathStart.EndsWith(RepositoryPath.PathSeparator))
                    sql.Append(RepositoryPath.PathSeparator);
                sql.Append("%' COLLATE Latin1_General_CI_AS");
                first = false;
            }

            if (nodeTypeIds != null)
            {
                if (!first)
                    sql.Append(" AND");
                sql.Append(" NodeTypeId");
                if (nodeTypeIds.Length == 1)
                    sql.Append(" = ").Append(nodeTypeIds[0]);
                else
                    sql.Append(" IN (").Append(String.Join(", ", nodeTypeIds)).Append(")");

                first = false;
            }

            if (properties != null)
            {
                foreach (var queryPropVal in properties)
                {
                    if (string.IsNullOrEmpty(queryPropVal.PropertyName))
                        continue;

                    var pt = PropertyType.GetByName(queryPropVal.PropertyName);
                    var pm = pt == null ? null : pt.GetDatabaseInfo();
                    var colName = pm == null ? GetNodeAttributeName(queryPropVal.PropertyName) : pm.ColumnName;
                    var dt = pt == null ? GetNodeAttributeType(queryPropVal.PropertyName) : pt.DataType;

                    if (!first)
                        sql.Append(" AND");

                    if (queryPropVal.Value != null)
                    {
                        switch (dt)
                        {
                            case DataType.DateTime:
                            case DataType.String:
                                var stringValue = queryPropVal.Value.ToString().Replace("'", "''");
                                switch (queryPropVal.QueryOperator)
                                {
                                    case Operator.Equal:
                                        sql.AppendFormat(" {0} = '{1}'", colName, stringValue);
                                        break;
                                    case Operator.Contains:
                                        sql.AppendFormat(" {0} LIKE '%{1}%'", colName, stringValue);
                                        break;
                                    case Operator.StartsWith:
                                        sql.AppendFormat(" {0} LIKE '{1}%'", colName, stringValue);
                                        break;
                                    case Operator.EndsWith:
                                        sql.AppendFormat(" {0} LIKE '%{1}'", colName, stringValue);
                                        break;
                                    case Operator.GreaterThan:
                                        sql.AppendFormat(" {0} > '{1}'", colName, stringValue);
                                        break;
                                    case Operator.GreaterThanOrEqual:
                                        sql.AppendFormat(" {0} >= '{1}'", colName, stringValue);
                                        break;
                                    case Operator.LessThan:
                                        sql.AppendFormat(" {0} < '{1}'", colName, stringValue);
                                        break;
                                    case Operator.LessThanOrEqual:
                                        sql.AppendFormat(" {0} <= '{1}'", colName, stringValue);
                                        break;
                                    case Operator.NotEqual:
                                        sql.AppendFormat(" {0} <> '{1}'", colName, stringValue);
                                        break;
                                    default:
                                        throw new InvalidOperationException(string.Format("Direct query not implemented (data type: {0}, operator: {1})", dt, queryPropVal.QueryOperator));
                                }
                                break;
                            case DataType.Int:
                            case DataType.Currency:
                                switch (queryPropVal.QueryOperator)
                                {
                                    case Operator.Equal:
                                        sql.AppendFormat(" {0} = {1}", colName, queryPropVal.Value);
                                        break;
                                    case Operator.GreaterThan:
                                        sql.AppendFormat(" {0} > {1}", colName, queryPropVal.Value);
                                        break;
                                    case Operator.GreaterThanOrEqual:
                                        sql.AppendFormat(" {0} >= {1}", colName, queryPropVal.Value);
                                        break;
                                    case Operator.LessThan:
                                        sql.AppendFormat(" {0} < {1}", colName, queryPropVal.Value);
                                        break;
                                    case Operator.LessThanOrEqual:
                                        sql.AppendFormat(" {0} <= {1}", colName, queryPropVal.Value);
                                        break;
                                    case Operator.NotEqual:
                                        sql.AppendFormat(" {0} <> {1}", colName, queryPropVal.Value);
                                        break;
                                    default:
                                        throw new InvalidOperationException(string.Format("Direct query not implemented (data type: {0}, operator: {1})", dt, queryPropVal.QueryOperator));
                                }
                                break;
                            default:
                                throw new NotSupportedException("Not supported direct query dataType: " + dt);
                        }
                    }
                    else
                    {
                        sql.Append(" IS NULL");
                    }
                }
            }

            if (orderByPath)
                sql.AppendLine().Append("ORDER BY Path");

            var cmd = new SqlProcedure { CommandText = sql.ToString(), CommandType = CommandType.Text };
            SqlDataReader reader = null;
            var result = new List<int>();
            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(reader.GetSafeInt32(0));
                return result;
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
                cmd.Dispose();
            }
        }
        protected internal override IEnumerable<int> QueryNodesByReferenceAndType(string referenceName, int referredNodeId, int[] allowedTypeIds)
        {
            if (referenceName == null)
                throw new ArgumentNullException("referenceName");
            if (referenceName.Length == 0)
                throw new ArgumentException("Argument referenceName cannot be empty.", "referenceName");
            var referenceProperty = ActiveSchema.PropertyTypes[referenceName];
            if (referenceProperty == null)
                throw new ArgumentException("PropertyType is not found: " + referenceName, "referenceName");
            var referencePropertyId = referenceProperty.Id;

            string sql;
            if (allowedTypeIds == null || allowedTypeIds.Length == 0)
            {
                sql = @"SELECT V.NodeId FROM ReferenceProperties R
	JOIN Versions V ON R.VersionId = V.VersionId
	JOIN Nodes N ON V.VersionId = N.LastMinorVersionId
WHERE R.PropertyTypeId = @PropertyTypeId AND R.ReferredNodeId = @ReferredNodeId";
            }
            else
            {
                sql = String.Format(@"SELECT N.NodeId FROM ReferenceProperties R
	JOIN Versions V ON R.VersionId = V.VersionId
	JOIN Nodes N ON V.VersionId = N.LastMinorVersionId
WHERE R.PropertyTypeId = @PropertyTypeId AND R.ReferredNodeId = @ReferredNodeId AND N.NodeTypeId IN ({0})", String.Join(", ", allowedTypeIds));
            }

            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = referencePropertyId;
                cmd.Parameters.Add("@ReferredNodeId", SqlDbType.Int).Value = referredNodeId;
                var result = new List<int>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        result.Add(reader.GetSafeInt32(0));
                    return result;
                }
            }
        }

        private static string GetNodeAttributeName(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException("propertyName");

            switch (propertyName)
            {
                case "Id":
                    return "NodeId";
                case "ParentId":
                case "Parent":
                    return "ParentNodeId";
                case "Locked":
                    return "Locked";
                case "LockedById":
                case "LockedBy":
                    return "LockedById";
                case "MajorVersion":
                    return "MajorNumber";
                case "MinorVersion":
                    return "MinorNumber";
                case "CreatedById":
                case "CreatedBy":
                    return "CreatedById";
                case "ModifiedById":
                case "ModifiedBy":
                    return "ModifiedById";
                default:
                    return propertyName;
            }
        }
        private static DataType GetNodeAttributeType(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException("propertyName");

            switch (propertyName)
            {
                case "Id":
                case "IsDeleted":
                case "IsInherited":
                case "ParentId":
                case "Parent":
                case "Index":
                case "Locked":
                case "LockedById":
                case "LockedBy":
                case "LockType":
                case "LockTimeout":
                case "MajorVersion":
                case "MinorVersion":
                case "CreatedById":
                case "CreatedBy":
                case "ModifiedById":
                case "ModifiedBy":
                case "IsSystem":
                case "ClosestSecurityNodeId":
                case "SavingState":
                    return DataType.Int;
                case "Name":
                case "Path":
                case "ETag":
                case "LockToken":
                    return DataType.String;
                case "LockDate":
                case "LastLockUpdate":
                case "CreationDate":
                case "ModificationDate":
                    return DataType.DateTime;
                default:
                    return DataType.String;
            }
        }

        protected internal override int InstanceCount(int[] nodeTypeIds)
        {
            var sql = new StringBuilder("SELECT COUNT(*) FROM Nodes WHERE NodeTypeId");
            if (nodeTypeIds.Length == 1)
                sql.Append(" = ").Append(nodeTypeIds[0]);
            else
                sql.Append(" IN (").Append(String.Join(", ", nodeTypeIds)).Append(")");

            var cmd = new SqlProcedure { CommandText = sql.ToString(), CommandType = CommandType.Text }; ;
            try
            {
                var count = (int)cmd.ExecuteScalar();
                return count;
            }
            finally
            {
                cmd.Dispose();
            }

        }

        //////////////////////////////////////// Node Query ////////////////////////////////////////

        protected internal override VersionNumber[] GetVersionNumbers(int nodeId)
        {
            List<VersionNumber> versions = new List<VersionNumber>();
            SqlProcedure cmd = null;
            SqlDataReader reader = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_VersionNumbers_GetByNodeId" };
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeId;
                reader = cmd.ExecuteReader();

                int majorNumberIndex = reader.GetOrdinal("MajorNumber");
                int minorNumberIndex = reader.GetOrdinal("MinorNumber");
                int statusIndex = reader.GetOrdinal("Status");

                while (reader.Read())
                {
                    versions.Add(new VersionNumber(
                        reader.GetInt16(majorNumberIndex),
                        reader.GetInt16(minorNumberIndex),
                        (VersionStatus)reader.GetInt16(statusIndex)));
                }
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();

                cmd.Dispose();
            }
            return versions.ToArray();
        }

        protected internal override VersionNumber[] GetVersionNumbers(string path)
        {
            List<VersionNumber> versions = new List<VersionNumber>();
            SqlProcedure cmd = null;
            SqlDataReader reader = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_VersionNumbers_GetByPath" };
                cmd.Parameters.Add("@Path", SqlDbType.NVarChar, 450).Value = path;
                reader = cmd.ExecuteReader();

                int majorNumberIndex = reader.GetOrdinal("MajorNumber");
                int minorNumberIndex = reader.GetOrdinal("MinorNumber");
                int statusIndex = reader.GetOrdinal("Status");

                while (reader.Read())
                {
                    versions.Add(new VersionNumber(
                        reader.GetInt32(majorNumberIndex),
                        reader.GetInt32(minorNumberIndex),
                        (VersionStatus)reader.GetInt32(statusIndex)));
                }
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();

                cmd.Dispose();
            }
            return versions.ToArray();
        }

        //protected internal override INodeQueryCompiler CreateNodeQueryCompiler()
        //{
        //    return new SqlCompiler();
        //}
        //protected internal override List<NodeToken> ExecuteQuery(NodeQuery query)
        //{
        //    List<NodeToken> result = new List<NodeToken>();
        //    SqlCompiler compiler = new SqlCompiler();

        //    NodeQueryParameter[] parameters;
        //    string compiledCommandText = compiler.Compile(query, out parameters);

        //    SqlProcedure command = null;
        //    SqlDataReader reader = null;
        //    try
        //    {
        //        command = new SqlProcedure { CommandText = compiledCommandText };
        //        command.CommandType = CommandType.Text;
        //        foreach (var parameter in parameters)
        //            command.Parameters.Add(new SqlParameter(parameter.Name, parameter.Value));

        //        reader = command.ExecuteReader();

        //        ReadNodeTokens(reader, result);
        //    }
        //    finally
        //    {
        //        if (reader != null && !reader.IsClosed)
        //            reader.Close();

        //        command.Dispose();
        //    }

        //    return result;
        //}

        protected internal override void LoadNodes(Dictionary<int, NodeBuilder> buildersByVersionId)
        {
            List<string> versionInfo = new List<string>();
            versionInfo.Add(String.Concat("VersionsId[count: ", buildersByVersionId.Count, "]"));

            if (buildersByVersionId.Keys.Count > 20)
            {
                versionInfo.AddRange(buildersByVersionId.Keys.Take(20).Select(x => x.ToString()));
                versionInfo.Add("...");
            }
            else
                versionInfo.AddRange(buildersByVersionId.Keys.Select(x => x.ToString()).ToArray());
            var operationTitle = String.Join(", ", versionInfo.ToArray());

            using (var traceOperation = Logger.TraceOperation("SqlProvider.LoadNodes" + operationTitle))
            {
                var builders = buildersByVersionId; // Shortcut
                SqlProcedure cmd = null;
                SqlDataReader reader = null;
                try
                {
                    cmd = new SqlProcedure { CommandText = "proc_Node_LoadData_Batch" };
                    string xmlIds = CreateIdXmlForNodeInfoBatchLoad(builders);
                    cmd.Parameters.Add("@IdsInXml", SqlDbType.Xml).Value = xmlIds;
                    reader = cmd.ExecuteReader();

                    //-- #1: FlatProperties
                    //SELECT * FROM FlatProperties
                    //    WHERE VersionId IN (select id from @versionids)
                    var versionIdIndex = reader.GetOrdinal("VersionId");
                    var pageIndex = reader.GetOrdinal("Page");

                    while (reader.Read())
                    {
                        int versionId = reader.GetInt32(versionIdIndex);
                        int page = reader.GetInt32(pageIndex);
                        NodeBuilder builder = builders[versionId];
                        foreach (PropertyType pt in builder.Token.AllPropertyTypes)
                        {
                            string mapping = PropertyMap.GetValidMapping(page, pt);
                            if (mapping.Length != 0)
                            {
                                // Mapped property appears in the given page
                                object val = reader[mapping];
                                if (val is DateTime)
                                {
                                    val = DateTime.SpecifyKind((DateTime)val, DateTimeKind.Utc);
                                }

                                builder.AddDynamicProperty(pt, (val == DBNull.Value) ? null : val);
                            }
                        }
                    }

                    reader.NextResult();


                    //-- #2: BinaryProperties
                    //SELECT BinaryPropertyId, VersionId, PropertyTypeId, ContentType, FileNameWithoutExtension,
                    //    Extension, [Size], [Checksum], NULL AS Stream, 0 AS Loaded
                    //FROM dbo.BinaryProperties
                    //WHERE PropertyTypeId IN (select id from @binids) AND VersionId IN (select id from @versionids)
                    var binaryPropertyIdIndex = reader.GetOrdinal("BinaryPropertyId");
                    versionIdIndex = reader.GetOrdinal("VersionId");
                    var checksumPropertyIndex = reader.GetOrdinal("Checksum");
                    var propertyTypeIdIndex = reader.GetOrdinal("PropertyTypeId");
                    var contentTypeIndex = reader.GetOrdinal("ContentType");
                    var fileNameWithoutExtensionIndex = reader.GetOrdinal("FileNameWithoutExtension");
                    var extensionIndex = reader.GetOrdinal("Extension");
                    var sizeIndex = reader.GetOrdinal("Size");
                    var timestampIndex = reader.GetOrdinal("Timestamp");

                    while (reader.Read())
                    {
                        string ext = reader.GetString(extensionIndex);
                        if (ext.Length != 0)
                            ext = ext.Remove(0, 1); // Remove dot from the start if extension is not empty

                        string fn = reader.GetSafeString(fileNameWithoutExtensionIndex); // reader.IsDBNull(fileNameWithoutExtensionIndex) ? null : reader.GetString(fileNameWithoutExtensionIndex);

                        var x = new BinaryDataValue
                        {
                            Id = reader.GetInt32(binaryPropertyIdIndex),
                            Checksum = reader.GetSafeString(checksumPropertyIndex), //reader.IsDBNull(checksumPropertyIndex) ? null : reader.GetString(checksumPropertyIndex),
                            FileName = new BinaryFileName(fn, ext),
                            ContentType = reader.GetString(contentTypeIndex),
                            Size = reader.GetInt64(sizeIndex),
                            Timestamp = DataProvider.GetLongFromBytes((byte[])reader.GetValue(timestampIndex))
                        };

                        var versionId = reader.GetInt32(versionIdIndex);
                        var propertyTypeId = reader.GetInt32(propertyTypeIdIndex);
                        builders[versionId].AddDynamicProperty(propertyTypeId, x);
                    }

                    reader.NextResult();


                    //-- #3: ReferencePropertyInfo + Referred NodeToken
                    //SELECT VersionId, PropertyTypeId, ReferredNodeId
                    //FROM dbo.ReferenceProperties ref
                    //WHERE ref.VersionId IN (select id from @versionids)
                    versionIdIndex = reader.GetOrdinal("VersionId");
                    propertyTypeIdIndex = reader.GetOrdinal("PropertyTypeId");
                    var nodeIdIndex = reader.GetOrdinal("ReferredNodeId");

                    //-- Collect references to Dictionary<versionId, Dictionary<propertyTypeId, List<referredNodeId>>>
                    var referenceCollector = new Dictionary<int, Dictionary<int, List<int>>>();
                    while (reader.Read())
                    {
                        var versionId = reader.GetInt32(versionIdIndex);
                        var propertyTypeId = reader.GetInt32(propertyTypeIdIndex);
                        var referredNodeId = reader.GetInt32(nodeIdIndex);

                        if (!referenceCollector.ContainsKey(versionId))
                            referenceCollector.Add(versionId, new Dictionary<int, List<int>>());
                        var referenceCollectorPerVersion = referenceCollector[versionId];
                        if (!referenceCollectorPerVersion.ContainsKey(propertyTypeId))
                            referenceCollectorPerVersion.Add(propertyTypeId, new List<int>());
                        referenceCollectorPerVersion[propertyTypeId].Add(referredNodeId);
                    }
                    //-- Set references to NodeData
                    foreach (var versionId in referenceCollector.Keys)
                    {
                        var referenceCollectorPerVersion = referenceCollector[versionId];
                        foreach (var propertyTypeId in referenceCollectorPerVersion.Keys)
                            builders[versionId].AddDynamicProperty(propertyTypeId, referenceCollectorPerVersion[propertyTypeId]);
                    }

                    reader.NextResult();


                    //-- #4: TextPropertyInfo (NText:Lazy, NVarchar(4000):loaded)
                    //SELECT VersionId, PropertyTypeId, NULL AS Value, 0 AS Loaded
                    //FROM dbo.TextPropertiesNText
                    //WHERE VersionId IN (select id from @versionids)
                    //UNION ALL
                    //SELECT VersionId, PropertyTypeId, Value, 1 AS Loaded
                    //FROM dbo.TextPropertiesNVarchar
                    //WHERE VersionId IN (select id from @versionids)
                    versionIdIndex = reader.GetOrdinal("VersionID");
                    propertyTypeIdIndex = reader.GetOrdinal("PropertyTypeId");
                    var valueIndex = reader.GetOrdinal("Value");
                    var loadedIndex = reader.GetOrdinal("Loaded");

                    while (reader.Read())
                    {
                        int versionId = reader.GetInt32(versionIdIndex);
                        int propertyTypeId = reader.GetInt32(propertyTypeIdIndex);
                        string value = reader.GetSafeString(valueIndex); // (reader[valueIndex] == DBNull.Value) ? null : reader.GetString(valueIndex);
                        bool loaded = Convert.ToBoolean(reader.GetInt32(loadedIndex));

                        if (loaded)
                            builders[versionId].AddDynamicProperty(propertyTypeId, value);
                    }

                    reader.NextResult();


                    //-- #5: BaseData
                    //SELECT N.NodeId, N.NodeTypeId, N.ContentListTypeId, N.ContentListId, N.CreatingInProgress, N.IsDeleted, N.IsInherited, 
                    //    N.ParentNodeId, N.[Name], N.DisplayName, N.[Path], N.[Index], N.Locked, N.LockedById, 
                    //    N.ETag, N.LockType, N.LockTimeout, N.LockDate, N.LockToken, N.LastLockUpdate,
                    //    N.CreationDate AS NodeCreationDate, N.CreatedById AS NodeCreatedById, 
                    //    N.ModificationDate AS NodeModificationDate, N.ModifiedById AS NodeModifiedById,
                    //    N.IsSystem, ClosestSecurityNodeId,
                    //    N.IsSystem, ClosestSecurityNodeId,
                    //    N.SavingState, V.ChangedData,
                    //    V.VersionId, V.MajorNumber, V.MinorNumber, V.CreationDate, V.CreatedById, 
                    //    V.ModificationDate, V.ModifiedById, V.[Status],
                    //    V.Timestamp AS VersionTimestamp
                    //FROM dbo.Nodes AS N 
                    //    INNER JOIN dbo.Versions AS V ON N.NodeId = V.NodeId
                    //WHERE V.VersionId IN (select id from @versionids)
                    nodeIdIndex = reader.GetOrdinal("NodeId");
                    var nodeTypeIdIndex = reader.GetOrdinal("NodeTypeId");
                    var contentListTypeIdIndex = reader.GetOrdinal("ContentListTypeId");
                    var contentListIdIndex = reader.GetOrdinal("ContentListId");
                    var creatingInProgressIndex = reader.GetOrdinal("CreatingInProgress");
                    var isDeletedIndex = reader.GetOrdinal("IsDeleted");
                    var isInheritedIndex = reader.GetOrdinal("IsInherited");
                    var parentNodeIdIndex = reader.GetOrdinal("ParentNodeId");
                    var nameIndex = reader.GetOrdinal("Name");
                    var displayNameIndex = reader.GetOrdinal("DisplayName");
                    var pathIndex = reader.GetOrdinal("Path");
                    var indexIndex = reader.GetOrdinal("Index");
                    var lockedIndex = reader.GetOrdinal("Locked");
                    var lockedByIdIndex = reader.GetOrdinal("LockedById");
                    var eTagIndex = reader.GetOrdinal("ETag");
                    var lockTypeIndex = reader.GetOrdinal("LockType");
                    var lockTimeoutIndex = reader.GetOrdinal("LockTimeout");
                    var lockDateIndex = reader.GetOrdinal("LockDate");
                    var lockTokenIndex = reader.GetOrdinal("LockToken");
                    var lastLockUpdateIndex = reader.GetOrdinal("LastLockUpdate");
                    var nodeCreationDateIndex = reader.GetOrdinal("NodeCreationDate");
                    var nodeCreatedByIdIndex = reader.GetOrdinal("NodeCreatedById");
                    var nodeModificationDateIndex = reader.GetOrdinal("NodeModificationDate");
                    var nodeModifiedByIdIndex = reader.GetOrdinal("NodeModifiedById");
                    var isSystemIndex = reader.GetOrdinal("IsSystem");
                    var closestSecurityNodeIdIndex = reader.GetOrdinal("ClosestSecurityNodeId");
                    var savingStateIndex = reader.GetOrdinal("SavingState");
                    var changedDataIndex = reader.GetOrdinal("ChangedData");
                    var nodeTimestampIndex = reader.GetOrdinal("NodeTimestamp");

                    versionIdIndex = reader.GetOrdinal("VersionId");
                    var majorNumberIndex = reader.GetOrdinal("MajorNumber");
                    var minorNumberIndex = reader.GetOrdinal("MinorNumber");
                    var versionCreationDateIndex = reader.GetOrdinal("CreationDate");
                    var versionCreatedByIdIndex = reader.GetOrdinal("CreatedById");
                    var versionModificationDateIndex = reader.GetOrdinal("ModificationDate");
                    var versionModifiedByIdIndex = reader.GetOrdinal("ModifiedById");
                    var status = reader.GetOrdinal("Status");
                    var versionTimestampIndex = reader.GetOrdinal("VersionTimestamp");

                    while (reader.Read())
                    {
                        int versionId = reader.GetInt32(versionIdIndex);

                        VersionNumber versionNumber = new VersionNumber(
                            reader.GetInt16(majorNumberIndex),
                            reader.GetInt16(minorNumberIndex),
                            (VersionStatus)reader.GetInt16(status));

                        builders[versionId].SetCoreAttributes(
                            reader.GetInt32(nodeIdIndex),
                            reader.GetInt32(nodeTypeIdIndex),
                            TypeConverter.ToInt32(reader.GetValue(contentListIdIndex)),
                            TypeConverter.ToInt32(reader.GetValue(contentListTypeIdIndex)),
                            Convert.ToBoolean(reader.GetByte(creatingInProgressIndex)),
                            Convert.ToBoolean(reader.GetByte(isDeletedIndex)),
                            Convert.ToBoolean(reader.GetByte(isInheritedIndex)),
                            reader.GetSafeInt32(parentNodeIdIndex), // reader.GetValue(parentNodeIdIndex) == DBNull.Value ? 0 : reader.GetInt32(parentNodeIdIndex), //parent,
                            reader.GetString(nameIndex),
                            reader.GetSafeString(displayNameIndex),
                            reader.GetString(pathIndex),
                            reader.GetInt32(indexIndex),
                            Convert.ToBoolean(reader.GetByte(lockedIndex)),
                            reader.GetSafeInt32(lockedByIdIndex), // reader.GetValue(lockedByIdIndex) == DBNull.Value ? 0 : reader.GetInt32(lockedByIdIndex),
                            reader.GetString(eTagIndex),
                            reader.GetInt32(lockTypeIndex),
                            reader.GetInt32(lockTimeoutIndex),
                            reader.GetDateTimeUtc(lockDateIndex),
                            reader.GetString(lockTokenIndex),
                            reader.GetDateTimeUtc(lastLockUpdateIndex),
                            versionId,
                            versionNumber,
                            reader.GetDateTimeUtc(versionCreationDateIndex),
                            reader.GetInt32(versionCreatedByIdIndex),
                            reader.GetDateTimeUtc(versionModificationDateIndex),
                            reader.GetInt32(versionModifiedByIdIndex),
                            reader.GetSafeBoolean(isSystemIndex),
                            reader.GetSafeInt32(closestSecurityNodeIdIndex),
                            reader.GetSavingState(savingStateIndex),
                            reader.GetChangedData(changedDataIndex),
                            reader.GetDateTimeUtc(nodeCreationDateIndex),
                            reader.GetInt32(nodeCreatedByIdIndex),
                            reader.GetDateTimeUtc(nodeModificationDateIndex),
                            reader.GetInt32(nodeModifiedByIdIndex),
                            GetLongFromBytes((byte[])reader.GetValue(nodeTimestampIndex)),
                            GetLongFromBytes((byte[])reader.GetValue(versionTimestampIndex))
                            );
                    }
                    foreach (var builder in builders.Values)
                        builder.Finish();
                }
                finally
                {
                    if (reader != null && !reader.IsClosed)
                        reader.Close();

                    cmd.Dispose();
                }
                traceOperation.IsSuccessful = true;
            }
        }

        protected internal override bool IsCacheableText(string text)
        {
            if (text == null)
                return false;
            return text.Length < TextAlternationSizeLimit;
        }

        protected internal override string LoadTextPropertyValue(int versionId, int propertyTypeId)
        {
            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_TextProperty_LoadValue" };
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId;
                var s = (string)cmd.ExecuteScalar();
                return s;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        protected internal override BinaryDataValue LoadBinaryPropertyValue(int versionId, int propertyTypeId)
        {
            BinaryDataValue result = null;
            using (var traceOperation = Logger.TraceOperation("SqlProvider.LoadBinaryPropertyValue"))
            {
                using (var cmd = new SqlProcedure { CommandText = "proc_BinaryProperty_LoadValue" })
                {
                    cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                    cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId;
                    using (var reader = cmd.ExecuteReader())
                    {
                        //-- #2: BinaryProperties
                        //SELECT BinaryPropertyId, VersionId, PropertyTypeId, ContentType, FileNameWithoutExtension,
                        //    Extension, [Size], [Checksum], CreationDate, [Timestamp]
                        //FROM dbo.BinaryProperties
                        //WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId AND Staging IS NULL
                        var binaryPropertyIdIndex = reader.GetOrdinal("BinaryPropertyId");
                        var contentTypeIndex = reader.GetOrdinal("ContentType");
                        var fileNameWithoutExtensionIndex = reader.GetOrdinal("FileNameWithoutExtension");
                        var extensionIndex = reader.GetOrdinal("Extension");
                        var sizeIndex = reader.GetOrdinal("Size");
                        var checksumPropertyIndex = reader.GetOrdinal("Checksum");
                        var timestampIndex = reader.GetOrdinal("Timestamp");

                        if (reader.Read())
                        {
                            string ext = reader.GetString(extensionIndex);
                            if (ext.Length != 0)
                                ext = ext.Remove(0, 1); // Remove dot from the start if extension is not empty
                            string fn = reader.GetSafeString(fileNameWithoutExtensionIndex);

                            result = new BinaryDataValue
                            {
                                Id = reader.GetInt32(binaryPropertyIdIndex),
                                Checksum = reader.GetSafeString(checksumPropertyIndex),
                                FileName = new BinaryFileName(fn, ext),
                                ContentType = reader.GetString(contentTypeIndex),
                                Size = reader.GetInt64(sizeIndex),
                                Timestamp = DataProvider.GetLongFromBytes((byte[])reader.GetValue(timestampIndex))
                            };
                        }
                    }
                }
                traceOperation.IsSuccessful = true;
            }
            return result;
        }

        protected internal override Stream LoadStream(int versionId, int propertyTypeId)
        {
            // Retrieve binary pointer for chunk reading
            int length = 0;
            int pointer = 0;

            var path = string.Empty;
            byte[] transactionContext = null;

            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_BinaryProperty_GetPointer" };
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId;
                SqlParameter pointerOutParam = cmd.Parameters.Add("@Id", SqlDbType.Int);
                pointerOutParam.Direction = ParameterDirection.Output;
                SqlParameter lengthOutParam = cmd.Parameters.Add("@Length", SqlDbType.Int);
                lengthOutParam.Direction = ParameterDirection.Output;

                SqlParameter transactionOutParam = null;
                SqlParameter pathOutParam = null;

                //add Filestream parameters if needed
                if (RepositoryConfiguration.FileStreamEnabled)
                {
                    transactionOutParam = cmd.Parameters.Add("@TransactionContext", SqlDbType.Binary, 16);
                    transactionOutParam.Direction = ParameterDirection.Output;
                    
                    pathOutParam = cmd.Parameters.Add("@FilePath", SqlDbType.NVarChar, 4000);
                    pathOutParam.Direction = ParameterDirection.Output;
                }

                cmd.ExecuteNonQuery();

                if (lengthOutParam.Value != DBNull.Value)
                    length = (int)lengthOutParam.Value;
                if (pointerOutParam.Value != DBNull.Value)
                    pointer = Convert.ToInt32(pointerOutParam.Value);

                //read Filestream parameters if needed
                if (RepositoryConfiguration.FileStreamEnabled)
                {
                    transactionContext = transactionOutParam != null && transactionOutParam.Value != DBNull.Value
                                             ? (byte[]) transactionOutParam.Value
                                             : null;

                    path = pathOutParam != null && pathOutParam.Value != DBNull.Value
                                             ? transactionOutParam.Value as string
                                             : null;
                }
            }
            finally
            {
                cmd.Dispose();
            }

            if (pointer == 0)
                return null;

            //read binary using SqlFileStream if possible
            if (RepositoryConfiguration.FileStreamEnabled && !string.IsNullOrEmpty(path))
            {
                //return LoadFileStream(path, transactionContext);
                return new ContentRepository.Storage.Data.SqlFileStream(length, pointer, transactionContext == null 
                    ? null 
                    : new FileStreamData {Path = path, TransactionContext = transactionContext});
            }

            return new RepositoryStream(length, pointer);
        }

        protected internal override IEnumerable<int> GetChildrenIdentfiers(int nodeId)
        {
            using (var cmd = new SqlProcedure { CommandText = "SELECT NodeId FROM Nodes WHERE ParentNodeId = @ParentNodeId" })
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@ParentNodeId", SqlDbType.Int).Value = nodeId;
                using (var reader = cmd.ExecuteReader())
                {
                    var ids = new List<int>();
                    while (reader.Read())
                        ids.Add(reader.GetSafeInt32(0));

                    return ids;
                }
            }
        }

        //////////////////////////////////////// Operations ////////////////////////////////////////

        protected internal override IEnumerable<NodeType> LoadChildTypesToAllow(int sourceNodeId)
        {
            var result = new List<NodeType>();
            using (var cmd = new SqlProcedure { CommandText = "proc_LoadChildTypesToAllow" })
            {
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = sourceNodeId;
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var name = (string)reader[0];
                    var nt = ActiveSchema.NodeTypes[name];
                    if (nt != null)
                        result.Add(nt);
                }
            }
            return result;
        }
        protected internal override DataOperationResult MoveNodeTree(int sourceNodeId, int targetNodeId, long sourceTimestamp = 0, long targetTimestamp = 0)
        {
            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Node_Move" };
                cmd.Parameters.Add("@SourceNodeId", SqlDbType.Int).Value = sourceNodeId;
                cmd.Parameters.Add("@TargetNodeId", SqlDbType.Int).Value = targetNodeId;
                cmd.Parameters.Add("@SourceTimestamp", SqlDbType.Timestamp).Value = sourceTimestamp == 0 ? DBNull.Value : (object)GetBytesFromLong(sourceTimestamp);
                cmd.Parameters.Add("@TargetTimestamp", SqlDbType.Timestamp).Value = targetTimestamp == 0 ? DBNull.Value : (object)GetBytesFromLong(targetTimestamp);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException e) //logged //rethrow
            {
                if (e.Message.StartsWith("Source node is out of date"))
                    throw new NodeIsOutOfDateException(e.Message, e);

                if (e.Message.StartsWith("String or binary data would be truncated"))
                    return DataOperationResult.DataTooLong;

                switch (e.State)
                {
                    case 1: //'Invalid operation: moving a contentList / a subtree that contains a contentList under an another contentList.'
                        Logger.WriteException(e);
                        return DataOperationResult.Move_TargetContainsSameName;
                    case 2:
                        return DataOperationResult.Move_NodeWithContentListContentUnderContentList;
                    default:
                        throw;
                }
            }
            finally
            {
                cmd.Dispose();
            }
            return 0;
        }

        protected internal override DataOperationResult DeleteNodeTree(int nodeId)
        {
            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Node_Delete" };
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeId;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }
            return DataOperationResult.Successful;
        }

        protected internal override DataOperationResult DeleteNodeTreePsychical(int nodeId, long timestamp = 0)
        {
            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Node_DeletePhysical" };
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeId;
                cmd.Parameters.Add("@Timestamp", SqlDbType.Timestamp).Value = timestamp == 0 ? DBNull.Value : (object)GetBytesFromLong(timestamp);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException e) //rethrow
            {
                if (e.Message.StartsWith("Node is out of date"))
                    throw new NodeIsOutOfDateException(e.Message, e);
                throw;
            }
            finally
            {
                cmd.Dispose();
            }
            return DataOperationResult.Successful;
        }

        protected internal override void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            SqlProcedure cmd = null;
            SqlDataReader reader = null;
            lastMajorVersionId = 0;
            lastMinorVersionId = 0;

            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Node_DeleteVersion" };
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;

                reader = cmd.ExecuteReader();

                //refresh timestamp value from the db
                while (reader.Read())
                {
                    nodeData.NodeTimestamp = DataProvider.GetLongFromBytes((byte[])reader[0]);
                    lastMajorVersionId = reader.GetSafeInt32(1);
                    lastMinorVersionId = reader.GetSafeInt32(2);
                }
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
                cmd.Dispose();
            }
        }

        protected internal override bool HasChild(int nodeId)
        {
            SqlProcedure cmd = null;
            int result;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Node_HasChild" };
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeId;
                result = (int)cmd.ExecuteScalar();
            }
            finally
            {
                cmd.Dispose();
            }

            if (result == -1)
                throw new ApplicationException();

            return result > 0;
        }

        protected internal override List<ContentListType> GetContentListTypesInTree(string path)
        {
            SqlProcedure cmd = null;
            SqlDataReader reader = null;
            var result = new List<ContentListType>();

            string commandString = @"SELECT ContentListTypeId FROM Nodes WHERE ContentListId IS NULL AND ContentListTypeId IS NOT NULL AND Path LIKE @Path + '/%' COLLATE Latin1_General_CI_AS";
            cmd = new SqlProcedure { CommandText = commandString };
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add("@Path", SqlDbType.NVarChar, 450).Value = path;
            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var t = NodeTypeManager.Current.ContentListTypes.GetItemById(id);
                    result.Add(t);
                }
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();
                if (cmd != null)
                    cmd.Dispose();
            }
            return result;
        }

        protected internal override bool IsFilestreamEnabled()
        {
            bool fsEnabled;

            using (var pro = CreateDataProcedure("SELECT COUNT(name) FROM sys.columns WHERE Name = N'FileStream' and Object_ID = Object_ID(N'BinaryProperties')"))
            {
                pro.CommandType = CommandType.Text;

                try
                {
                    fsEnabled = Convert.ToInt32(pro.ExecuteScalar()) > 0;
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);

                    fsEnabled = false;
                }
            }

            return fsEnabled;
        }

        //////////////////////////////////////// Chunk upload ////////////////////////////////////////

        protected internal override string StartChunk(int versionId, int propertyTypeId)
        {
            var isLocalTransaction = !TransactionScope.IsActive;
            if (isLocalTransaction)
                TransactionScope.Begin();

            try
            {
                using (var cmd = new SqlProcedure() { CommandText = INSERT_STAGING_BINARY, CommandType = CommandType.Text })
                {
                    cmd.Parameters.Add(new SqlParameter("@VersionId", SqlDbType.Int)).Value = versionId;
                    cmd.Parameters.Add(new SqlParameter("@PropertyTypeId", SqlDbType.Int)).Value = propertyTypeId;

                    return Convert.ToInt32(cmd.ExecuteScalar()).ToString();
                }
            }
            catch (Exception ex)
            {
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Rollback();

                throw new DataException("Error during saving binary chunk to SQL Server.", ex);
            }
            finally
            {
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Commit();
            }
        }

        protected internal override void WriteChunk(int versionId, string token, byte[] buffer, long offset, long fullSize)
        {
            var binaryPropertyId = Convert.ToInt32(token);

            if (RepositoryConfiguration.FileStreamEnabled && fullSize > RepositoryConfiguration.MinimumSizeForFileStreamInBytes)
                WriteChunkToFilestream(versionId, binaryPropertyId, buffer, offset);
            else
                WriteChunkToSql(versionId, binaryPropertyId, buffer, offset);
        }

        private void WriteChunkToFilestream(int versionId, int binaryPropertyId, byte[] buffer, long offset)
        {
            var isLocalTransaction = false;

            if (!TransactionScope.IsActive)
            {
                //Start a new transaction here to serve the needs of the SqlFileStream type.
                TransactionScope.Begin();
                isLocalTransaction = true;
            }

            try
            {
                // Load the pointer that will be used to access the file system entry. The FileStream
                // column should be initialized with an empty value, because NULL value does not
                // have a file system pointer.
                var fsd = LoadFileStreamData(binaryPropertyId, true, versionId);
                if (fsd == null)
                    throw new InvalidOperationException("Binary row not found. BinaryPropertyId: " + binaryPropertyId);

                //Write data using SqlFileStream
                using (var fs = new System.Data.SqlTypes.SqlFileStream(fsd.Path, fsd.TransactionContext, FileAccess.ReadWrite, FileOptions.SequentialScan, 0))
                {
                    // if the current stream is smaller than the position where we want to write the bytes
                    if (fs.Length < offset)
                    {
                        // go to the end of the existing stream
                        fs.Seek(0, SeekOrigin.End);

                        // calculate the size of the gap (warning: fs.Length changes during the write below!)
                        var gapSize = offset - fs.Length;

                        // fill the gap with empty bytes (one-by-one, because this gap could be huge)
                        for (var i = 0; i < gapSize; i++)
                        {
                            fs.WriteByte(0x00);
                        }
                    }
                    else if (offset > 0)
                    {
                        // otherwise we will append to the end or overwrite existing bytes
                        fs.Seek(offset, SeekOrigin.Begin);
                    }

                    //write chunk to the file (no offset is needed here, the stream is already at the correct position)
                    fs.Write(buffer, 0, buffer.Length);
                }
            }
            catch(Exception ex)
            {
                //rollback the transaction if it was opened locally
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Rollback();

                throw new DataException("Error during saving binary chunk to filestream.", ex);
            }
            finally 
            {
                //commit the transaction if it was opened locally
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Commit();
            }
        }
        
        private void WriteChunkToSql(int versionId, int binaryPropertyId, byte[] buffer, long offset)
        {
            var isLocalTransaction = !TransactionScope.IsActive;
            if (isLocalTransaction)
                TransactionScope.Begin();

            try
            {
                // If Filestream is enabled but not used, we need to set it NULL 
                // when inserting the chunk to the regular Stream column
                var cmdText = RepositoryConfiguration.FileStreamEnabled
                    ? UPDATE_STREAM_WRITE_CHUNK_FS
                    : UPDATE_STREAM_WRITE_CHUNK;

                using (var cmd = new SqlProcedure() { CommandText = cmdText, CommandType = CommandType.Text })
                {
                    cmd.Parameters.Add(new SqlParameter("@VersionId", SqlDbType.Int)).Value = versionId;
                    cmd.Parameters.Add(new SqlParameter("@BinaryPropertyId", SqlDbType.Int)).Value = binaryPropertyId;
                    cmd.Parameters.Add(new SqlParameter("@Data", SqlDbType.VarBinary)).Value = buffer;
                    cmd.Parameters.Add(new SqlParameter("@Offset", SqlDbType.BigInt)).Value = offset;

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Rollback();

                throw new DataException("Error during saving binary chunk to SQL Server.", ex);
            }
            finally
            {
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Commit();
            }
        }

        protected internal override void CommitChunk(int versionId, int propertyTypeId, string token, long fullSize, BinaryDataValue source = null)
        {
            var binaryPropertyId = Convert.ToInt32(token);

            CommitChunkInternal(versionId, propertyTypeId, binaryPropertyId, fullSize, source);
        }

        private static void CommitChunkInternal(int versionId, int propertyTypeId, int binaryPropertyId, long fullSize, BinaryDataValue source = null)
        {
            //start a new transaction here if needed
            var isLocalTransaction = !TransactionScope.IsActive;
            if (isLocalTransaction)
                TransactionScope.Begin();

            try
            {
                //commit the process: set the final full size and checksum
                using (var cmd = new SqlProcedure { CommandText = COMMIT_CHUNK, CommandType = CommandType.Text })
                {
                    cmd.Parameters.Add("@BinaryPropertyId", SqlDbType.Int).Value = binaryPropertyId;
                    cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                    cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId;
                    cmd.Parameters.Add("@Size", SqlDbType.BigInt).Value = fullSize;
                    cmd.Parameters.Add("@Checksum", SqlDbType.VarChar, 200).Value = DBNull.Value; //!(string.IsNullOrEmpty(checksum)) ? (object)checksum : DBNull.Value;

                    cmd.Parameters.Add("@ContentType", SqlDbType.NVarChar, 50).Value = source != null ? source.ContentType : string.Empty;
                    cmd.Parameters.Add("@FileNameWithoutExtension", SqlDbType.NVarChar, 450).Value = source != null 
                        ? source.FileName.FileNameWithoutExtension == null 
                            ? DBNull.Value 
                            : (object)source.FileName.FileNameWithoutExtension
                        : DBNull.Value;

                    cmd.Parameters.Add("@Extension", SqlDbType.NVarChar, 50).Value = source != null ? SqlNodeWriter.ValidateExtension(source.FileName.Extension) : string.Empty;

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                //rollback the transaction if it was opened locally
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Rollback();

                throw new DataException("Error during committing binary chunk to file stream.", ex);
            }
            finally
            {
                //commit the transaction if it was opened locally
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Commit();
            }
        }

        internal static string CreateIdXmlForReferencePropertyUpdate(IEnumerable<int> values)
        {
            StringBuilder xmlBuilder = new StringBuilder(values == null ? 50 : 50 + values.Count() * 10);
            xmlBuilder.AppendLine("<Identifiers>");
            xmlBuilder.AppendLine("<ReferredNodeIds>");
            if (values != null)
                foreach (var value in values)
                    if (value > 0)
                        xmlBuilder.Append("<Id>").Append(value).AppendLine("</Id>");
            xmlBuilder.AppendLine("</ReferredNodeIds>");
            xmlBuilder.AppendLine("</Identifiers>");
            return xmlBuilder.ToString();
        }

        private static string CreateIdXmlForNodeInfoBatchLoad(Dictionary<int, NodeBuilder> builders)
        {
            StringBuilder xmlBuilder = new StringBuilder(500 + builders.Count * 20);

            xmlBuilder.AppendLine("<Identifiers>");
            xmlBuilder.AppendLine("  <VersionIds>");
            foreach (int versionId in builders.Keys)
                xmlBuilder.Append("    <Id>").Append(versionId).AppendLine("</Id>");
            xmlBuilder.AppendLine("  </VersionIds>");
            xmlBuilder.AppendLine("</Identifiers>");

            return xmlBuilder.ToString();
        }

        protected internal override long GetTreeSize(string path, bool includeChildren)
        {
            SqlProcedure cmd = null;
            long result;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Node_GetTreeSize" };
                cmd.Parameters.Add("@NodePath", SqlDbType.NVarChar, 450).Value = path;
                cmd.Parameters.Add("@IncludeChildren", SqlDbType.TinyInt).Value = includeChildren ? 1 : 0;

                var obj = cmd.ExecuteScalar();

                result = (obj == DBNull.Value) ? 0 : (long)obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }

            if (result == -1)
                throw new ApplicationException();

            return result;
        }

        protected override int NodeCount(string path)
        {
            var proc = new SqlProcedure();
            proc.CommandType = CommandType.Text;
            if (String.IsNullOrEmpty(path) || path == RepositoryPath.PathSeparator)
            {
                proc.CommandText = "SELECT COUNT(*) FROM Nodes";
            }
            else
            {
                proc.CommandText = "SELECT COUNT(*) FROM Nodes WHERE Path LIKE @Path + '/%' COLLATE Latin1_General_CI_AS";
                proc.Parameters.Add("@Path", SqlDbType.NVarChar, 450).Value = path;
            }
            return (int)proc.ExecuteScalar();
        }
        protected override int VersionCount(string path)
        {
            var proc = new SqlProcedure();
            proc.CommandType = CommandType.Text;
            if (String.IsNullOrEmpty(path) || path == RepositoryPath.PathSeparator)
            {
                proc.CommandText = "SELECT COUNT(*) FROM Versions V JOIN Nodes N ON N.NodeId = V.NodeId";
            }
            else
            {
                proc.CommandText = "SELECT COUNT(*) FROM Versions V JOIN Nodes N ON N.NodeId = V.NodeId WHERE N.Path LIKE @Path + '/%' COLLATE Latin1_General_CI_AS";
                proc.Parameters.Add("@Path", SqlDbType.NVarChar, 450).Value = path;
            }
            return (int)proc.ExecuteScalar();
        }

        //////////////////////////////////////// Security Methods ////////////////////////////////////////

        private const string SYSTEMPERMISSIONSTABLENAME = "SecurityEntries";
        private const string CUSTOMPERMISSIONSTABLENAME = "SecurityCustomEntries";

        protected internal override string GetPermissionLoaderScript()
        {
            return @"
                (SELECT 1 AS System, N.Path, N.CreatedById, N.ModifiedById, N.IsInherited, S.* FROM SecurityEntries S INNER JOIN Nodes N ON S.DefinedOnNodeId = N.NodeId)
                UNION ALL
                (SELECT 0 AS System, N.Path, N.CreatedById, N.ModifiedById, N.IsInherited, S.* FROM SecurityCustomEntries S INNER JOIN Nodes N ON S.DefinedOnNodeId = N.NodeId)
                ORDER BY N.Path

                SELECT * FROM SecurityMemberships ORDER BY UserId
                ";

        }

        protected internal override Dictionary<int, List<int>> LoadMemberships()
        {
            SqlProcedure cmd = null;
            var result = new Dictionary<int, List<int>>();
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Security_LoadMemberships" };
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int groupId = (int) reader["ContainerId"];
                        int userId = (int) reader["UserId"];
                        if (!result.ContainsKey(groupId))
                            result.Add(groupId, new List<int>());
                        result[groupId].Add(userId);
                    }
                }
            }
            finally
            {
                cmd.Dispose();
            }
            return result;
        }

        private static string GetPermissionCacheKey(int principalID, int nodeID, PermissionType type)
        {
            return string.Format(CultureInfo.InvariantCulture, "SN:NodePermissionCache:{0}:{1}:{2}", principalID, nodeID, type.Id); //@@ PermissionType.Id
        }

        private static string _setPermissionScript = @"
	IF NOT EXISTS (SELECT SecurityEntryId FROM dbo.{0} WHERE DefinedOnNodeId = @NodeId AND PrincipalId = @PrincipalId AND IsInheritable = @IsInheritable)
		INSERT INTO dbo.{0} (DefinedOnNodeId, PrincipalId, IsInheritable) VALUES (@NodeId, @PrincipalId, @IsInheritable)

	UPDATE dbo.{0} SET PermissionValue'{1}' = @PermissionValue WHERE DefinedOnNodeId = @NodeId AND PrincipalId = @PrincipalId AND IsInheritable = @IsInheritable

	DECLARE @SumPermissionValue int
	SELECT @SumPermissionValue = PermissionValue1 + PermissionValue2 + PermissionValue3 + PermissionValue4 + PermissionValue5 + PermissionValue6 + PermissionValue7 +
                                 PermissionValue8 + PermissionValue9 + PermissionValue10 + PermissionValue11 + PermissionValue12 + PermissionValue13 + PermissionValue14 +
                                 PermissionValue15 + PermissionValue16 + PermissionValue17 + PermissionValue18 + PermissionValue19 + PermissionValue20 + PermissionValue21 +
                                 PermissionValue22 + PermissionValue23 + PermissionValue24 + PermissionValue25 + PermissionValue26 + PermissionValue27 + PermissionValue28 +
                                 PermissionValue29 + PermissionValue30 + PermissionValue31 + PermissionValue32
	FROM dbo.{0}
	WHERE DefinedOnNodeId = @NodeId AND PrincipalId = @PrincipalId AND IsInheritable = @IsInheritable

	IF @SumPermissionValue = 0
		DELETE dbo.{0} WHERE DefinedOnNodeId = @NodeId AND PrincipalId = @PrincipalId AND IsInheritable = @IsInheritable
";
        [Obsolete("Not performant. Use SetPermission(SecurityEntry) instead.")]
        protected internal override void SetPermission(int principalId, int nodeId, PermissionType permissionType, bool isInheritable, PermissionValue permissionValue)
        {
            var tableName = permissionType.IsSystemPermission ? SYSTEMPERMISSIONSTABLENAME : CUSTOMPERMISSIONSTABLENAME;
            var sql = String.Format(_setPermissionScript, tableName, permissionType.IsSystemPermission ? permissionType.Id : permissionType.Id - PermissionType.NumberOfSystemPermissionTypes); //@@ PermissionType.Id

            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text };
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeId;
                cmd.Parameters.Add("@PrincipalId", SqlDbType.Int).Value = principalId;
                cmd.Parameters.Add("@IsInheritable", SqlDbType.TinyInt).Value = isInheritable ? (byte)1 : (byte)0;
                cmd.Parameters.Add("@PermissionValue", SqlDbType.TinyInt).Value = Convert.ToByte(permissionValue);
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }
        }

        protected internal override void SetPermission(SecurityEntry entry)
        {
            var allowBits = PermissionType.ConvertBitsIndexToId(entry.AllowBits);
            var denyBits = PermissionType.ConvertBitsIndexToId(entry.DenyBits);

            var systemAllowBits = allowBits & PermissionType.SystemMask;
            var systemDenyBits = denyBits & PermissionType.SystemMask;
            var customAllowBits = (allowBits & PermissionType.CustomMask) >> PermissionType.NumberOfSystemPermissionTypes;
            var customDenyBits = (denyBits & PermissionType.CustomMask) >> PermissionType.NumberOfSystemPermissionTypes;

            StorePermission(entry.DefinedOnNodeId, entry.PrincipalId, entry.Propagates, SYSTEMPERMISSIONSTABLENAME, systemAllowBits, systemDenyBits);
            StorePermission(entry.DefinedOnNodeId, entry.PrincipalId, entry.Propagates, CUSTOMPERMISSIONSTABLENAME, customAllowBits, customDenyBits);
        }
        private void StorePermission(int definedOnNodeId, int identityId, bool propagates, string tableName, uint allowBits, uint denyBits)
        {
            var sql = new StringBuilder();

            if (allowBits + denyBits == 0)
            {
                sql.Append(String.Concat("DELETE FROM ", tableName, " WHERE DefinedOnNodeId = @NodeId AND PrincipalId = @PrincipalId AND IsInheritable = @IsInheritable"));
            }
            else
            {
                sql.Append("IF NOT EXISTS (SELECT SecurityEntryId FROM dbo.");
                sql.Append(tableName);
                sql.AppendLine(" WHERE DefinedOnNodeId = @NodeId AND PrincipalId = @PrincipalId AND IsInheritable = @IsInheritable)");
                sql.Append("    INSERT INTO dbo.");
                sql.Append(tableName);
                sql.AppendLine("(DefinedOnNodeId, PrincipalId, IsInheritable) VALUES (@NodeId, @PrincipalId, @IsInheritable)");
                sql.Append("UPDATE ").Append(tableName).AppendLine(" SET");

                for (int i = 0; i < ActiveSchema.PermissionTypes.Count; i++)
                {
                    var value = (byte)0;
                    var mask = 1 << i;
                    if ((denyBits & mask) != 0)
                        value = (byte)2;
                    else if ((allowBits & mask) != 0)
                        value = (byte)1;
                    if (i > 0)
                        sql.AppendLine(",");
                    sql.Append("    PermissionValue").Append(i + 1).Append(" = ").Append(value);
                }
                sql.AppendLine();

                sql.AppendLine("WHERE DefinedOnNodeId = @NodeId AND PrincipalId = @PrincipalId AND IsInheritable = @IsInheritable");
            }

            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = sql.ToString(), CommandType = CommandType.Text };
                cmd.Parameters.Add("@PrincipalId", SqlDbType.Int).Value = identityId;
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = definedOnNodeId;
                cmd.Parameters.Add("@IsInheritable", SqlDbType.TinyInt).Value = propagates ? (byte)1 : (byte)0;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }

        }

        protected internal override void ExplicateGroupMemberships()
        {
            SqlProcedure cmd = null;

            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Security_ExplicateGroupMemberships" };
                cmd.ExecuteNonQuery();
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }
        }

        protected internal override void ExplicateOrganizationUnitMemberships(IUser user)
        {
            SqlProcedure cmd = null;

            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Security_ExplicateOrgUnitMemberships" };
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = user.Id;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }
        }

        protected internal override void BreakInheritance(int nodeId)
        {
            SetBreakInheritanceFlag(nodeId, true);
        }
        protected internal override void RemoveBreakInheritance(int nodeId)
        {
            SetBreakInheritanceFlag(nodeId, false);
        }
        private void SetBreakInheritanceFlag(int nodeId, bool @break)
        {
            SqlProcedure cmd = null;
            using (cmd = new SqlProcedure { CommandText = "proc_Security_BreakInheritance2" })
            {
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeId;
                cmd.Parameters.Add("@BreakInheritanceValue", SqlDbType.TinyInt).Value = @break ? (byte)0 : (byte)1;
                cmd.ExecuteNonQuery();
            }
        }

        private const string LOADGROUPMEMBERSHIPSQL = @"DECLARE @GroupTypeId int
SELECT @GroupTypeId = PropertySetId FROM SchemaPropertySets WHERE Name = 'Group'
DECLARE @MembersPropertyTypeId int
SELECT @MembersPropertyTypeId = PropertyTypeId FROM SchemaPropertyTypes WHERE Name = 'Members';

WITH AllMembers (GroupId, MemberId) AS
(
	SELECT NodeId, NodeId
	FROM Nodes
	WHERE NodeId = @GroupId

	UNION ALL

	SELECT THIS.GroupId, RP.ReferredNodeId
	FROM ReferenceProperties RP
		JOIN Nodes N ON RP.VersionId = N.LastMinorVersionId
		JOIN AllMembers THIS ON THIS.MemberId = N.NodeId
	WHERE PropertyTypeId = @MembersPropertyTypeId
)
SELECT  DISTINCT AllMembers.GroupId, AllMembers.MemberId
FROM
	AllMembers
	JOIN Nodes ON Nodes.NodeId = AllMembers.MemberId
WHERE
	Nodes.NodeTypeId IN (SELECT NodeTypeId FROM dbo.udfGetAllDerivatedNodeTypesByNodeTypeId (@GroupTypeId))
	AND AllMembers.GroupId != AllMembers.MemberId
";
        protected internal override List<int> LoadGroupMembership(int groupId)
        {
            SqlProcedure cmd = null;
            SqlDataReader reader = null;
            var members = new List<int>();
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Security_LoadGroupMembership" };
                cmd.Parameters.Add("@GroupId", SqlDbType.Int).Value = groupId;
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var memberId = (int)reader["MemberId"];
                    members.Add(memberId);
                }
                return members;
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
                cmd.Dispose();
            }

        }

        internal override int LoadLastModifiersGroupId()
        {
            // SELECT TOP 1 NodeId FROM Nodes WHERE NodeTypeId = 2 AND Name = 'LastModifiers'
            SqlProcedure cmd = null;
            SqlDataReader reader = null;

            string commandString = String.Format("SELECT TOP 1 NodeId FROM Nodes WHERE NodeTypeId = {0} AND Name = 'LastModifiers'", ActiveSchema.NodeTypes["Group"].Id);

            cmd = new SqlProcedure { CommandText = commandString };
            cmd.CommandType = CommandType.Text;

            try
            {
                reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return 0;
                var id = reader.GetSafeInt32(0);
                return id;
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();
                if (cmd != null)
                    cmd.Dispose();
            }

        }

        //======================================================

        protected internal override void PersistUploadToken(UploadToken value)
        {
            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_ApplicationMessaging_PersistUploadToken" };

                cmd.Parameters.Add("@Token", SqlDbType.UniqueIdentifier).Value = value.UploadGuid;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = value.UserId;
                cmd.Parameters.Add("@CreatedOn", SqlDbType.DateTime).Value = DateTime.UtcNow;

                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }
        }
        protected internal override int GetUserIdByUploadGuid(Guid uploadGuid)
        {
            SqlProcedure cmd = null;
            int result;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_ApplicationMessaging_GetUserIdByUploadGuid" };
                cmd.Parameters.Add("@Token", SqlDbType.UniqueIdentifier).Value = uploadGuid;
                result = (int)cmd.ExecuteScalar();
            }
            finally
            {
                cmd.Dispose();
            }

            return result;
        }

        protected internal override NodeHead LoadNodeHead(string path)
        {
            return LoadNodeHead(0, path, 0);
        }
        protected internal override NodeHead LoadNodeHead(int nodeId)
        {
            return LoadNodeHead(nodeId, null, 0);
        }
        protected internal override NodeHead LoadNodeHeadByVersionId(int versionId)
        {
            return LoadNodeHead(0, null, versionId);
        }
        private NodeHead LoadNodeHead(int nodeId, string path, int versionId)
        {
            SqlProcedure cmd = null;
            SqlDataReader reader = null;

            //command string sceleton. When using this, WHERE clause needs to be completed!
            string commandString = @"
                    SELECT
                        Node.NodeId,             -- 0
	                    Node.Name,               -- 1
	                    Node.DisplayName,        -- 2
                        Node.Path,               -- 3
                        Node.ParentNodeId,       -- 4
                        Node.NodeTypeId,         -- 5
	                    Node.ContentListTypeId,  -- 6
	                    Node.ContentListId,      -- 7
                        Node.CreationDate,       -- 8
                        Node.ModificationDate,   -- 9
                        Node.LastMinorVersionId, -- 10
                        Node.LastMajorVersionId, -- 11
                        Node.CreatedById,        -- 12
                        Node.ModifiedById,       -- 13
  		                Node.[Index],            -- 14
		                Node.LockedById,         -- 15
                        Node.Timestamp           -- 16
                    FROM
	                    Nodes Node  
                    WHERE ";
            if (path != null)
            {
                commandString = string.Concat(commandString, "Node.Path = @Path COLLATE Latin1_General_CI_AS");
            }
            else if (versionId > 0)
            {
                commandString = string.Concat(@"DECLARE @NodeId int
                    SELECT @NodeId = NodeId FROM Versions WHERE VersionId = @VersionId
                ",
                 commandString,
                 "Node.NodeId = @NodeId");
            }
            else
            {
                commandString = string.Concat(commandString, "Node.NodeId = @NodeId");
            }

            cmd = new SqlProcedure { CommandText = commandString };
            cmd.CommandType = CommandType.Text;
            if (path != null)
                cmd.Parameters.Add("@Path", SqlDbType.NVarChar, 450).Value = path;
            else if (versionId > 0)
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
            else
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeId;

            try
            {
                reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return null;

                return new NodeHead(
                    reader.GetInt32(0),      // nodeId,
                    reader.GetString(1),     // name,
                    reader.GetSafeString(2), // displayName,
                    reader.GetString(3),     // pathInDb,
                    reader.GetSafeInt32(4),  // parentNodeId,
                    reader.GetInt32(5),      // nodeTypeId,
                    reader.GetSafeInt32(6),  // contentListTypeId,
                    reader.GetSafeInt32(7),  // contentListId,
                    reader.GetDateTimeUtc(8),   // creationDate,
                    reader.GetDateTimeUtc(9),   // modificationDate,
                    reader.GetSafeInt32(10), // lastMinorVersionId,
                    reader.GetSafeInt32(11), // lastMajorVersionId,
                    reader.GetSafeInt32(12), // creatorId,
                    reader.GetSafeInt32(13), // modifierId,
                    reader.GetSafeInt32(14), // index,
                    reader.GetSafeInt32(15), // lockerId
                    GetLongFromBytes((byte[])reader.GetValue(16))     // timestamp
                );

            }
            finally
            {
                if (reader != null)
                    reader.Dispose();
                if (cmd != null)
                    cmd.Dispose();
            }
        }
        protected internal override IEnumerable<NodeHead> LoadNodeHeads(IEnumerable<int> heads)
        {
            var nodeHeads = new List<NodeHead>();


            var cn = new SqlConnection(RepositoryConfiguration.ConnectionString);
            var cmd = new SqlCommand
            {
                Connection = cn,
                CommandType = CommandType.StoredProcedure,
                CommandText = "proc_NodeHead_Load_Batch"
            };

            var sb = new StringBuilder();
            sb.Append("<NodeHeads>");
            foreach (var id in heads)
                sb.Append("<id>").Append(id).Append("</id>");
            sb.Append("</NodeHeads>");

            cmd.Parameters.Add("@IdsInXml", SqlDbType.Xml).Value = sb.ToString();
            var adapter = new SqlDataAdapter(cmd);
            var dataSet = new DataSet();

            try
            {
                cn.Open();
                adapter.Fill(dataSet);
            }
            finally
            {
                cn.Close();
            }

            if (dataSet.Tables[0].Rows.Count > 0)
                foreach (DataRow currentRow in dataSet.Tables[0].Rows)
                {
                    if (currentRow["NodeID"] == DBNull.Value)
                        nodeHeads.Add(null);
                    else
                        nodeHeads.Add(new NodeHead(
                            TypeConverter.ToInt32(currentRow["NodeID"]),  //  0 - NodeId
                            TypeConverter.ToString(currentRow[1]),        //  1 - Name
                            TypeConverter.ToString(currentRow[2]),        //  2 - DisplayName
                            TypeConverter.ToString(currentRow[3]),        //  3 - Path
                            TypeConverter.ToInt32(currentRow[4]),         //  4 - ParentNodeId
                            TypeConverter.ToInt32(currentRow[5]),         //  5 - NodeTypeId
                            TypeConverter.ToInt32(currentRow[6]),         //  6 - ContentListTypeId 
                            TypeConverter.ToInt32(currentRow[7]),         //  7 - ContentListId
                            TypeConverter.ToDateTime(currentRow[8]),      //  8 - CreationDate
                            TypeConverter.ToDateTime(currentRow[9]),      //  9 - ModificationDate
                            TypeConverter.ToInt32(currentRow[10]),        // 10 - LastMinorVersionId
                            TypeConverter.ToInt32(currentRow[11]),        // 11 - LastMajorVersionId
                            TypeConverter.ToInt32(currentRow[12]),        // 12 - CreatedById
                            TypeConverter.ToInt32(currentRow[13]),        // 13 - ModifiedById
                            TypeConverter.ToInt32(currentRow[14]),        // 14 - Index
                            TypeConverter.ToInt32(currentRow[15]),        // 15 - LockedById
                            GetLongFromBytes((byte[])currentRow[16])
                            ));

                }
            return nodeHeads;
        }

        protected internal override NodeHead.NodeVersion[] GetNodeVersions(int nodeId)
        {
            SqlProcedure cmd = null;
            SqlDataReader reader = null;
            try
            {
                string commandString = @"
                    SELECT VersionId, MajorNumber, MinorNumber, Status
                    FROM Versions
                    WHERE NodeId = @NodeId
                    ORDER BY MajorNumber, MinorNumber
                ";
                cmd = new SqlProcedure { CommandText = commandString };
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@NodeId", SqlDbType.NVarChar, 450).Value = nodeId;
                reader = cmd.ExecuteReader();

                List<NodeHead.NodeVersion> versionList = new List<NodeHead.NodeVersion>();

                while (reader.Read())
                {
                    var versionId = reader.GetInt32(0);
                    var major = reader.GetInt16(1);
                    var minor = reader.GetInt16(2);
                    var statusCode = reader.GetInt16(3);

                    var versionNumber = new VersionNumber(major, minor, (VersionStatus)statusCode);

                    versionList.Add(new NodeHead.NodeVersion(versionNumber, versionId));
                }

                return versionList.ToArray();

            }
            finally
            {
                if (reader != null)
                    reader.Dispose();
                if (cmd != null)
                    cmd.Dispose();
            }


        }

        protected internal override BinaryCacheEntity LoadBinaryCacheEntity(int nodeVersionId, int propertyTypeId, out FileStreamData fileStreamData)
        {
            var columnDefinitions = RepositoryConfiguration.FileStreamEnabled
                ? string.Format(LOAD_BINARY_CACHEENTITY_COLUMNS_FORMAT_FILESTREAM, RepositoryConfiguration.BinaryChunkSize)
                : string.Format(LOAD_BINARY_CACHEENTITY_COLUMNS_FORMAT, RepositoryConfiguration.BinaryChunkSize);

            var commandText = string.Format(LOAD_BINARY_CACHEENTITY_FORMAT, columnDefinitions);

            fileStreamData = null;

            using (var cmd = new SqlProcedure { CommandText = commandText })
            {
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = nodeVersionId;
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId;
                cmd.CommandType = CommandType.Text;
                
                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult))
                {
                    if (reader.HasRows && reader.Read())
                    {
                        var length = reader.GetInt64(0);
                        var binaryPropertyId = reader.GetInt32(1);

                        byte[] rawData;
                        if (reader.IsDBNull(2))
                            rawData = null;
                        else
                            rawData = (byte[]) reader.GetValue(2);

                        var useFileStream = false;

                        if (RepositoryConfiguration.FileStreamEnabled)
                        {
                            useFileStream = reader.GetInt32(3) == 1;

                            if (useFileStream)
                            {
                                //fill Filestream info if we really need it
                                fileStreamData = new FileStreamData
                                                     {
                                                         Path = reader.GetSafeString(4),
                                                         TransactionContext = reader.GetSqlBytes(5).Buffer
                                                     };
                            }
                        }

                        return new BinaryCacheEntity()
                                   {
                                       Length = length,
                                       RawData = rawData,
                                       BinaryPropertyId = binaryPropertyId,
                                       UseFileStream = useFileStream
                                   };
                    }
                    
                    return null;
                }
            }
        }
        protected internal override byte[] LoadBinaryFragment(int binaryPropertyId, long position, int count)
        {
            var commandText = RepositoryConfiguration.FileStreamEnabled
                ? LOAD_BINARY_FRAGMENT_FILESTREAM
                : LOAD_BINARY_FRAGMENT;

            byte[] result;

            using (var cmd = new SqlProcedure { CommandText = commandText })
            {
                cmd.Parameters.Add("@BinaryPropertyId", SqlDbType.Int).Value = binaryPropertyId;
                cmd.Parameters.Add("@Position", SqlDbType.BigInt).Value = position + 1;
                cmd.Parameters.Add("@Count", SqlDbType.Int).Value = count;
                cmd.CommandType = CommandType.Text;

                result = (byte[])cmd.ExecuteScalar();
            }

            return result;
        }

        //private static Stream LoadFileStream(string path, byte[] transactionContext)
        //{
        //    return new System.Data.SqlTypes.SqlFileStream(path, transactionContext, FileAccess.Read, FileOptions.SequentialScan, 0);

        //    #region COMMENTED OUT, wrong solution: read full data into memory before returning a new memory stream
        //    //Stream stream = null;

        //    //using (Stream fileStream = new System.Data.SqlTypes.SqlFileStream(path, transactionContext, FileAccess.Read, FileOptions.SequentialScan, 0))
        //    //{
        //    //    var offset = 0;
        //    //    var size = BinaryStreamBufferLength;
        //    //    var length = Convert.ToInt32(fileStream.Length);
        //    //    var buffer = new byte[BinaryStreamBufferLength];

        //    //    stream = new MemoryStream(length);

        //    //    if (length > 0)
        //    //    {
        //    //        do
        //    //        {
        //    //            // Calculate buffer size - may be less than BinaryStreamBufferLength for last block.
        //    //            if ((offset + BinaryStreamBufferLength) >= length)
        //    //            {
        //    //                size = length - offset;
        //    //            }

        //    //            fileStream.Read(buffer, 0, size);

        //    //            stream.Write(buffer, 0, size);

        //    //            // Set the new offset
        //    //            offset += size;
        //    //        }
        //    //        while (offset < length);

        //    //        stream.Seek(0, SeekOrigin.Begin);
        //    //    }
        //    //}

        //    //return stream;

        //    #endregion
        //}

        protected internal override FileStreamData LoadFileStreamData(int binaryPropertyId, bool clearStream = false, int versionId = 0)
        {
            if (clearStream && versionId == 0)
                throw new DataException("If the stream needs to be cleared, the version id must be specified for security reasons.");

            FileStreamData fsData = null;

            using (var cmd = new SqlProcedure
                                {
                                    CommandText = clearStream ? CLEAR_STREAM_BY_BINARYID + GET_FILESTREAM_TRANSACTION_BY_BINARYID : GET_FILESTREAM_TRANSACTION_BY_BINARYID,
                                    CommandType = CommandType.Text
                                })
            {
                cmd.Parameters.Add("@BinaryPropertyId", SqlDbType.Int).Value = binaryPropertyId;

                // security check: the given binary property id must belong to the given version id
                if (clearStream)
                    cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult))
                {
                    if (reader.Read())
                    {
                        fsData = new FileStreamData
                            {
                                Path = reader.GetSafeString(0),
                                TransactionContext = reader.GetSqlBytes(1).Buffer
                            };
                    }
                }
            }

            return fsData;
        }

        protected override bool NodeExistsInDatabase(string path)
        {
            var cmd = new SqlProcedure { CommandText = "SELECT COUNT(*) FROM Nodes WHERE Path = @Path COLLATE Latin1_General_CI_AS", CommandType = CommandType.Text };
            cmd.Parameters.Add("@Path", SqlDbType.NVarChar, PathMaxLength).Value = path;
            try
            {
                var count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }
        }
        public override string GetNameOfLastNodeWithNameBase(int parentId, string namebase, string extension)
        {
            //var cmd = new SqlProcedure { CommandText = "SELECT TOP 1 Name FROM Nodes WHERE ParentNodeId=@ParentId AND Name LIKE @Name+'([0-9])' + @Extension ORDER BY LEN(Name) DESC, Name DESC", CommandType = CommandType.Text };
            var cmd = new SqlProcedure
            {
                CommandText = @"
SELECT TOP 1 Name FROM Nodes WHERE ParentNodeId=@ParentId AND (
	Name LIKE @Name+'([0-9])' + @Extension OR
	Name LIKE @Name+'([0-9][0-9])' + @Extension OR
	Name LIKE @Name+'([0-9][0-9][0-9])' + @Extension OR
	Name LIKE @Name+'([0-9][0-9][0-9][0-9])' + @Extension
)
ORDER BY LEN(Name) DESC, Name DESC",
                CommandType = CommandType.Text
            };
            cmd.Parameters.Add("@ParentId", SqlDbType.Int).Value = parentId;
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = namebase;
            cmd.Parameters.Add("@Extension", SqlDbType.NVarChar).Value = extension;
            try
            {
                var lastName = (string)cmd.ExecuteScalar();
                return lastName;
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }
        }

        public override DateTime RoundDateTime(DateTime d)
        {
            return new DateTime(d.Ticks / 100000 * 100000);
        }

        //====================================================== AppModel script generator

        #region AppModel script generator constants
        private const string AppModelQ0 = "DECLARE @availablePaths AS TABLE([Id] INT IDENTITY (1, 1), [Path] NVARCHAR(900))";
        private const string AppModelQ1 = "INSERT @availablePaths ([Path]) VALUES ('{0}')";

        private const string AppModelQ2 = @"SELECT TOP 1 N.NodeId FROM @availablePaths C
LEFT OUTER JOIN Nodes N ON C.[Path] = N.[Path]
WHERE N.[Path] IS NOT NULL
ORDER BY C.Id";

        private const string AppModelQ3 = @"SELECT N.NodeId FROM @availablePaths C
LEFT OUTER JOIN Nodes N ON C.[Path] = N.[Path]
WHERE N.[Path] IS NOT NULL
ORDER BY C.Id";

        private const string AppModelQ4 = @"SELECT N.NodeId, N.[Path] FROM Nodes N
WHERE N.ParentNodeId IN
(
    SELECT N.NodeId FROM @availablePaths C
    LEFT OUTER JOIN Nodes N ON C.[Path] = N.[Path]
    WHERE N.[Path] IS NOT NULL
)";
        #endregion

        protected override string GetAppModelScriptPrivate(IEnumerable<string> paths, bool resolveAll, bool resolveChildren)
        {
            var script = new StringBuilder();
            script.AppendLine(AppModelQ0);
            foreach (var path in paths)
            {
                script.AppendFormat(AppModelQ1, SecureSqlStringValue(path));
                script.AppendLine();
            }

            if (resolveAll)
            {
                if (resolveChildren)
                    script.AppendLine(AppModelQ4);
                else
                    script.AppendLine(AppModelQ3);
            }
            else
            {
                script.Append(AppModelQ2);
            }
            return script.ToString();
        }

        /// <summary>
        /// SQL injection prevention.
        /// </summary>
        /// <param name="value">String value that will changed to.</param>
        /// <returns>Safe string value.</returns>
        public static string SecureSqlStringValue(string value)
        {
            return value.Replace(@"'", @"''").Replace("/*", "**").Replace("--", "**");
        }

        //====================================================== Custom database script support

        protected internal override IDataProcedure CreateDataProcedureInternal(string commandText, string connectionName = null, InitialCatalog initialCatalog = InitialCatalog.Initial)
        {
            var proc = new SqlProcedure(connectionName, initialCatalog)
            {
                CommandText = commandText
            };
            return proc;
        }
        protected override IDbDataParameter CreateParameterInternal()
        {
            return new SqlParameter();
        }

        protected internal override void CheckScriptInternal(string commandText)
        {
            // c:\Program Files\Microsoft SQL Server\90\SDK\Assemblies\Microsoft.SqlServer.Smo.dll
            // c:\Program Files\Microsoft SQL Server\90\SDK\Assemblies\Microsoft.SqlServer.ConnectionInfo.dll

            //--- The code is equivalent to this script:
            // SET NOEXEC ON
            // GO
            // SELECT * FROM Nodes
            // GO
            // SET NOEXEC OFF
            // GO

            //var c = new Microsoft.SqlServer.Management.Common.ServerConnection(new SqlConnection(RepositoryConfiguration.ConnectionString));
            //var server = new Microsoft.SqlServer.Management.Smo.Server(c);
            //server.ConnectionContext.ExecuteNonQuery(commandText, Microsoft.SqlServer.Management.Common.ExecutionTypes.NoExec);
        }

        //====================================================== Index document save / load operations

        const string LOADINDEXDOCUMENTSCRIPT = @"
            SELECT N.NodeTypeId, V.VersionId, V.NodeId, N.ParentNodeId, N.Path, N.IsSystem, N.LastMinorVersionId, N.LastMajorVersionId, V.Status, 
                V.IndexDocument, N.Timestamp, V.Timestamp
            FROM Nodes N INNER JOIN Versions V ON N.NodeId = V.NodeId
            ";

        private const int DOCSFRAGMENTSIZE = 100;

        protected internal override void UpdateIndexDocument(NodeData nodeData, byte[] indexDocumentBytes)
        {
            using (var cmd = (SqlProcedure)CreateDataProcedure("UPDATE Versions SET [IndexDocument] = @IndexDocument WHERE VersionId = @VersionId\nSELECT Timestamp FROM Versions WHERE VersionId = @VersionId"))
            {
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = nodeData.VersionId;
                cmd.Parameters.Add("@IndexDocument", SqlDbType.VarBinary).Value = indexDocumentBytes;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // SELECT Timestamp FROM Versions WHERE VersionId = @VersionId
                        nodeData.VersionTimestamp = DataProvider.GetLongFromBytes((byte[])reader[0]);
                    }
                }
            }
        }
        protected internal override void UpdateIndexDocument(int versionId, byte[] indexDocumentBytes)
        {
            using (var cmd = (SqlProcedure)CreateDataProcedure("UPDATE Versions SET [IndexDocument] = @IndexDocument WHERE VersionId = @VersionId"))
            {
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                cmd.Parameters.Add("@IndexDocument", SqlDbType.VarBinary).Value = indexDocumentBytes;
                cmd.ExecuteNonQuery();
            }
        }

        protected internal override IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            using (var cmd = new SqlProcedure { CommandText = LOADINDEXDOCUMENTSCRIPT + "WHERE V.VersionId = @VersionId" })
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return GetIndexDocumentDataFromReader(reader);
                    return null;
                }
            }
        }
        protected internal override IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId)
        {
            var fi = 0;
            var listCount = versionId.Count();
            var result = new List<IndexDocumentData>();

            while (fi * DOCSFRAGMENTSIZE < listCount)
            {
                var docsSegment = versionId.Skip(fi * DOCSFRAGMENTSIZE).Take(DOCSFRAGMENTSIZE).ToArray();
                var paramNames = docsSegment.Select((s, i) => "@vi" + i.ToString()).ToArray();
                var where = String.Concat("WHERE V.VersionId IN (", string.Join(", ", paramNames), ")");

                SqlProcedure cmd = null;
                var retry = 0;
                while (retry < 15)
                {
                    try
                    {
                        cmd = new SqlProcedure { CommandText = LOADINDEXDOCUMENTSCRIPT + where };
                        cmd.CommandType = CommandType.Text;
                        for (var i = 0; i < paramNames.Length; i++)
                        {
                            cmd.Parameters.AddWithValue(paramNames[i], docsSegment[i]);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                result.Add(GetIndexDocumentDataFromReader(reader));
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteException(ex);
                        retry++;
                        System.Threading.Thread.Sleep(1000);
                    }
                    finally
                    {
                        if (cmd != null)
                            cmd.Dispose();
                    }
                }

                fi++;
            }

            return result;
        }
        protected internal override System.Data.Common.DbCommand CreateLoadIndexDocumentCollectionByPathProcedure(string path)
        {
            //var proc = CreateDataProcedure(LOADINDEXDOCUMENTSCRIPT + "WHERE N.Path = @Path COLLATE Latin1_General_CI_AS OR N.Path LIKE @Path + '/%' COLLATE Latin1_General_CI_AS ORDER BY N.Path");
            //proc.CommandType = CommandType.Text;
            //var pathParam = new SqlParameter("@Path", SqlDbType.NVarChar, PathMaxLength);
            //pathParam.Value = path;
            //proc.Parameters.Add(pathParam);
            //return proc;
            var cn = new SqlConnection(RepositoryConfiguration.ConnectionString);
            var cm = new SqlCommand
            {
                Connection = cn,
                CommandType = CommandType.Text,
                CommandText = LOADINDEXDOCUMENTSCRIPT + "WHERE N.Path = @Path COLLATE Latin1_General_CI_AS OR N.Path LIKE @Path + '/%' COLLATE Latin1_General_CI_AS ORDER BY N.Path"
            };
            var pathParam = new SqlParameter("@Path", SqlDbType.NVarChar, PathMaxLength);
            pathParam.Value = path;
            cm.Parameters.Add(pathParam);
            return cm;
        }
        protected internal override IndexDocumentData GetIndexDocumentDataFromReader(System.Data.Common.DbDataReader reader)
        {
            // 0           1          2       3             4     5         6                   7                   8       9              10         11
            // NodeTypeId, VersionId, NodeId, ParentNodeId, Path, IsSystem, LastMinorVersionId, LastMajorVersionId, Status, IndexDocument, Timestamp, Timestamp

            var versionId = reader.GetSafeInt32(1);
            var approved = Convert.ToInt32(reader.GetInt16(8)) == (int)VersionStatus.Approved;
            var isLastMajor = reader.GetSafeInt32(7) == versionId;

            var bytesData = reader.GetValue(9);
            var bytes = (bytesData == DBNull.Value) ? new byte[0] : (byte[])bytesData;

            return new IndexDocumentData(null, bytes)
            {
                NodeTypeId = reader.GetSafeInt32(0),
                VersionId = versionId,
                NodeId = reader.GetSafeInt32(2),
                ParentId = reader.GetSafeInt32(3),
                Path = reader.GetSafeString(4),
                IsSystem = reader.GetSafeBoolean(5),
                IsLastDraft = reader.GetSafeInt32(6) == versionId,
                IsLastPublic = approved && isLastMajor,
                NodeTimestamp = GetLongFromBytes((byte[])reader[10]),
                VersionTimestamp = GetLongFromBytes((byte[])reader[11]),
            };
        }
        protected internal override IEnumerable<int> GetIdsOfNodesThatDoNotHaveIndexDocument()
        {
            using (var proc = CreateDataProcedure("SELECT NodeId FROM Versions WHERE IndexDocument IS NULL"))
            {
                proc.CommandType = CommandType.Text;
                using (var reader = proc.ExecuteReader())
                {
                    var idSet = new List<int>();
                    while (reader.Read())
                        idSet.Add(reader.GetSafeInt32(0));

                    return idSet;
                }
            }
        }

        //====================================================== Index backup / restore operations

        const int BUFFERSIZE = 1024 * 128; // * 512; // * 64; // * 8;

        protected internal override IndexBackup LoadLastBackup()
        {
            var sql = @"
SELECT [IndexBackupId], [BackupNumber], [BackupDate], [ComputerName], [AppDomain],
        DATALENGTH([BackupFile]) AS [BackupFileLength], [RowGuid], [Timestamp]
    FROM [IndexBackup] WHERE IsActive != 0
SELECT [IndexBackupId], [BackupNumber], [BackupDate], [ComputerName], [AppDomain],
        DATALENGTH([BackupFile]) AS [BackupFileLength], [RowGuid], [Timestamp]
    FROM [IndexBackup2] WHERE IsActive != 0
";
            IndexBackup result = null;
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                using (var reader = cmd.ExecuteReader())
                {
                    do
                    {
                        while (reader.Read())
                            result = GetBackupFromReader(reader);
                    } while (reader.NextResult());
                }
            }
            return result;
        }
        protected internal override IndexBackup CreateBackup(int backupNumber)
        {
            var backup = new IndexBackup
            {
                BackupNumber = backupNumber,
                AppDomainName = AppDomain.CurrentDomain.FriendlyName,
                BackupDate = DateTime.UtcNow,
                ComputerName = Environment.MachineName,
            };

            var sql = String.Format(@"INSERT INTO {0} (BackupNumber, IsActive, BackupDate, ComputerName, [AppDomain]) VALUES
                (@BackupNumber, 0, @BackupDate, @ComputerName, @AppDomain)", backup.TableName);

            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                cmd.Parameters.Add("@BackupNumber", SqlDbType.Int).Value = backup.BackupNumber;
                cmd.Parameters.Add("@BackupDate", SqlDbType.DateTime).Value = backup.BackupDate;
                cmd.Parameters.Add("@ComputerName", SqlDbType.NVarChar, 100).Value = backup.ComputerName;
                cmd.Parameters.Add("@AppDomain", SqlDbType.NVarChar, 500).Value = backup.AppDomainName;

                cmd.ExecuteNonQuery();
            }
            return backup;
        }
        private IndexBackup GetBackupFromReader(SqlDataReader reader)
        {
            var result = new IndexBackup();
            result.IndexBackupId = reader.GetInt32(0);              // IndexBackupId
            result.BackupNumber = reader.GetInt32(1);               // BackupNumber
            result.BackupDate = reader.GetDateTimeUtc(2);              // BackupDate
            result.ComputerName = reader.GetSafeString(3);          // ComputerName
            result.AppDomainName = reader.GetSafeString(4);         // AppDomain
            result.BackupFileLength = reader.GetInt64(5);           // BackupFileLength
            result.RowGuid = reader.GetGuid(6);                     // RowGuid
            result.Timestamp = GetLongFromBytes((byte[])reader[7]); // Timestamp
            return result;
        }
        protected internal override void StoreBackupStream(string backupFilePath, IndexBackup backup, IndexBackupProgress progress)
        {
            var fileLength = new FileInfo(backupFilePath).Length;

            using (var writeCommand = CreateWriteCommand(backup))
            {
                using (var stream = new FileStream(backupFilePath, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        InitializeNewStream(backup);

                        progress.Type = IndexBackupProgressType.Storing;
                        progress.Message = "Storing backup";
                        progress.MaxValue = fileLength;

                        var timer = Stopwatch.StartNew();

                        var offset = 0L;
                        while (offset < fileLength)
                        {
                            progress.Value = offset;
                            progress.NotifyChanged();

                            var remnant = fileLength - offset;
                            var length = remnant < BUFFERSIZE ? Convert.ToInt32(remnant) : BUFFERSIZE;
                            var buffer = reader.ReadBytes(length);
                            writeCommand.Parameters["@Buffer"].Value = buffer;
                            writeCommand.Parameters["@Offset"].Value = offset;
                            writeCommand.Parameters["@Length"].Value = length;
                            writeCommand.ExecuteNonQuery();
                            offset += BUFFERSIZE;
                        }
                        //progress.FinishStoreIndexBackupToDb();
                        ////progress.Value = fileLength;
                        ////progress.NotifyChanged();
                    }
                }
            }
        }
        //protected internal override void StoreBackupStream(string backupFilePath, IndexBackup2 backup, BackupProgress progress)
        //{
        //    var fileLength = new FileInfo(backupFilePath).Length;

        //    using (var stream = new FileStream(backupFilePath, FileMode.Open))
        //    {
        //        using (var reader = new BinaryReader(stream))
        //        {
        //            InitializeNewStream(backup);

        //            progress.Type = BackupProgressType.Storing;
        //            progress.Message = "Storing backup";
        //            progress.MaxValue = fileLength;

        //            var offset = 0L;
        //            while (offset < fileLength)
        //            {
        //                using (var writeCommand = CreateWriteCommand(backup))
        //                {
        //                    progress.Value = offset;
        //                    progress.NotifyChanged();

        //                    var remnant = fileLength - offset;
        //                    var length = remnant < BUFFERSIZE ? Convert.ToInt32(remnant) : BUFFERSIZE;
        //                    var buffer = reader.ReadBytes(length);
        //                    writeCommand.Parameters["@Buffer"].Value = buffer;
        //                    writeCommand.Parameters["@Offset"].Value = offset;
        //                    writeCommand.Parameters["@Length"].Value = length;
        //                    writeCommand.ExecuteNonQuery();
        //                    offset += BUFFERSIZE;
        //                }
        //            }
        //            progress.Value = fileLength;
        //            progress.NotifyChanged();
        //        }
        //    }
        //}
        private SqlProcedure CreateWriteCommand(IndexBackup backup)
        {
            var sql = String.Format("UPDATE {0} SET [BackupFile].WRITE(@Buffer, @Offset, @Length) WHERE BackupNumber = @BackupNumber", backup.TableName);
            var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text };
            cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@BackupNumber", SqlDbType.Int)).Value = backup.BackupNumber;
            cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Offset", SqlDbType.BigInt));
            cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Length", SqlDbType.BigInt));
            cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Buffer", SqlDbType.VarBinary));
            return cmd;
        }
        private void InitializeNewStream(IndexBackup backup)
        {
            var sql = String.Format("UPDATE {0} SET [BackupFile] = @InitialStream WHERE BackupNumber = @BackupNumber", backup.TableName);
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add(new SqlParameter("@BackupNumber", SqlDbType.Int));
                cmd.Parameters["@BackupNumber"].Value = backup.BackupNumber;
                cmd.Parameters.Add(new SqlParameter("@InitialStream", SqlDbType.VarBinary));
                cmd.Parameters["@InitialStream"].Value = new byte[0];
                cmd.ExecuteNonQuery();
            }
        }
        protected internal override void SetActiveBackup(IndexBackup backup, IndexBackup lastBackup)
        {
            var sql = (lastBackup == null) ?
                String.Format("UPDATE {0} SET IsActive = 1 WHERE BackupNumber = @ActiveBackupNumber", backup.TableName)
                :
                String.Format(@"UPDATE {0} SET IsActive = 1 WHERE BackupNumber = @ActiveBackupNumber
                    UPDATE {1} SET IsActive = 0 WHERE BackupNumber = @InactiveBackupNumber", backup.TableName, lastBackup.TableName);
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                cmd.Parameters.Add(new SqlParameter("@ActiveBackupNumber", SqlDbType.Int)).Value = backup.BackupNumber;
                if (lastBackup != null)
                    cmd.Parameters.Add(new SqlParameter("@InactiveBackupNumber", SqlDbType.Int)).Value = lastBackup.BackupNumber;
                cmd.ExecuteNonQuery();
            }
        }
        protected override void KeepOnlyLastIndexBackup()
        {
            var backup = LoadLastBackup();
            if (backup == null)
                return;

            backup = new IndexBackup { BackupNumber = backup.BackupNumber - 1 };
            var sql = "TRUNCATE TABLE " + backup.TableName;
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
                cmd.ExecuteNonQuery();
        }

        protected override Guid GetLastIndexBackupNumber()
        {
            var backup = LoadLastBackup();
            if (backup == null)
                throw GetNoBackupException();
            return backup.RowGuid;
        }
        private Exception GetNoBackupException()
        {
            return new InvalidOperationException("Last index backup does not exist in the database.");
        }

        /*------------------------------------------------------*/

        protected override IndexBackup RecoverIndexBackup(string backupFilePath)
        {
            var backup = LoadLastBackup();
            if (backup == null)
                throw GetNoBackupException();

            if (File.Exists(backupFilePath))
                File.Delete(backupFilePath);

            var dbFileLength = backup.BackupFileLength;

            using (var readCommand = CreateReadCommand(backup))
            {
                using (var stream = new FileStream(backupFilePath, FileMode.Create))
                {
                    BinaryWriter writer = new BinaryWriter(stream);
                    var offset = 0L;
                    while (offset < dbFileLength)
                    {
                        var remnant = dbFileLength - offset;
                        var length = remnant < BUFFERSIZE ? Convert.ToInt32(remnant) : BUFFERSIZE;
                        readCommand.Parameters["@Offset"].Value = offset;
                        readCommand.Parameters["@Length"].Value = length;
                        readCommand.ExecuteNonQuery();
                        var buffer = (byte[])readCommand.ExecuteScalar();
                        writer.Write(buffer, 0, buffer.Length);
                        offset += BUFFERSIZE;
                    }
                }
            }
            return backup;
        }
        private IDataProcedure CreateReadCommand(IndexBackup backup)
        {
            var sql = String.Format("SELECT SUBSTRING([BackupFile], @Offset, @Length) FROM {0} WHERE BackupNumber = @BackupNumber", backup.TableName);
            var cmd = new SqlProcedure { CommandText = sql, CommandType = System.Data.CommandType.Text };
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new SqlParameter("@BackupNumber", SqlDbType.Int)).Value = backup.BackupNumber;
            cmd.Parameters.Add(new SqlParameter("@Offset", SqlDbType.BigInt));
            cmd.Parameters.Add(new SqlParameter("@Length", SqlDbType.BigInt));
            return cmd;
        }

        private const string GETLASTACTIVITYIDSCRIPT = "SELECT CASE WHEN i.last_value IS NULL THEN 0 ELSE CONVERT(int, i.last_value) END last_value FROM sys.identity_columns i JOIN sys.tables t ON i.object_id = t.object_id WHERE t.name = 'IndexingActivity'";
        public override int GetLastActivityId()
        {
            using (var cmd = new SqlProcedure { CommandText = GETLASTACTIVITYIDSCRIPT, CommandType = CommandType.Text })
            {
                var x = cmd.ExecuteScalar();
                if (x == DBNull.Value)
                    return 0;
                return Convert.ToInt32(x);
            }
        }

        //====================================================== Checking  index integrity

        public override IDataProcedure GetTimestampDataForOneNodeIntegrityCheck(string path)
        {
            string checkNodeSql = "SELECT V.VersionId, CONVERT(bigint, n.timestamp) NodeTimestamp, CONVERT(bigint, v.timestamp) VersionTimestamp from Versions V join Nodes N on V.NodeId = N.NodeId WHERE N.Path = '{0}' COLLATE Latin1_General_CI_AS";
            var sql = String.Format(checkNodeSql, path);
            var proc = ContentRepository.Storage.Data.DataProvider.CreateDataProcedure(sql);
            proc.CommandType = System.Data.CommandType.Text;
            return proc;
        }
        public override IDataProcedure GetTimestampDataForRecursiveIntegrityCheck(string path)
        {
            string sql;
            if (path == null)
                sql = "SELECT V.VersionId, CONVERT(bigint, n.timestamp) NodeTimestamp, CONVERT(bigint, v.timestamp) VersionTimestamp from Versions V join Nodes N on V.NodeId = N.NodeId";
            else
                sql = String.Format("SELECT V.VersionId, CONVERT(bigint, n.timestamp) NodeTimestamp, CONVERT(bigint, v.timestamp) VersionTimestamp from Versions V join Nodes N on V.NodeId = N.NodeId WHERE N.Path = '{0}' COLLATE Latin1_General_CI_AS OR N.Path LIKE '{0}/%' COLLATE Latin1_General_CI_AS", path);
            var proc = ContentRepository.Storage.Data.DataProvider.CreateDataProcedure(sql);
            proc.CommandType = System.Data.CommandType.Text;
            return proc;
        }

        //====================================================== Database backup / restore operations

        private string _databaseName;
        public override string DatabaseName
        {
            get
            {
                if (_databaseName == null)
                {
                    var cnstr = new SqlConnectionStringBuilder(RepositoryConfiguration.ConnectionString);
                    _databaseName = cnstr.InitialCatalog;
                }
                return _databaseName;
            }
        }

        public override IEnumerable<string> GetScriptsForDatabaseBackup()
        {
            return new[]
            {
                "USE [Master]",
                @"BACKUP DATABASE [{DatabaseName}] TO DISK = N'{BackupFilePath}' WITH NOFORMAT, INIT, NAME = N'ContentRepository-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
            };
        }

        //====================================================== Powershell provider

        protected internal override int InitializeStagingBinaryData(int versionId, int propertyTypeId, string fileName, long fileSize)
        {
            var sql = @"
                INSERT INTO StagingBinaryProperties ( VersionId,  PropertyTypeId,  ContentType,  FileNameWithoutExtension,  Extension,  Size,  Stream) VALUES
                                                    (@VersionId, @PropertyTypeId, @ContentType, @FileNameWithoutExtension, @Extension, @Size,    0x00)
                SELECT @@IDENTITY";
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                var fName = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);
                var mime = MimeTable.GetMimeType(ext.ToLower(CultureInfo.InvariantCulture));
                cmd.Parameters.Add(new SqlParameter("@VersionId", SqlDbType.Int)).Value = versionId;
                cmd.Parameters.Add(new SqlParameter("@PropertyTypeId", SqlDbType.Int)).Value = propertyTypeId;
                cmd.Parameters.Add(new SqlParameter("@ContentType", SqlDbType.NVarChar, 50)).Value = mime;
                cmd.Parameters.Add(new SqlParameter("@FileNameWithoutExtension", SqlDbType.NVarChar, 450)).Value = fName;
                cmd.Parameters.Add(new SqlParameter("@Extension", SqlDbType.NVarChar, 450)).Value = ext;
                cmd.Parameters.Add(new SqlParameter("@Size", SqlDbType.BigInt)).Value = fileSize;
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }
        protected internal override void SaveChunk(int stagingBinaryDataId, byte[] bytes, int offset)
        {
            var sql = "UPDATE StagingBinaryProperties SET [Stream].WRITE(@Buffer, @Offset, @Length) WHERE Id = @Id";
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Id", SqlDbType.Int)).Value = stagingBinaryDataId;
                cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Buffer", SqlDbType.VarBinary)).Value = bytes;
                cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Offset", SqlDbType.BigInt)).Value = offset;
                cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Length", SqlDbType.BigInt)).Value = bytes.LongLength;
                cmd.ExecuteNonQuery();
            }
        }
        protected internal override void CopyStagingToBinaryData(int versionId, int propertyTypeId, int stagingBinaryDataId, string checksum)
        {
            var sql = @"
UPDATE BinaryProperties
	SET ContentType = S.ContentType,
		FileNameWithoutExtension = S.FileNameWithoutExtension,
		Extension = S.Extension,
		Size = S.Size,
		[Checksum] = @Checksum,
		Stream = S.Stream
	FROM BinaryProperties B
		JOIN StagingBinaryProperties S ON S.VersionId = B.VersionId AND S.PropertyTypeId = B.PropertyTypeId
WHERE B.VersionId = @VersionId AND B.PropertyTypeId = @PropertyTypeId AND B.Staging IS NULL
";
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                cmd.Parameters.Add(new SqlParameter("@VersionId", SqlDbType.Int)).Value = versionId;
                cmd.Parameters.Add(new SqlParameter("@PropertyTypeId", SqlDbType.Int)).Value = propertyTypeId;
                cmd.Parameters.Add(new SqlParameter("@Checksum", SqlDbType.VarChar, 200)).Value = checksum;
                cmd.ExecuteNonQuery();
            }
        }
        protected internal override void DeleteStagingBinaryData(int stagingBinaryDataId)
        {
            var sql = "DELETE from StagingBinaryProperties WHERE Id = @Id";
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int)).Value = stagingBinaryDataId;
                cmd.ExecuteNonQuery();
            }
        }

        //====================================================== Packaging

        public override ApplicationInfo CreateInitialVersion(string name, string edition, Version version, string description)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (edition == null)
                throw new ArgumentNullException("edition");
            if (version == null)
                throw new ArgumentNullException("version");
            if (name.Length == 0)
                throw new ArgumentException("The name cannot be empty.");
            if (edition.Length == 0)
                throw new ArgumentException("The edition cannot be empty.");

            var snAppInfo = LoadOfficialVersion();
            if (snAppInfo != null)
                return snAppInfo; //throw new ApplicationException("Cannot create initial version.");

            SavePackage(new Package
                {
                    Name = name,
                    Edition = edition,
                    Description = description,
                    Version = version,
                    AppId = null,
                    PackageType = PackageType.Product,
                    PackageLevel = PackageLevel.Install,
                    ReleaseDate = DateTime.UtcNow,
                    ExecutionDate = DateTime.UtcNow,
                    ExecutionResult = ExecutionResult.Successful
                });

            return LoadOfficialVersion();
        }

        private static readonly string InstalledScript = @"SELECT P2.Name, P2.Edition, P2.Description, null AppId, P1.AppVersion, P3.AppVersion AcceptableVersion
FROM (SELECT Name, Edition, Description, AppId FROM Packages WHERE PackageLevel = '" + PackageLevel.Install.ToString() + @"' AND APPID IS NULL) P2
JOIN (SELECT AppId, MAX(Version) AppVersion FROM Packages WHERE APPID IS NULL GROUP BY AppId) P1
ON P1.AppId IS NULL AND P2.AppId IS NULL
JOIN (SELECT AppId, MAX(Version) AppVersion FROM Packages WHERE APPID IS NULL
    AND ExecutionResult != '" + ExecutionResult.Faulty.ToString() + @"' 
    AND ExecutionResult != '" + ExecutionResult.Unfinished.ToString() + @"' GROUP BY AppId, ExecutionResult) P3
ON P1.AppId IS NULL AND P3.AppId IS NULL";

        public override ApplicationInfo LoadOfficialVersion()
        {
            var apps = new List<ApplicationInfo>();
            using (var cmd = new SqlProcedure { CommandText = InstalledScript, CommandType = CommandType.Text })
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        apps.Add(new ApplicationInfo
                        {
                            Name = reader.GetString(reader.GetOrdinal("Name")),                                                     // nvarchar 450  not null
                            Edition = reader.GetSafeString(reader.GetOrdinal("Edition")),                                           // nvarchar 450  null
                            AppId = reader.GetSafeString(reader.GetOrdinal("AppId")),                                               // varchar  50   null
                            Version = DecodePackageVersion(reader.GetSafeString(reader.GetOrdinal("AppVersion"))),                  // varchar  50   null
                            AcceptableVersion = DecodePackageVersion(reader.GetSafeString(reader.GetOrdinal("AcceptableVersion"))), // varchar  50   null
                            Description = reader.GetSafeString(reader.GetOrdinal("Description")),                                   // nvarchar 1000 null
                        });
                    }
                }
            }
            return apps.FirstOrDefault();
        }

        private static readonly string InstalledAppScript = @"SELECT P2.Name, P2.Edition, P2.Description, P1.AppId, P1.AppVersion, P1a.AppVersion AcceptableVersion
FROM (SELECT AppId, MAX(AppVersion) AppVersion FROM Packages WHERE APPID IS NOT NULL GROUP BY AppId) P1
JOIN (SELECT AppId, MAX(AppVersion) AppVersion FROM Packages WHERE APPID IS NOT NULL 
    AND ExecutionResult != '" + ExecutionResult.Faulty.ToString() + @"'
    AND ExecutionResult != '" + ExecutionResult.Unfinished.ToString() + @"' GROUP BY AppId, ExecutionResult) P1a
ON P1.AppId = P1a.AppId
JOIN (SELECT Name, Edition, Description, AppId FROM Packages WHERE PackageLevel = '" + PackageLevel.Install.ToString() + @"'
    AND ExecutionResult != '" + ExecutionResult.Faulty.ToString() + @"'
    AND ExecutionResult != '" + ExecutionResult.Unfinished.ToString() + @"') P2
ON P1.AppId = P2.AppId";

        public override IEnumerable<ApplicationInfo> LoadInstalledApplications()
        {
            var apps = new List<ApplicationInfo>();
            using (var cmd = new SqlProcedure { CommandText = InstalledAppScript, CommandType = CommandType.Text })
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        apps.Add(new ApplicationInfo
                        {
                            Name = reader.GetString(reader.GetOrdinal("Name")),                                                     // nvarchar 450  not null
                            Edition = reader.GetSafeString(reader.GetOrdinal("Edition")),                                           // nvarchar 450  null
                            AppId = reader.GetSafeString(reader.GetOrdinal("AppId")),                                               // varchar  50   null
                            Version = DecodePackageVersion(reader.GetSafeString(reader.GetOrdinal("AppVersion"))),                  // varchar  50   null
                            AcceptableVersion = DecodePackageVersion(reader.GetSafeString(reader.GetOrdinal("AcceptableVersion"))), // varchar  50   null
                            Description = reader.GetSafeString(reader.GetOrdinal("Description")),                                   // nvarchar 1000 null
                        });
                    }
                }
            }
            return apps;
        }

        public override IEnumerable<Package> LoadInstalledPackages()
        {
            var packages = new List<Package>();
            using (var cmd = new SqlProcedure { CommandText = "SELECT * FROM Packages", CommandType = CommandType.Text })
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        packages.Add(new Package
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),                                                                                  // int           not null
                            Name = reader.GetString(reader.GetOrdinal("Name")),                                                                             // nvarchar 450  not null
                            Edition = reader.GetSafeString(reader.GetOrdinal("Edition")),                                                                   // nvarchar 450  null
                            Description = reader.GetSafeString(reader.GetOrdinal("Description")),                                                           // nvarchar 1000 null
                            AppId = reader.GetSafeString(reader.GetOrdinal("AppId")),                                                                       // varchar 50    null
                            PackageLevel = (PackageLevel)Enum.Parse(typeof(PackageLevel), reader.GetString(reader.GetOrdinal("PackageLevel"))),             // varchar 50    not null
                            PackageType = (PackageType)Enum.Parse(typeof(PackageType), reader.GetString(reader.GetOrdinal("PackageType"))),                 // varchar 50    not null
                            ReleaseDate = reader.GetDateTimeUtc(reader.GetOrdinal("ReleaseDate")),                                                             // datetime      not null
                            ExecutionDate = reader.GetDateTimeUtc(reader.GetOrdinal("ExecutionDate")),                                                         // datetime      not null
                            ExecutionResult = (ExecutionResult)Enum.Parse(typeof(ExecutionResult), reader.GetString(reader.GetOrdinal("ExecutionResult"))), // varchar 50    not null
                            ExecutionError = DeserializeExecutionError(reader.GetSafeString(reader.GetOrdinal("ExecutionError"))),     
                            ApplicationVersion = DecodePackageVersion(reader.GetSafeString(reader.GetOrdinal("AppVersion"))),                                                                      // varchar 50    null
                            Version =  DecodePackageVersion(reader.GetSafeString(reader.GetOrdinal("Version"))),                                        // varchar 50    not null
                        });
                    }
                }
            }
            return packages;
        }
        private Version GetSafeVersion(SqlDataReader reader, string columnName)
        {
            var version = reader.GetSafeString(reader.GetOrdinal(columnName));
            if (version == null)
                return null;
            return Version.Parse(version);
        }

        private static readonly string SavePackageScript = @"INSERT INTO Packages
    (  Name,  Edition,  Description,  AppId,  PackageLevel,  PackageType,  ReleaseDate,  ExecutionDate,  ExecutionResult,  ExecutionError,  AppVersion,  Version) VALUES
    ( @Name, @Edition, @Description, @AppId, @PackageLevel, @PackageType, @ReleaseDate, @ExecutionDate, @ExecutionResult, @ExecutionError, @AppVersion, @Version)
SELECT @@IDENTITY";
        public override void SavePackage(Package package)
        {
            using (var cmd = new SqlProcedure { CommandText = SavePackageScript, CommandType = CommandType.Text })
            {
                cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 450)).Value = package.Name;
                cmd.Parameters.Add(new SqlParameter("@Edition", SqlDbType.NVarChar, 450)).Value = (object)package.Edition ?? DBNull.Value;
                cmd.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 1000)).Value = (object)package.Description ?? DBNull.Value;
                cmd.Parameters.Add(new SqlParameter("@AppId", SqlDbType.VarChar, 50)).Value = (object)package.AppId ?? DBNull.Value;
                cmd.Parameters.Add(new SqlParameter("@PackageLevel", SqlDbType.VarChar, 50)).Value = package.PackageLevel.ToString();
                cmd.Parameters.Add(new SqlParameter("@PackageType", SqlDbType.VarChar, 50)).Value = package.PackageType.ToString();
                cmd.Parameters.Add(new SqlParameter("@ReleaseDate", SqlDbType.DateTime)).Value = package.ReleaseDate;
                cmd.Parameters.Add(new SqlParameter("@ExecutionDate", SqlDbType.DateTime)).Value = package.ExecutionDate;
                cmd.Parameters.Add(new SqlParameter("@ExecutionResult", SqlDbType.VarChar, 50)).Value = package.ExecutionResult.ToString();
                cmd.Parameters.Add(new SqlParameter("@ExecutionError", SqlDbType.NVarChar)).Value = SerializeExecutionError(package.ExecutionError) ?? (object)DBNull.Value;
                cmd.Parameters.Add(new SqlParameter("@AppVersion", SqlDbType.VarChar, 50)).Value = package.ApplicationVersion == null ? DBNull.Value : (object)EncodePackageVersion(package.ApplicationVersion);
                cmd.Parameters.Add(new SqlParameter("@Version", SqlDbType.VarChar, 50)).Value = EncodePackageVersion(package.Version);

                var result = cmd.ExecuteScalar();
                package.Id = Convert.ToInt32(result);
            }
        }

        private static readonly string UpdatePackageScript = @"UPDATE Packages
    SET AppId = @AppId,
        Name = @Name,
        Edition = @Edition,
        Description = @Description,
        PackageLevel = @PackageLevel,
        PackageType = @PackageType,
        ReleaseDate = @ReleaseDate,
        ExecutionDate = @ExecutionDate,
        ExecutionResult = @ExecutionResult,
        ExecutionError = @ExecutionError,
        AppVersion = @AppVersion,
        Version = @Version
WHERE Id = @Id
";
        public override void UpdatePackage(Package package)
        {
            var product = package.PackageType == PackageType.Product;
            using (var cmd = new SqlProcedure { CommandText = UpdatePackageScript, CommandType = CommandType.Text })
            {
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int)).Value = package.Id;
                cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 450)).Value = package.Name;
                cmd.Parameters.Add(new SqlParameter("@Edition", SqlDbType.NVarChar, 450)).Value = (object)package.Edition ?? DBNull.Value;
                cmd.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 1000)).Value = (object)package.Description ?? DBNull.Value;
                cmd.Parameters.Add(new SqlParameter("@AppId", SqlDbType.VarChar, 50)).Value = (object)package.AppId ?? DBNull.Value;
                cmd.Parameters.Add(new SqlParameter("@PackageLevel", SqlDbType.VarChar, 50)).Value = package.PackageLevel.ToString();
                cmd.Parameters.Add(new SqlParameter("@PackageType", SqlDbType.VarChar, 50)).Value = package.PackageType.ToString();
                cmd.Parameters.Add(new SqlParameter("@ReleaseDate", SqlDbType.DateTime)).Value = package.ReleaseDate;
                cmd.Parameters.Add(new SqlParameter("@ExecutionDate", SqlDbType.DateTime)).Value = package.ExecutionDate;
                cmd.Parameters.Add(new SqlParameter("@ExecutionResult", SqlDbType.VarChar, 50)).Value = package.ExecutionResult.ToString();
                cmd.Parameters.Add(new SqlParameter("@ExecutionError", SqlDbType.NVarChar)).Value = SerializeExecutionError(package.ExecutionError) ?? (object)DBNull.Value;
                cmd.Parameters.Add(new SqlParameter("@AppVersion", SqlDbType.VarChar, 50)).Value = package.ApplicationVersion == null ? DBNull.Value : (object)EncodePackageVersion(package.ApplicationVersion);
                cmd.Parameters.Add(new SqlParameter("@Version", SqlDbType.VarChar, 50)).Value = EncodePackageVersion(package.Version);

                cmd.ExecuteNonQuery();
            }
        }
        private string SerializeExecutionError(Exception e)
        {
            if (e == null)
                return null;

            var serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            try
            {
                using (var sw = new StringWriter())
                {
                    using (JsonWriter writer = new JsonTextWriter(sw))
                        serializer.Serialize(writer, e);
                    return sw.GetStringBuilder().ToString();
                }
            }
            catch (Exception ee)
            {
                using (var sw = new StringWriter())
                {
                    using (JsonWriter writer = new JsonTextWriter(sw))
                        serializer.Serialize(writer, new Exception("Cannot serialize the execution error: " + ee.Message));
                    return sw.GetStringBuilder().ToString();
                }
            }
        }
        private Exception DeserializeExecutionError(string data)
        {
            if (data == null)
                return null;

            var serializer = new JsonSerializer();
            using (var jreader = new JsonTextReader(new StringReader(data)))
                return serializer.Deserialize<Exception>(jreader);
        }

        private static readonly string ProductPackageExistenceScript = @"SELECT COUNT(0) FROM Packages
WHERE AppId = @AppId AND PackageType = @PackageType AND PackageLevel = @PackageLevel AND Version = @Version
";
        public override bool IsPackageExist(string appId, PackageType packageType, PackageLevel packageLevel, Version version)
        {
            int count;
            using (var cmd = new SqlProcedure { CommandText = ProductPackageExistenceScript, CommandType = CommandType.Text })
            {
                cmd.Parameters.Add(new SqlParameter("@AppId", SqlDbType.VarChar, 50)).Value = (object)appId ?? DBNull.Value;
                cmd.Parameters.Add(new SqlParameter("@PackageLevel", SqlDbType.VarChar, 50)).Value = packageLevel.ToString();
                cmd.Parameters.Add(new SqlParameter("@PackageType", SqlDbType.VarChar, 50)).Value = packageType.ToString();
                cmd.Parameters.Add(new SqlParameter("@Version", SqlDbType.VarChar, 50)).Value = EncodePackageVersion(version);
                count = (int)cmd.ExecuteScalar();
            }
            return count > 0;
        }

        public override void DeletePackage(Package package)
        {
            if (package.Id < 1)
                throw new ApplicationException("Cannot delete unsaved package");

            using (var cmd = new SqlProcedure { CommandText = "DELETE FROM Packages WHERE Id = @Id", CommandType = CommandType.Text })
            {
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int)).Value = package.Id;
                cmd.ExecuteNonQuery();
            }
        }

        internal override void DeletePackagesExceptFirst()
        {
            using (var cmd = new SqlProcedure { CommandText = "DELETE FROM Packages WHERE Id > 1", CommandType = CommandType.Text })
                cmd.ExecuteNonQuery();
        }

        private static string EncodePackageVersion(Version v)
        {
            if (v.Build < 0)
                return String.Format("{0:0#########}.{1:0#########}", v.Major, v.Minor);
            if (v.Revision < 0)
                return String.Format("{0:0#########}.{1:0#########}.{2:0#########}", v.Major, v.Minor, v.Build);
            return String.Format("{0:0#########}.{1:0#########}.{2:0#########}.{3:0#########}", v.Major, v.Minor, v.Build, v.Revision);
        }
        private static Version DecodePackageVersion(string s)
        {
            if (s == null)
                return null;
            return Version.Parse(s);
        }
    }
}

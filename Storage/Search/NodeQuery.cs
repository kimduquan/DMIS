using System;
using ContentRepository.Storage;
using ContentRepository.Storage.Data;
using ContentRepository.Storage.Security;
using System.Collections.Generic;
using System.Xml;
using ContentRepository.Storage.Schema;
using System.IO;
using System.Xml.XPath;
using System.Reflection;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using System.Linq;
using System.Globalization;
using Diagnostics;
using ContentRepository.Storage.Search.Internal;

namespace ContentRepository.Storage.Search
{
    public enum ExecutionHint { None, ForceRelationalEngine, ForceIndexedEngine }

    [System.Diagnostics.DebuggerDisplay("{Name} = {Value}")]
    public class NodeQueryParameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public class NodeQuery : ExpressionList, IEnumerable<Expression>
    {
        private ISearchEngine IndexedSearchEngine
        {
            get { return StorageContext.Search.SearchEngine; }
        }
        private bool IsRelationalEngineQuery()
        {
            if (!StorageContext.Search.IsOuterEngineEnabled)
                return true;
            if (IndexedSearchEngine == InternalSearchEngine.Instance)
                return true;
            return false;
        }

        private static readonly string XmlNamespaceOld = "http://schemas" + ".hu/ContentRepository/SearchExpression";
        public static readonly string XmlNamespace = "http://schemas.com/ContentRepository/SearchExpression";
        private const string NodeQuerySchemaManifestResourceName = "ContentRepository.Storage.Search.QuerySchema.xsd";

        private static Dictionary<string, NodeQueryTemplateReplacer> _templateResolvers;
        private static Dictionary<string, NodeQueryTemplateReplacer> TemplateResolvers
        {
            get
            {
                if (_templateResolvers == null)
                    _templateResolvers = NodeQueryTemplateReplacer.DiscoverTemplateReplacers();
                return _templateResolvers;
            }
        }

        //================================================================================= Fields

        private int _skip;
        private int _pageSize;
        private int _top;// = 0;
        private IUser _user;
        private List<SearchOrder> _orders;
        private int _resultCount;

        //================================================================================= Properties

        /// <summary>
        /// <para>Gets the maximum size of a result page. Can be used when no paging is needed.</para>
        /// <para>In the current implementation it's equals to <see cref="Int32.MaxValue"/>.</para>
        /// </summary>
        public static int UnlimitedPageSize
        {
            get { return int.MaxValue; }
        }

        private static object _evaluatorLock = new object();
        private static object _jsEvaluator;
        private static object JsEvaluator
        {
            get
            {
                if (_jsEvaluator == null)
                {
                    lock (_evaluatorLock)
                    {
                        if (_jsEvaluator == null)
                        {
                            _jsEvaluator = CreateJsEvaluator();
                        }
                    }
                }
                return (_jsEvaluator);
            }
        }

        /// <summary>
        /// <para>Gets or sets the start index of results. This property - combined with the <see cref="PageSize" /> property - can be used for paging.</para>
        /// <para>Please note that this is an item index, not a page index. For example, it you query returns 3 items - "Alpha", "Beta", "Gamma", and the StartIndex is set to 3, your query will return only one result, "Gamma".</para>
        /// <para>The <c>StartIndex</c> must be greater than 0.</para>
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">The StartIndex must be grater than 0, otherwise ArgumentOutOfRangeException thrown.</exception>
        [Obsolete("Use Skip instead. Be aware that StartIndex is 1-based but Skip is 0-based.")]
        public int StartIndex
        {
            get { return Skip + 1; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", value, SR.Exceptions.Search.Msg_StartIndexOutOfRange);
                Skip = Math.Min(0, value - 1);
            }
        }

        /// <summary>
        /// <para>Gets or sets the number of skipped result items. Combined with the <see cref="Top" /> property - can be used for paging.</para>
        /// <para>The <c>Skip</c> must be greater than 0.</para>
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">The StartIndex must be grater than 0, otherwise ArgumentOutOfRangeException thrown.</exception>
        public int Skip
        {
            get { return _skip; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", value, SR.Exceptions.Search.Msg_SkipOutOfRange);

                _skip = value;
            }
        }

        /// <summary>
        /// <para>Gets or sets the top of results.</para>
        /// <para>The <c>Top</c> must be greater than 0.</para>
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">The StartIndex must be grater than 0, otherwise ArgumentOutOfRangeException thrown.</exception>
        public int Top
        {
            get { return _top; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", value, SR.Exceptions.Search.Msg_TopOutOfRange);
                _top = value;
            }
        }

        /// <summary>
        /// <para>Gets or sets the size of the result page (the maximum number of the result the query will return).</para>
        /// <para>The default <c>PageSize</c> is <see cref="ContentRepository.Storage.Search.NodeQuery.UnlimitedPageSize">UnlimitedPageSize</see>. Its value must be greater than zero and less or equal to <c>UnlimitedPageSize</c>.</para>
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">The PageSize must be grater than 0, otherwise ArgumentOutOfRangeException thrown.</exception>
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", value, SR.Exceptions.Search.Msg_PageSizeOutOfRange);
                _pageSize = value;
            }
        }
        public IUser User
        {
            get { return _user; }
            set { _user = value; }
        }
        public List<SearchOrder> Orders
        {
            get { return _orders; }
        }
        public int ResultCount
        {
            get { return _resultCount; }
        }

        //================================================================================= Construction

        /// <summary>
        /// <para>Creates a new <c>NodeQuery</c> instance with the default <c>PageSize</c> (unlimited), <c>StartIndex</c> (1) and <c>Order</c> (undetermined)</para>
        /// </summary>
        public NodeQuery()
            : base(ChainOperator.And)
        {
            Initialize();
        }
        public NodeQuery(params Expression[] expressions)
            : base(ChainOperator.And, expressions)
        {
            Initialize();
        }
        public NodeQuery(ChainOperator operatorType, params Expression[] expressions)
            : base(operatorType, expressions)
        {
            Initialize();
        }
        private void Initialize()
        {
            _pageSize = UnlimitedPageSize;
            _orders = new List<SearchOrder>();
        }

        //================================================================================= Methods

        //public string Compile(out NodeQueryParameter[] parameters)
        //{
        //    var compiler = DataProvider.Current.CreateNodeQueryCompiler();
        //    return compiler.Compile(this, out parameters);
        //}

        /// <summary>
        /// <para>Executes the query, loads and returns the result nodes in a list.</para>
        /// <para>The <c>Execute()</c> method can be called multiple time, for example after increasing the <c>StartIndex</c> property.</para>
        /// </summary>
        /// <returns>A NodeList object that contains the result nodes.</returns>
        public NodeQueryResult Execute()
        {
            return Execute(ExecutionHint.None);
        }
        public NodeQueryResult Execute(ExecutionHint hint)
        {
            switch (hint)
            {
                case ExecutionHint.None:
                    if (IsRelationalEngineQuery())
                        return ExecuteRelationalEngineQuery();
                    return ExecuteIndexedEngineQuery();
                case ExecutionHint.ForceRelationalEngine:
                    return ExecuteRelationalEngineQuery();
                case ExecutionHint.ForceIndexedEngine:
                    return ExecuteIndexedEngineQuery();
                default:
                    throw new NotImplementedException();
            }
        }
        private NodeQueryResult ExecuteIndexedEngineQuery()
        {
            if (!StorageContext.Search.IsOuterEngineEnabled)
                throw new InvalidOperationException("Outer indexing engine is not present or disabled.");
            var idArray = IndexedSearchEngine.Execute(this);
            var result = new NodeQueryResult(new NodeList<Node>(idArray));
            return result;
        }
        private NodeQueryResult ExecuteRelationalEngineQuery()
        {
            throw new NotSupportedException();

            //var tokens = DataProvider.Current.ExecuteQuery(this);
            //_resultCount = tokens.Count;

            //var idList = from token in tokens select token.NodeId;
            //return new NodeQueryResult(new NodeList<Node>(idList));
        }
        ///// <summary>
        ///// DEFERRED
        ///// <para>Executes the query, but does not load the result nodes. This method returns only the <see cref="NodeToken"/>s.</para>
        ///// <para>The <c>NodeToken</c> contains some basic information about the node, and the node can be loaded by its token.</para>
        ///// </summary>
        ///// <returns>A <c>NodeToken</c> array that contains the result tokens.</returns>
        //[Obsolete("Use Execute method instead.")]
        //public NodeToken[] ExecuteToTokens()
        //{
        //    List<NodeToken> tokens = DataProvider.Current.ExecuteQuery(this);
        //    _resultCount = tokens.Count;
        //    return tokens.ToArray();
        //}

        internal override void WriteXml(System.Xml.XmlWriter writer)
        {
            base.WriteXml(writer);

            //==== Order
            if (_orders.Count > 0)
            {
                writer.WriteStartElement("Orders", NodeQuery.XmlNamespace);
                foreach (SearchOrder order in _orders)
                {
                    writer.WriteStartElement("Order", NodeQuery.XmlNamespace);
                    order.PropertyToOrder.WriteXml(writer);
                    if (order.Direction == OrderDirection.Desc)
                        writer.WriteAttributeString("direction", "desc");
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

            //==== Page
            if (StartIndex != 1 && _pageSize != UnlimitedPageSize)
            {
                writer.WriteStartElement("Page", NodeQuery.XmlNamespace);
                if (StartIndex != 1)
                    writer.WriteAttributeString("startIndex", StartIndex.ToString(CultureInfo.InvariantCulture));
                if (_pageSize != UnlimitedPageSize)
                    writer.WriteAttributeString("pageSize", _pageSize.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }
        }

        //--------------------------------------------------------------------------------- IEnumerable<Expression> Members

        public IEnumerator<Expression> GetEnumerator()
        {
            return new ExpressionEnumerator(this);
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        //--------------------------------------------------------------------------------- FromXml


        /// <summary>
        /// <para>Creates a NodeQuery instace from an XML formatted query.</para>
        /// <para>The XML can be created by the <c>ToXml()</c> or <c>WriteXml()</c> methods.</para>
        /// </summary>
        /// <param name="query">A string that contains an XML that represents a <c>NodeQuery</c>.</param>
        /// <returns>A <c>NodeQuery</c> instance that was built upon the XML.</returns>
        public static NodeQuery Parse(string query)
        {
            return Parse(query, NodeTypeManager.Current);
        }
        private static NodeQuery Parse(string query, SchemaRoot schema)
        {
            if (RepositoryConfiguration.BackwardCompatibilityXmlNamespaces)
                query = query.Replace(NodeQuery.XmlNamespaceOld, NodeQuery.XmlNamespace);

            query = DateTimeParser.GetDateTimeModifiedQuery(query);
            query = ReplaceJScriptTags(query);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(query);

            NodeQueryTemplateReplacer.ReplaceTemplates(doc, TemplateResolvers);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("x", NodeQuery.XmlNamespace);

            CheckValidation(doc);

            //-- Parse root chain
            var rootChain = doc.DocumentElement.SelectSingleNode("x:*[1]", nsmgr);
            var rootChainType = rootChain.LocalName == "And" ? ChainOperator.And : ChainOperator.Or;

            NodeQuery nodeQuery = new NodeQuery(rootChainType);

            //-- Parse expression tree
            foreach (XmlNode node in rootChain.SelectNodes("x:*", nsmgr))
                nodeQuery.Add(ParseExpression(node, nsmgr, schema));

            //-- Parse orders
            foreach (XmlNode node in doc.DocumentElement.SelectNodes("x:Orders/x:Order", nsmgr))
                nodeQuery.Orders.Add(ParseOrder(node, nsmgr, schema));

            //-- Parse page
            XmlNode pageNode = doc.DocumentElement.SelectSingleNode("x:Page", nsmgr);
            if (pageNode != null)
            {
                //<Page startIndex="987" pageSize="123" />
                XmlAttribute attr = pageNode.Attributes["startIndex"];
                if (attr != null)
                    nodeQuery.StartIndex = Convert.ToInt32(attr.Value, CultureInfo.InvariantCulture);
                attr = pageNode.Attributes["pageSize"];
                if (attr != null)
                    nodeQuery.PageSize = Convert.ToInt32(attr.Value, CultureInfo.InvariantCulture);
            }
            //-- Parse top
            XmlNode topNode = doc.DocumentElement.SelectSingleNode("x:Top", nsmgr);
            if (topNode != null)
            {
                //<Top>5</Top>
                nodeQuery.Top = Convert.ToInt32(topNode.InnerText, CultureInfo.InvariantCulture);
            }

            return nodeQuery;
        }

        public static string ReplaceJScriptTags(string input)
        {
            if (input.IndexOf("[jScript]", StringComparison.Ordinal) == -1)
            {
                return (input);
            }
            Regex regex = new Regex(@"\[jScript\](.+?)\[\/jScript\]");
            return regex.Replace(input, new MatchEvaluator(ReplaceJScriptTags));
        }
        public static string ReplaceJScriptTags(Match match)
        {
            string input = match.Value;
            input = input.Replace("[jScript]", "");
            input = input.Replace("[/jScript]", "");
            input = input.Replace("´", "'");

            try
            {
                input = JsEvaluator.GetType().InvokeMember("Eval", System.Reflection.BindingFlags.InvokeMethod, null, JsEvaluator, new object[] { input }).ToString();
            }
            catch (Exception ex) //logged
            {
                Logger.WriteException(ex);
                //Assembly a = Assembly.LoadWithPartialName("Storage");
                //Type t = a.GetType("Services.Instrumentation.ExceptionLogger");
                //var r = Activator.CreateInstance(t, new object[] { ex });
                //MethodInfo m = t.GetMethod("LogToEventLog", BindingFlags.Public | BindingFlags.Instance);
                //m.Invoke(r, null);
                if (input.IndexOf("DateTime.UtcNow.ToString(\"yyyy'/'MM'/'dd\")", StringComparison.Ordinal) != -1) // DateTime.UtcNow.ToString("yyyy´/´MM´/´dd")
                {
                    return (DateTime.UtcNow.ToString("yyyy'/'MM'/'dd"));
                }
            }
            return (input);
        }

        private static Expression ParseExpression(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            switch (node.LocalName)
            {
                case "And":
                case "Or":
                    return ParseExpressionList(node, nsmgr, schema);
                case "Not":
                    return ParseNotExpression(node, nsmgr, schema);
                case "String":
                    return ParseStringExpression(node, nsmgr, schema);
                case "Int":
                    return ParseIntExpression(node, nsmgr, schema);
                case "DateTime":
                    return ParseDateTimeExpression(node, nsmgr, schema);
                case "Currency":
                    return ParseCurrencyExpression(node, nsmgr, schema);
                case "Reference":
                    return ParseReferenceExpression(node, nsmgr, schema);
                case "Type":
                    return ParseTypeExpression(node, nsmgr, schema);
                case "FullText":
                    return ParseFullTextExpression(node, nsmgr, schema);
                default:
                    throw new NotSupportedException(String.Concat("Unrecognized expression type: ", node.LocalName));
            }
        }
        private static Expression ParseExpressionList(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            ChainOperator op = (ChainOperator)Enum.Parse(typeof(ChainOperator), node.LocalName);
            ExpressionList exprList = new ExpressionList(op);
            foreach (XmlNode subNode in node.SelectNodes("x:*", nsmgr))
                exprList.Add(ParseExpression(subNode, nsmgr, schema));
            return exprList;
        }
        private static Expression ParseNotExpression(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            XmlNode subNode = node.SelectSingleNode("x:*[1]", nsmgr);
            if (subNode == null)
                throw new InvalidOperationException("NotExpression cannot be empty.");
            return new NotExpression(ParseExpression(subNode, nsmgr, schema));
        }
        private static Expression ParseStringExpression(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            StringOperator op = ParseStringOperator(node);
            object leftValue = ParseLeftValue(node, nsmgr, schema);
            object rightValue = ParseRightValue(node, nsmgr, schema);

            PropertyType leftProp = leftValue as PropertyType;
            if (leftProp != null)
            {
                if (rightValue == null)
                    return new StringExpression(leftProp, op, (string)null);

                PropertyType rightProp = rightValue as PropertyType;
                if (rightProp != null)
                    return new StringExpression(leftProp, op, rightProp);

                string rightString = rightValue as String;
                if (rightString != null)
                    return new StringExpression(leftProp, op, rightString);

                try
                {
                    StringAttribute rightAttr = (StringAttribute)rightValue;
                    return new StringExpression(leftProp, op, rightAttr);
                }
                catch (Exception e) //rethrow
                {
                    throw new ApplicationException(String.Concat("Unrecognized StringAttribute: '", rightValue, "'. Source: ", node.OuterXml), e);
                }
            }
            else
            {
                StringAttribute leftAttr = (StringAttribute)leftValue;
                if (rightValue == null)
                    return new StringExpression(leftAttr, op, (string)null);

                PropertyType rightProp = rightValue as PropertyType;
                if (rightProp != null)
                    return new StringExpression(leftAttr, op, rightProp);

                string rightString = rightValue as String;
                if (rightString != null)
                    return new StringExpression(leftAttr, op, rightString);

                try
                {
                    StringAttribute rightAttr = (StringAttribute)rightValue;
                    return new StringExpression(leftAttr, op, rightAttr);
                }
                catch (Exception e) //rethrow
                {
                    throw new ApplicationException(String.Concat("Unrecognized StringAttribute: '", rightValue, "'. Source: ", node.OuterXml), e);
                }
            }
        }
        private static Expression ParseIntExpression(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            ValueOperator op = ParseValueOperator(node);
            object leftValue = ParseLeftValue(node, nsmgr, schema);
            object rightValue = ParseRightValue(node, nsmgr, schema);

            PropertyType leftProp = leftValue as PropertyType;
            if (leftProp != null)
            {
                if (rightValue == null)
                    return new IntExpression(leftProp, op, (int?)null);

                PropertyType rightProp = rightValue as PropertyType;
                if (rightProp != null)
                    return new IntExpression(leftProp, op, rightProp);

                string rightString = rightValue as String;
                if (rightString != null)
                    return new IntExpression(leftProp, op, XmlConvert.ToInt32(rightString));

                try
                {
                    IntAttribute rightAttr = (IntAttribute)rightValue;
                    return new IntExpression(leftProp, op, rightAttr);
                }
                catch (Exception e) //rethrow
                {
                    throw new ApplicationException(String.Concat("Unrecognized IntAttribute: '", rightValue, "'. Source: ", node.OuterXml), e);
                }
            }
            else
            {
                IntAttribute leftAttr = (IntAttribute)leftValue;
                if (rightValue == null)
                    return new IntExpression(leftAttr, op, (int?)null);

                PropertyType rightProp = rightValue as PropertyType;
                if (rightProp != null)
                    return new IntExpression(leftAttr, op, rightProp);

                string rightString = rightValue as String;
                if (rightString != null)
                    return new IntExpression(leftAttr, op, XmlConvert.ToInt32(rightString));

                try
                {
                    IntAttribute rightAttr = (IntAttribute)rightValue;
                    return new IntExpression(leftAttr, op, rightAttr);
                }
                catch (Exception e) //rethrow
                {
                    throw new ApplicationException(String.Concat("Unrecognized IntAttribute: '", rightValue, "'. Source: ", node.OuterXml), e);
                }
            }

        }
        private static Expression ParseDateTimeExpression(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            ValueOperator op = ParseValueOperator(node);
            object leftValue = ParseLeftValue(node, nsmgr, schema);
            object rightValue = ParseRightValue(node, nsmgr, schema);

            PropertyType leftProp = leftValue as PropertyType;
            if (leftProp != null)
            {
                if (rightValue == null)
                    return new DateTimeExpression(leftProp, op, (DateTime?)null);

                PropertyType rightProp = rightValue as PropertyType;
                if (rightProp != null)
                    return new DateTimeExpression(leftProp, op, rightProp);

                string rightString = rightValue as String;
                if (rightString != null)
                    return new DateTimeExpression(leftProp, op, XmlConvert.ToDateTime(rightString, XmlDateTimeSerializationMode.Unspecified));

                try
                {
                    DateTimeAttribute rightAttr = (DateTimeAttribute)rightValue;
                    return new DateTimeExpression(leftProp, op, rightAttr);
                }
                catch (Exception e) //rethrow
                {
                    throw new ApplicationException(String.Concat("Unrecognized DateTimeAttribute: '", rightValue, "'. Source: ", node.OuterXml), e);
                }
            }
            else
            {
                DateTimeAttribute leftAttr = (DateTimeAttribute)leftValue;
                if (rightValue == null)
                    return new DateTimeExpression(leftAttr, op, (DateTime?)null);

                PropertyType rightProp = rightValue as PropertyType;
                if (rightProp != null)
                    return new DateTimeExpression(leftAttr, op, rightProp);

                string rightString = rightValue as String;
                if (rightString != null)
                    return new DateTimeExpression(leftAttr, op, XmlConvert.ToDateTime(rightString, XmlDateTimeSerializationMode.Unspecified));

                try
                {
                    DateTimeAttribute rightAttr = (DateTimeAttribute)rightValue;
                    return new DateTimeExpression(leftAttr, op, rightAttr);
                }
                catch (Exception e) //rethrow
                {
                    throw new ApplicationException(String.Concat("Unrecognized DateTimeAttribute: '", rightValue, "'. Source: ", node.OuterXml), e);
                }
            }
        }
        private static Expression ParseCurrencyExpression(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            ValueOperator op = ParseValueOperator(node);
            object leftValue = ParseLeftValue(node, nsmgr, schema);
            object rightValue = ParseRightValue(node, nsmgr, schema);

            PropertyType leftProp = leftValue as PropertyType;
            if (leftProp != null)
            {
                if (rightValue == null)
                    return new CurrencyExpression(leftProp, op, (decimal?)null);

                PropertyType rightProp = rightValue as PropertyType;
                if (rightProp != null)
                    return new CurrencyExpression(leftProp, op, rightProp);

                string rightString = rightValue as String;
                if (rightString != null)
                    return new CurrencyExpression(leftProp, op, XmlConvert.ToDecimal(rightString));
            }
            throw new NotSupportedException(String.Concat("Unknown property in a Currency expression: ", node.OuterXml));
        }
        private static Expression ParseReferenceExpression(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            XmlAttribute attr;

            bool existenceOnly = false;
            attr = node.Attributes["existenceOnly"];
            if (attr != null)
                existenceOnly = attr.Value == "yes";

            object leftValue = ParseLeftValue(node, nsmgr, schema);
            PropertyType leftProp = leftValue as PropertyType;
            Node referencedNode = null;
            attr = node.Attributes["referencedNodeId"];
            if (attr != null)
            {
                int referencedNodeId = XmlConvert.ToInt32(attr.Value);
                referencedNode = Node.LoadNode(referencedNodeId);
                if (referencedNode == null)
                    throw new ContentNotFoundException(String.Concat("Referred node is not found: ", referencedNodeId));
                if (leftProp != null)
                {
                    return new ReferenceExpression(leftProp, referencedNode);
                }
                ReferenceAttribute leftAttr = (ReferenceAttribute)leftValue;
                return new ReferenceExpression(leftAttr, referencedNode);
            }
            if (node.SelectSingleNode("x:*[1]", nsmgr) == null)
            {
                if (leftProp != null)
                {
                    if (existenceOnly)
                        return new ReferenceExpression(leftProp);
                    return new ReferenceExpression(leftProp, (Node)null);
                }
                ReferenceAttribute leftAttr = (ReferenceAttribute)leftValue;
                if (existenceOnly)
                    return new ReferenceExpression(leftAttr);
                return new ReferenceExpression(leftAttr, (Node)null);
            }

            object rightValue = ParseRightValue(node, nsmgr, schema);

            Expression rightExpr = rightValue as Expression;
            if (leftProp != null)
            {
                if (rightExpr != null)
                    return new ReferenceExpression(leftProp, rightExpr);
            }
            else
            {
                ReferenceAttribute leftAttr = (ReferenceAttribute)leftValue;
                if (rightExpr != null)
                    return new ReferenceExpression(leftAttr, rightExpr);
            }
            throw new ApplicationException(String.Concat("Unrecognized Reference expression: ", node.OuterXml));
        }
        private static Expression ParseTypeExpression(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            string nodeTypeName = node.Attributes["nodeType"].Value;
            NodeType nodeType = schema.NodeTypes[nodeTypeName];
            if (nodeType == null)
                throw new ApplicationException(String.Concat("Unknown NodeType: '", nodeTypeName, "'. Source: ", node.OuterXml));
            bool exactMatch = false;
            XmlAttribute attr = node.Attributes["exactMatch"];
            if (attr != null)
                exactMatch = attr.Value == "yes";
            return new TypeExpression(nodeType, exactMatch);
        }
        private static Expression ParseFullTextExpression(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            string nodeTypeName = node.InnerXml;
            return new SearchExpression(nodeTypeName);
        }
        private static SearchOrder ParseOrder(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            OrderDirection direction = OrderDirection.Asc;
            XmlAttribute attr = node.Attributes["direction"];
            if (attr != null)
                if (attr.Value == "desc")
                    direction = OrderDirection.Desc;
            object leftValue = ParseLeftValue(node, nsmgr, schema);
            PropertyType leftProp = leftValue as PropertyType;
            if (leftProp != null)
                return new SearchOrder(leftProp, direction);
            return new SearchOrder((NodeAttribute)leftValue, direction);
        }
        private static StringOperator ParseStringOperator(XmlNode node)
        {
            string opName = node.Attributes["op"].Value;
            try
            {
                return (StringOperator)Enum.Parse(typeof(StringOperator), opName);
            }
            catch (Exception e) //rethrow
            {
                throw new ApplicationException(String.Concat("Unrecognized StringOperator: '", opName, "'. Source: ", node.OuterXml), e);
            }
        }
        private static ValueOperator ParseValueOperator(XmlNode node)
        {
            string opName = node.Attributes["op"].Value;
            try
            {
                return (ValueOperator)Enum.Parse(typeof(ValueOperator), opName);
            }
            catch (Exception e) //rethrow
            {
                throw new ApplicationException(String.Concat("Unrecognized ValueOperator: '", opName, "'. Source: ", node.OuterXml), e);
            }
        }
        private static object ParseLeftValue(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            XmlAttribute attr = node.Attributes["property"];
            PropertyType propType = schema.PropertyTypes[attr.Value];
            if (propType != null)
                return propType;
            try
            {
                return Enum.Parse(typeof(NodeAttribute), attr.Value);
            }
            catch (Exception e) //rethrow
            {
                throw new ApplicationException(String.Concat("Unknown Property: '", attr.Value, "'. Source: ", node.OuterXml), e);
            }
        }
        private static object ParseRightValue(XmlNode node, XmlNamespaceManager nsmgr, SchemaRoot schema)
        {
            XmlNode subNode = node.SelectSingleNode("x:*", nsmgr);
            if (subNode == null)
                return node.InnerText;

            switch (subNode.LocalName)
            {
                case "Property":
                    //return schema.PropertyTypes[];
                    string name = subNode.Attributes["name"].Value;
                    PropertyType propType = schema.PropertyTypes[name];
                    if (propType != null)
                        return propType;
                    try
                    {
                        return Enum.Parse(typeof(NodeAttribute), name);
                    }
                    catch (Exception e) //rethrow
                    {
                        throw new ApplicationException(String.Concat("Unknown Property: '", name, "'. Source: ", node.OuterXml), e);
                    }
                case "NullValue":
                    return null;
                default:
                    return ParseExpression(subNode, nsmgr, schema);
            }
        }

        private static void CheckValidation(IXPathNavigable xml)
        {
            var schema = XmlValidator.LoadFromManifestResource(Assembly.GetExecutingAssembly(), NodeQuerySchemaManifestResourceName);
            if (!schema.Validate(xml))
            {
                if (schema.Errors.Count == 0)
                    throw new InvalidOperationException(SR.Exceptions.Search.Msg_InvalidNodeQueryXml);
                else
                    throw new InvalidOperationException(String.Concat(
                        SR.Exceptions.Search.Msg_InvalidNodeQueryXml, ": ", schema.Errors[0].Exception.Message),
                        schema.Errors[0].Exception);
            }

        }

        private static object CreateJsEvaluator()
        {
            Microsoft.JScript.JScriptCodeProvider jsCodeProvider = new Microsoft.JScript.JScriptCodeProvider();
            CompilerParameters compilerParam = new CompilerParameters();
            compilerParam.ReferencedAssemblies.Add("System.dll");
            compilerParam.ReferencedAssemblies.Add("System.Data.dll");
            compilerParam.ReferencedAssemblies.Add("System.Xml.dll");
            compilerParam.ReferencedAssemblies.Add("System.Web.dll");

            compilerParam.CompilerOptions = "/t:library";
            compilerParam.GenerateInMemory = true;

            string JScriptSource =
            @"
            import System;
            import System.Web;
            package Evaluator
            {
                class JsEvaluator
                {
                    public function Eval(expr : String) : String
                    {
                        return eval(expr, ""unsafe"");
                    }
                }
            }";

            CompilerResults compilerResult = jsCodeProvider.CompileAssemblyFromSource(compilerParam, JScriptSource);

            if (compilerResult.Errors.Count > 0)
            {
                string errMsg = String.Concat("Compiling JScript code failed and threw the exception: ", compilerResult.Errors[0].ErrorText);
                throw new ApplicationException(errMsg);
            }
            Assembly assembly = compilerResult.CompiledAssembly;
            var jsEvaluatorType = assembly.GetType("Evaluator.JsEvaluator");
            var jsEvaluator = Activator.CreateInstance(jsEvaluatorType);
            return jsEvaluator;
        }

        public static void InitTemplateResolvers()
        {
            var resolvers = TemplateResolvers;
        }
        //============================================================================================================================ NodeQuery replacement

        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static int InstanceCount(NodeType nodeType, bool exactType)
        {
            return DataProvider.Current.InstanceCount(exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray());
        }

        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static NodeQueryResult QueryChildren(string parentPath)
        {
            if (parentPath == null)
                throw new ArgumentNullException("parentPath");
            var head = NodeHead.Get(parentPath);
            if (head == null)
                throw new InvalidOperationException("Node does not exist: " + parentPath);

            var ids = DataProvider.Current.GetChildrenIdentfiers(head.Id);
            return new NodeQueryResult(new NodeList<Node>(ids));
        }

        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static NodeQueryResult QueryChildren(int parentId)
        {
            if (parentId <= 0)
                throw new InvalidOperationException("Parent node is not saved");

            var ids = DataProvider.Current.GetChildrenIdentfiers(parentId);
            return new NodeQueryResult(new NodeList<Node>(ids));
        }

        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static NodeQueryResult QueryNodesByPath(string pathStart, bool orderByPath)
        {
            if (pathStart == null)
                throw new ArgumentNullException("pathStart");
            IEnumerable<int> ids = DataProvider.Current.QueryNodesByPath(pathStart, orderByPath);
            return new NodeQueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static NodeQueryResult QueryNodesByType(NodeType nodeType, bool exactType)
        {
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");
            var typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();
            IEnumerable<int> ids = DataProvider.Current.QueryNodesByType(typeIds);
            return new NodeQueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static NodeQueryResult QueryNodesByTypeAndName(NodeType nodeType, bool exactType, string name)
        {
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");
            var typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();
            IEnumerable<int> ids = DataProvider.Current.QueryNodesByTypeAndPathAndName(typeIds, (string[])null, false, name);
            return new NodeQueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static NodeQueryResult QueryNodesByTypeAndPath(NodeType nodeType, bool exactType, string pathStart, bool orderByPath)
        {
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");
            if (pathStart == null)
                throw new ArgumentNullException("pathStart");
            var typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();
            IEnumerable<int> ids = DataProvider.Current.QueryNodesByTypeAndPath(typeIds, pathStart, orderByPath);
            return new NodeQueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static NodeQueryResult QueryNodesByTypeAndPath(NodeType nodeType, bool exactType, string[] pathStart, bool orderByPath)
        {
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");
            if (pathStart == null)
                throw new ArgumentNullException("pathStart");
            var typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();
            IEnumerable<int> ids = DataProvider.Current.QueryNodesByTypeAndPath(typeIds, pathStart, orderByPath);
            return new NodeQueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static NodeQueryResult QueryNodesByTypeAndPathAndName(NodeType nodeType, bool exactType, string pathStart, bool orderByPath, string name)
        {
            int[] typeIds = null;
            if (nodeType != null)
                typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();
            IEnumerable<int> ids = DataProvider.Current.QueryNodesByTypeAndPathAndName(typeIds, pathStart, orderByPath, name);
            return new NodeQueryResult(new NodeList<Node>(ids));
        }

        public static NodeQueryResult QueryNodesByTypeAndPathAndName(IEnumerable<NodeType> nodeTypes, bool exactType, string pathStart, bool orderByPath, string name)
        {
            int[] typeIds = null;
            if (nodeTypes != null)
            {
                var idList = new List<int>();

                if (exactType)
                {
                    idList = nodeTypes.Select(nt => nt.Id).ToList();
                }
                else
                {
                    foreach (var nodeType in nodeTypes)
                    {
                        idList.AddRange(nodeType.GetAllTypes().ToIdArray());
                    }
                }

                if (idList.Count > 0)
                    typeIds = idList.ToArray();
            }

            var ids = DataProvider.Current.QueryNodesByTypeAndPathAndName(typeIds, pathStart, orderByPath, name);
            return new NodeQueryResult(new NodeList<Node>(ids));
        }

        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static NodeQueryResult QueryNodesByTypeAndPathAndProperty(NodeType nodeType, bool exactType, string pathStart, bool orderByPath, List<QueryPropertyData> properties)
        {
            int[] typeIds = null;
            if (nodeType != null)
                typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();

            var ids = DataProvider.Current.QueryNodesByTypeAndPathAndProperty(typeIds, pathStart, orderByPath, properties);

            return new NodeQueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static NodeQueryResult QueryNodesByReference(string referenceName, int referredNodeId)
        {
            return QueryNodesByReferenceAndType(referenceName, referredNodeId, null, false);
        }
        /// <summary>
        /// DO NOT USE THIS METHOD DIRECTLY IN YOUR CODE.
        /// </summary>
        public static NodeQueryResult QueryNodesByReferenceAndType(string referenceName, int referredNodeId, NodeType nodeType, bool exactType)
        {
            int[] typeIds = null;
            if (nodeType != null)
                typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();

            var ids = DataProvider.Current.QueryNodesByReferenceAndType(referenceName, referredNodeId, typeIds);

            return new NodeQueryResult(new NodeList<Node>(ids));
        }
    }
}

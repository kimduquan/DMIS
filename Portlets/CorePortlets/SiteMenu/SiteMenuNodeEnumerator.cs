using System;
using System.Collections.Generic;
using ContentRepository.Storage;
using ContentRepository.Storage.Search;
using Search;

namespace Portal.Portlets
{
    public class SiteMenuNodeEnumerator : NodeEnumerator
    {
        public static IEnumerable<Node> GetNodes(string path, ExecutionHint hint,
            NodeQuery filter, int? depth, string contextPath, bool getContextChildren)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            return new SiteMenuNodeEnumerator(path, hint, filter, depth, contextPath, getContextChildren);
        }

        public static IEnumerable<Node> GetNodes(string path, ExecutionHint hint,
            string filter, int? depth, string contextPath, bool getContextChildren)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            return new SiteMenuNodeEnumerator(path, hint, filter, depth, contextPath, getContextChildren);
        }

        //================================================================== 

        private readonly string _contextPath;
        private readonly bool _getContextChildren;
        private readonly string _childrenFilter;

        protected SiteMenuNodeEnumerator(string path, ExecutionHint executionHint,
            NodeQuery filter, int? depth, string contextPath, bool getContextChildren)
            : base(path, executionHint, filter, depth)
        {
            _contextPath = contextPath;
            _getContextChildren = getContextChildren;
        }

        protected SiteMenuNodeEnumerator(string path, ExecutionHint executionHint,
            string filter, int? depth, string contextPath, bool getContextChildren)
            : base(path, executionHint, null, depth)
        {
            _contextPath = contextPath;
            _getContextChildren = getContextChildren;
            _childrenFilter = filter;
        }

        protected override NodeQueryResult QueryChildrenFromLucene(int thisId)
        {
            if (string.IsNullOrEmpty(_childrenFilter))
            {
                return base.QueryChildrenFromLucene(thisId);
            }
            else
            {
                //We need to apply a query filter. Execute a content 
                //query and create a node query result on-the-fly.
                var query = ContentQuery.CreateQuery("+ParentId:@0", null, thisId);

                if (!string.IsNullOrEmpty(_childrenFilter))
                    query.AddClause(_childrenFilter);

                return new NodeQueryResult(query.ExecuteToIds(ExecutionHint.ForceIndexedEngine));
            }
        }

        protected override bool MoveToFirstChild()
        {
            if (!string.IsNullOrEmpty(_contextPath))
            {
                if (!_contextPath.StartsWith(CurrentNode.Path))
                    return false;

                if (!_getContextChildren && _contextPath.Equals(CurrentNode.Path))
                    return false;
            }

            return base.MoveToFirstChild();
        }
    }
}

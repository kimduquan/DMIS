using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Schema;
using ContentRepository.Storage;

namespace ContentRepository
{
    [ContentHandler]
    class NewsArticle : GenericContent, IFolder
    {

        public NewsArticle(Node parent) : this(parent, null) { }
		public NewsArticle(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected NewsArticle(NodeToken nt) : base(nt) { }

        #region IFolder Members

        public IEnumerable<ContentRepository.Storage.Node> Children
        {
            get { return this.GetChildren(); }
        }

        public int ChildCount
        {
            get { return this.GetChildCount(); }
        }

        #endregion
    }
}

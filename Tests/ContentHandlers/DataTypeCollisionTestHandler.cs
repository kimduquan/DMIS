using System;
using  ContentRepository.Schema;
using ContentRepository.Storage;

namespace ContentRepository.Tests.ContentHandlers
{
	[ContentHandler]
	public class DataTypeCollisionTestHandler : Node
	{
		public DataTypeCollisionTestHandler(Node parent) : base(parent) { }
		public DataTypeCollisionTestHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
		protected DataTypeCollisionTestHandler(NodeToken token) : base(token) { }

        public override bool IsContentType { get { return false; } }

		[RepositoryProperty]
		public string TestString
		{
			get { return this.GetProperty<string>("ContentRepository.Tests.ContentHandlers.DataTypeCollisionTestHandler.TestString"); }
			set { this["ContentRepository.Tests.ContentHandlers.DataTypeCollisionTestHandler.TestString"] = value; }
		}
	}
}

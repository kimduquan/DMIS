using System;
using  ContentRepository.Schema;
using ContentRepository.Storage;

namespace ContentRepository.Tests.ContentHandlers
{
	[ContentHandler]
	public class TestNode : Node
	{
		public static string ContentTypeDefinition = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='RepositoryTest_TestNode' handler='ContentRepository.Tests.ContentHandlers.TestNode' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
	<Fields />
</ContentType>
";

		public TestNode(Node parent) : base(parent, "RepositoryTest_TestNode") { }
		protected TestNode(NodeToken token) : base(token) { }

        public override bool IsContentType { get { return false; } }

		[RepositoryProperty("TestInt1")]
		public int TestInt
		{
			get { return this.GetProperty<int>("TestInt1"); }
			set { this["TestInt1"] = value; }
		}
		[RepositoryProperty(RepositoryDataType.Int)]
		public string TestInt2
		{
			get { return this.GetProperty<int>("TestInt2").ToString(); }
			set { this["TestInt2"] = Convert.ToInt32(value); }
		}
	}
}

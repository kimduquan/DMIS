using System;
using  ContentRepository.Schema;
using ContentRepository.Storage;

namespace ContentRepository.Tests.ContentHandlers
{
	[ContentHandler]
	public class TestNode2 : TestNode
	{
		public static new string ContentTypeDefinition = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='RepositoryTest_TestNode2' handler='ContentRepository.Tests.ContentHandlers.TestNode2' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
	<Fields />
</ContentType>
";

		public TestNode2(Node parent) : base(parent) { }
		public TestNode2(NodeToken token) : base(token) { }

		[RepositoryProperty]
		public string TestString
		{
			get { return this.GetProperty<string>("TestString"); }
			set { this["TestString"] = value; }
		}

		[RepositoryProperty]
		public int X
		{
			get { return this.GetProperty<int>("X"); }
			set { this["X"] = value; }
		}
		[RepositoryProperty]
		public int Y
		{
			get { return this.GetProperty<int>("Y"); }
			set { this["Y"] = value; }
		}
	}
}

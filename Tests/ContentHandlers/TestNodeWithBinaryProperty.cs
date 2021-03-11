using System;
using ContentRepository.Storage;
using  ContentRepository.Schema;

namespace ContentRepository.Tests.ContentHandlers
{
	[ContentHandler]
	class TestNodeWithBinaryProperty : Node
	{
		public static string ContentTypeDefinition =
			@"<?xml version='1.0' encoding='utf-8'?>
			<ContentType name='RepositoryTest_TestNodeWithBinaryProperty' handler='ContentRepository.Tests.ContentHandlers.TestNodeWithBinaryProperty' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
				<Fields />
			</ContentType>
			";

        public override bool IsContentType { get { return false; } }

		public TestNodeWithBinaryProperty(Node parent) : base(parent, "RepositoryTest_TestNodeWithBinaryProperty") { }
		protected TestNodeWithBinaryProperty(NodeToken token) : base(token) { }

		[RepositoryProperty]
		public string Note
		{
			get { return this.GetProperty<string>("ContentRepository.Tests.ContentHandlers.TestNodeWithBinaryProperty.Note"); }
			set { this["ContentRepository.Tests.ContentHandlers.TestNodeWithBinaryProperty.Note"] = value; }
		}
		[RepositoryProperty]
		public BinaryData FirstBinary
		{
			get { return this.GetBinary("ContentRepository.Tests.ContentHandlers.TestNodeWithBinaryProperty.FirstBinary"); }
			set { this.SetBinary("ContentRepository.Tests.ContentHandlers.TestNodeWithBinaryProperty.FirstBinary", value); }
		}
		[RepositoryProperty]
		public BinaryData SecondBinary
		{
			get { return this.GetBinary("ContentRepository.Tests.ContentHandlers.TestNodeWithBinaryProperty.SecondBinary"); }
			set { this.SetBinary("ContentRepository.Tests.ContentHandlers.TestNodeWithBinaryProperty.SecondBinary", value); }
		}

		public override string ToString()
		{
			return string.Concat((FirstBinary != null ? FirstBinary.FileName.FullFileName : "FirstBinary is NULL"), "/", (SecondBinary != null ? SecondBinary.FileName.FullFileName : "SecondBinary is NULL"), " (", Note, ")");
		}
	}
}

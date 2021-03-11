using System;
using System.Collections.Generic;
using System.Linq;

using  ContentRepository.Schema;
using ContentRepository.Storage;
using ContentRepository.Storage.Data;
using ContentRepository.Storage.Schema;

namespace ContentRepository.Tests.ContentHandlers
{
	[ContentHandler]
	public class NonGenericHandler : Node
	{
		public NonGenericHandler(Node parent) : base(parent) { }
		public NonGenericHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
		protected NonGenericHandler(NodeToken token) : base(token) { }

        public override bool IsContentType { get { return false; } }

		[RepositoryProperty("TestString")]
		public string TestString
		{
			get { return this.GetProperty<string>("TestString"); }
			set { this["TestString"] = value; }
		}

		public const string ContentTypeDefinition = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='NonGeneric' handler='ContentRepository.Tests.ContentHandlers.NonGenericHandler' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
	<DisplayName>NonGeneric [demo]</DisplayName>
	<Description>This is a demo NonGeneric node definition</Description>
	<Icon>icon.gif</Icon>
	<Fields>
		<Field name='Index' type='Integer' />
		<Field name='TestString' type='ShortText' />
	</Fields>
</ContentType>
";

	}
}
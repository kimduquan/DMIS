using System;
using ContentRepository.Storage;
using  ContentRepository.Schema;
using ContentRepository.Storage.Events;

namespace ContentRepository.Tests.ContentHandlers
{
	[ContentHandler]
	public class EventTestNode : Node
	{
		public static string DefaultNodeTypeName = "RepositoryTest_EventTestNode";
		public static string ContentTypeDefinition = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='RepositoryTest_EventTestNode' parentType='GenericContent' handler='ContentRepository.Tests.ContentHandlers.EventTestNode' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
	<Fields />
</ContentType>
";

		public EventTestNode(Node parent) : this(parent, DefaultNodeTypeName) { }
		public EventTestNode(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
		protected EventTestNode(NodeToken token) : base(token) { }

        public override bool IsContentType { get { return false; } }

		protected override void OnCreating(object sender, CancellableNodeEventArgs e)
		{
			if (e.SourceNode.Index == 0)
			{
				e.CancelMessage = "Index cannot be 0";
				e.Cancel = true;
			}
			base.OnCreating(sender, e);
		}
	}
}

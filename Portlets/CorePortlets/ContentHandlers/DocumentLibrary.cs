using System;
using ContentRepository.Storage;
using  ContentRepository.Schema;
using ContentRepository;

namespace Portal.Portlets.ContentHandlers
{
	[ContentHandler]
	public class DocumentLibrary : ContentList
	{
        public DocumentLibrary(Node parent) : this(parent, null) { }
		public DocumentLibrary(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
		protected DocumentLibrary(NodeToken nt) : base(nt) { }

		public override object GetProperty(string name)
		{
			switch (name)
			{
				default:
					return base.GetProperty(name);
			}
		}

		public override void SetProperty(string name, object value)
		{
			switch (name)
			{
				default:
					base.SetProperty(name, value);
					break;
			}
		}
	}
}
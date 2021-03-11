using System;
using System.Collections.Generic;
using System.Text;
using ContentRepository.Storage;
using  ContentRepository.Schema;

namespace ContentRepository
{
	[ContentHandler]
	public class PortalRoot : Folder
	{
		protected PortalRoot(NodeToken nt) : base(nt) { }
	}
}
using System;
using System.Collections.Generic;
using System.Text;
using  ContentRepository.Schema;
using ContentRepository.Storage.Security;

namespace ContentRepository.Fields
{
	[ShortName("Lock")]
	[DataSlot(0, RepositoryDataType.NotDefined, typeof(LockHandler))]
	[DefaultFieldSetting(typeof(NullFieldSetting))]
	[DefaultFieldControl("Portal.UI.Controls.ShortText")]
	public class LockField : Field
	{
		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
			throw new NotSupportedException("The ImportData operation is not supported on LockField.");
		}
	}
}
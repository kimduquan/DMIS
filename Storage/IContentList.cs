using System;
using System.Collections.Generic;
using System.Text;
using ContentRepository.Storage.Schema;

namespace ContentRepository.Storage
{
	public interface IContentList
	{
		ContentListType GetContentListType();
	}
}
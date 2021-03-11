using System;
using System.Collections.Generic;
using System.Text;
using ContentRepository.Storage;

namespace ContentRepository
{
	public interface IFile
	{
		BinaryData Binary { get; set;}
        long Size { get; }
        long FullSize { get; }
	}

}
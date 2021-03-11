using System;
using ContentRepository.Storage.Security;

namespace ContentRepository.Storage.Events
{
	public interface INodeEventArgs
	{
		Node SourceNode { get; }
		IUser User { get; }
		DateTime Time { get; }
	}
}
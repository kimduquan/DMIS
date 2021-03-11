using System.Collections.Generic;

namespace ContentRepository.Storage.Security
{
    public interface IGroup : ISecurityContainer
    {
        IEnumerable<Node> Members { get; }
    }
}
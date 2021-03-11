using System.Collections.Generic;
using ContentRepository.Storage;
using Search;

namespace ContentRepository
{
    public interface IFolder
    {
        IEnumerable<Node> Children { get; }
        int ChildCount { get; }

        QueryResult GetChildren(QuerySettings settings);
        QueryResult GetChildren(string text, QuerySettings settings);
    }
}
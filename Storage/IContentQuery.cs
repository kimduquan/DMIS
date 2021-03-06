using System.Collections.Generic;
using ContentRepository.Storage.Search;

namespace Search
{
    public interface IContentQuery
    {
        string Text { get; set; }
        int TotalCount { get; }

        QuerySettings Settings { get; set; }
        bool IsNodeQuery { get; }
        bool IsContentQuery { get; }

        void AddClause(string text);

        void AddClause(Expression expression);

        QueryResult Execute();
        QueryResult Execute(ExecutionHint hint);

        IEnumerable<int> ExecuteToIds();
        IEnumerable<int> ExecuteToIds(ExecutionHint hint);
    }
}

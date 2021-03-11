using System;
using System.Data;

namespace ContentRepository.Storage.Data
{
    public interface ITransactionProvider : IDisposable
    {
        long Id { get; }
        DateTime Started { get; }

        IsolationLevel IsolationLevel { get; }

        void Begin(IsolationLevel isolationLevel);
        void Commit();
        void Rollback();
    }
}
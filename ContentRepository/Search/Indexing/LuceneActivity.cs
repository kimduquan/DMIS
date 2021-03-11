using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Diagnostics;

namespace Search.Indexing
{
    [Serializable]
    internal abstract class LuceneActivity
    {
        [NonSerialized]
        private AutoResetEvent _finishSignal = new AutoResetEvent(false);

        public AutoResetEvent FinishSignal
        {
            get
            {
                return _finishSignal;
            }
        }

        internal void InternalExecute()
        {
            try
            {
#if INDEX
                var persistentActivity = this as Search.Indexing.Activities.LuceneIndexingActivity;
                var id = persistentActivity == null ? "" : ", ActivityId: " + persistentActivity.ActivityId;
                var op = DetailedLogger.CreateOperation();
                DetailedLogger.Log(op, "LUCENEACTIVITY: {0} {1}", this.GetType().Name, id); // category: INDEX
#endif
                using (new ContentRepository.Storage.Security.SystemAccount())
                    Execute();
#if INDEX
                op.Finish(); // category: INDEX
#endif
            }
            finally
            {
                if (FinishSignal != null)
                    FinishSignal.Set();
            }
        }

        internal abstract void Execute();

        public void WaitForComplete()
        {
            if (Debugger.IsAttached)
            {
                FinishSignal.WaitOne();
            }
            else
            {
                if (!FinishSignal.WaitOne(30000, false))
                {
                    throw new ApplicationException("Activity is not finishing on a timely manner");
                }
            }
        }

    }
}

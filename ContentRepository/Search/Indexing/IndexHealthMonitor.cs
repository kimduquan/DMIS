using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Storage.Search;
using ContentRepository.Storage;
using System.Diagnostics;

namespace Search.Indexing
{
    internal static class IndexHealthMonitor
    {
        private static System.Timers.Timer _timer;

        internal static void Start(System.IO.TextWriter consoleOut)
        {
            var pollInterval = ContentRepository.Storage.Data.RepositoryConfiguration.IndexHealthMonitorRunningPeriod * 1000.0;

            _timer = new System.Timers.Timer(pollInterval);
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            _timer.Disposed += new EventHandler(Timer_Disposed);
            _timer.Enabled = true;

            if (consoleOut == null)
                return;
            consoleOut.WriteLine("IndexHealthMonitor started. Frequency: {0} s", ContentRepository.Storage.Data.RepositoryConfiguration.IndexHealthMonitorRunningPeriod);
        }
        static void Timer_Disposed(object sender, EventArgs e)
        {
            _timer.Elapsed -= new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            _timer.Disposed -= new EventHandler(Timer_Disposed);
        }
        static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (StorageContext.Search.IsOuterEngineEnabled && StorageContext.Search.SearchEngine != InternalSearchEngine.Instance)
            {
                var timerEnabled = _timer.Enabled;
                _timer.Enabled = false;
                try
                {
                    LuceneManager.ExecuteLostIndexingActivities();
                }
                catch (Exception ex) //logged
                {
                    Diagnostics.Logger.WriteException(ex);
                }
                finally
                {
                    _timer.Enabled = timerEnabled;
                }
            }
        }
    }
}

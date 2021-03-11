using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackgroundOperations
{
    public class AgentManagerEventArgs : EventArgs
    {
        public AgentManagerEventArgs(Process process) { Process = process; }
        public Process Process { get; private set; }
    }

    public class AgentManager
    {
        public static readonly string AGENT_PROCESSNAME = "TaskAgent";
        public static readonly string UPDATER_PROCESSNAME = "TaskManagementUpdater";
        public static readonly string UPDATER_PACKAGENAME = "TaskManagementUpdate.zip";
        public static readonly string UPDATER_PACKAGEPARENTPATH = "/Root/System/TaskManagement";
        public static readonly string UPDATER_PACKAGEPATH = UPDATER_PACKAGEPARENTPATH + "/" + UPDATER_PACKAGENAME;
        
        public const int UPDATER_STATUSCODE_STARTED = -10000;

        private static Timer _agentTimer;
        private static int _counter;
        private static ILogger _logger;
        private static string _executionBasePath;
        private static string _agentPath;
        private static Process[] _agentProcesses;

        public static event EventHandler<AgentManagerEventArgs> ProcessStarted;
        public static event EventHandler<EventArgs> OnTaskManagementUpdateStarted;

        public static string AgentPath { get { return _agentPath; } }

        //====================================================================================== Service methods

        /// <summary>
        /// Start monitoring and reviving task executor agents.
        /// </summary>
        /// <param name="executionBasePath">The absolute path of the folder where the code is executing. This will be used for finding the agent executable if its configured path is relative.</param>
        /// <param name="logger">Helper object for logging.</param>
        /// <param name="taskManagementFolderPath">Optional path of the TaskManagement folder. Default: current execution folder.</param>
        public static void Startup(string executionBasePath, ILogger logger, bool delayedAgents, string taskManagementFolderPath = null)
        {            
            _logger = logger;
            _executionBasePath = executionBasePath;

            // TaskManagement path is different in case of Local and Distributed mode
            if (string.IsNullOrEmpty(taskManagementFolderPath))
            {
                // service mode: we look for the agent in the execution folder
                _agentPath = _executionBasePath;
            }
            else if (Path.IsPathRooted(taskManagementFolderPath))
            {
                _agentPath = taskManagementFolderPath;
            }
            else
            {
                _agentPath = Path.GetFullPath(Path.Combine(_executionBasePath, taskManagementFolderPath));
            }

            // add the agent executable name to the path
            _agentPath = Path.GetFullPath(Path.Combine(_agentPath, AGENT_PROCESSNAME + ".exe"));

            _agentProcesses = new Process[Configuration.TaskAgentCount];

            if (delayedAgents)
                Checker = new TaskChecker();
            else
                Checker = new AgentChecker();

            // We need a few seconds due time here, because if the heartbeat beats too soon the first time,
            // than there is a possibility that the Updater tool process (that starts the service as its
            // last step) is still running. That would lead to unwanted behavior, e.g. not starting agents.
            _agentTimer = new Timer(HeartBeatTimerElapsed, null, 3000, 5000);
        }

        public static void Shutdown()
        {
            ShutDownAgentProcess();
        }

        //====================================================================================== Agent manager methods

        private static void EnsureAgentProcess()
        {
            var startedCount = 0;

            try
            {
                for (var i = 0; i < _agentProcesses.Length; i++)
                {
                    if (_agentProcesses[i] == null || _agentProcesses[i].HasExited)
                    {
                        // start a new process, but do not wait for it
                        _agentProcesses[i] = Process.Start(new ProcessStartInfo(AgentPath));
                        startedCount++;

                        // notify outsiders
                        if (ProcessStarted != null)
                            ProcessStarted(null, new AgentManagerEventArgs(_agentProcesses[i]));
                    }
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                    _logger.WriteError("Agent start error. Agent path: " + AgentPath + ". ", ex);

                return;
            }

            if (startedCount > 0)
            {
                if (_logger != null)
                    _logger.WriteInformation(string.Format("{0} STARTED ({1} new instance(s) from {2}).", AGENT_PROCESSNAME, startedCount, AgentPath), EventId.AgentStarted);
            }
            else if (++_counter >= 10)
            {
                _counter = 0;

                if (_logger != null)
                    _logger.WriteVerbose(string.Format("{0} is running ({1} instance(s) from {2}).", AGENT_PROCESSNAME, Configuration.TaskAgentCount, AgentPath));
            }
        }

        private static void ShutDownAgentProcess()
        {
            if (_agentProcesses == null)
                return;

            var stopCount = 0;

            foreach (var agentProcess in _agentProcesses.Where(p => p != null && !p.HasExited))
            {
                agentProcess.Kill();
                stopCount++;
            }

            if (stopCount > 0 && _logger != null)
                _logger.WriteVerbose(string.Format("{0} instances of the {1} process were killed during shutdown.", stopCount, AGENT_PROCESSNAME));
        }

        //====================================================================================== Helper methods

        private static void HeartBeatTimerElapsed(object o)
        {
            // if an update process has been started, stop the timer and notify clients
            if (IsUpdateStarted())
            {
                _agentTimer.Change(Timeout.Infinite, Timeout.Infinite);

                if (OnTaskManagementUpdateStarted != null)
                    OnTaskManagementUpdateStarted(null, EventArgs.Empty);

                return;
            }

            Checker.Check();
        }

        private static bool IsUpdateStarted()
        {
            return Process.GetProcessesByName(UPDATER_PROCESSNAME).Length > 0;
        }

        private static class Configuration
        {
            private const string TASKAGENTCOUNTKEY = "TaskAgentCount";
            private const int DEFAULTTASKAGENTCOUNT = 1;
            private static int? _taskAgentCount;
            public static int TaskAgentCount
            {
                get
                {
                    if (!_taskAgentCount.HasValue)
                    {
                        int value;
                        if (!int.TryParse(ConfigurationManager.AppSettings[TASKAGENTCOUNTKEY], out value) || value < 1)
                            value = DEFAULTTASKAGENTCOUNT;
                        _taskAgentCount = value;
                    }

                    return _taskAgentCount.Value;
                }
            }
        }

        //====================================================================================== Delayed agent watching

        public static void AnyTaskRegistered()
        {
            AgentManager.Checker = new AgentChecker();
        }

        private static IChecker Checker = new TaskChecker();
        private interface IChecker
        {
            void Check();
        }
        private class AgentChecker : IChecker
        {
            public virtual void Check()
            {
                EnsureAgentProcess();
            }
        }
        private class TaskChecker : IChecker
        {
            public virtual void Check()
            {
                // do nothing
            }
        }
    }
}

using ContentRepository.Storage;
using ContentRepository.Storage.Data;
using Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace BackgroundOperations
{
    internal class LocalTaskManager : TaskManagerBase
    {
        private static bool _anyTaskRegistered;

        public override bool Distributed { get { return false; } }

        public override void Start()
        {
            var taskCount = TaskDataHandler.GetDeadTaskCount();
            AgentManager.Startup(HttpRuntime.BinDirectory, new AgentLogger(), taskCount == 0, "..\\" + TaskManagerBase.TASKMANAGEMENT_FOLDERNAME);
            Logger.WriteInformation(1, "LocalTaskManager AgentManager STARTED.");
        }
        public override void Stop()
        {
            AgentManager.Shutdown();
            Logger.WriteInformation(1, "LocalTaskManager AgentManager STOPPED.");
        }

        public override void RegisterTask(string type, TaskPriority priority, string data)
        {
            base.RegisterTask(type, priority, data);
            if (!_anyTaskRegistered)
            {
                _anyTaskRegistered = true;
                AgentManager.AnyTaskRegistered();
                Debug.WriteLine("@&#> LocalTaskManager.RegisterTask: AgentManager notified");
            }
        }

        //====================================================================================== Inner classes

        /// <summary>
        /// Helper logger for the agent manager class
        /// </summary>
        internal class AgentLogger : ILogger
        {
            public void WriteVerbose(string message)
            {
                Logger.WriteVerbose(message);
            }

            public void WriteInformation(string message, int eventId)
            {
                Logger.WriteInformation(eventId, message);
            }

            public void WriteError(string message, Exception ex)
            {
                Logger.WriteException(ex);
            }
        }
    }
}

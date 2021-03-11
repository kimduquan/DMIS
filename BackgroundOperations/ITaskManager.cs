using System;
using System.Collections.Generic;

namespace BackgroundOperations
{
    public interface ITaskManager
    {
        bool Distributed { get; }
        void Start();
        void Stop();

        event EventHandler<TaskFinishedEventArgs> TaskFinished;
        void RegisterTask(string type, TaskPriority priority, string data);

        bool IsUpdateAvailable(Version currentVersion, Dictionary<string, Version> executorVersions);
    }
}

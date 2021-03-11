using System;

namespace BackgroundOperations
{
    public class TaskFinishedEventArgs : EventArgs
    {
        public SnTaskResult TaskResult { get; set; }
    }
}

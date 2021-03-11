using ContentRepository.Storage;
using ContentRepository.Storage.Data;
using Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BackgroundOperations
{
    public static class TaskManager
    {
        public static void RegisterTask(string type, TaskPriority priority, string data)
        {
            Instance.RegisterTask(type, priority, data);
        }

        /*==================================================================================*/

        private static Dictionary<string, IEnumerable<ITaskFinalizer>> TaskFinalizers { get; set; }

        private static object _initializationLock = new object();
        private static ITaskManager __instance;
        private static ITaskManager Instance
        {
            get
            {
                if (__instance == null)
                {
                    lock (_initializationLock)
                    {
                        if (__instance == null)
                        {
                            ITaskManager instance;
                            if (String.IsNullOrEmpty(RepositoryConfiguration.TaskManagerClassName))
                                instance = new DefaultTaskManager();
                            else
                                instance = (ITaskManager)TypeHandler.CreateInstance(RepositoryConfiguration.TaskManagerClassName);

                            instance.TaskFinished += Instance_TaskFinished;
                            
                            TaskFinalizers = LoadTaskFinalizers();

                            Logger.WriteInformation("TaskManager created.", Logger.GetDefaultProperties, instance);
                            __instance = instance;
                        }
                    }
                }
                return __instance;
            }
        }

        private static Dictionary<string, IEnumerable<ITaskFinalizer>> LoadTaskFinalizers()
        {
            var finalizersBySupportedTasks = new Dictionary<string, IEnumerable<ITaskFinalizer>>();
            var finalizers = TypeHandler.GetTypesByInterface(typeof (ITaskFinalizer))
                .Select(t => (ITaskFinalizer) Activator.CreateInstance(t));

            foreach (var taskFinalizer in finalizers)
            {
                var supportedTaskNames = taskFinalizer.GetSupportedTaskNames();

                // iterate through all the supported task names (usually this will be a single task)
                foreach (var taskName in supportedTaskNames)
                {
                    // new task name
                    if (!finalizersBySupportedTasks.ContainsKey(taskName))
                        finalizersBySupportedTasks.Add(taskName, new List<ITaskFinalizer>());

                    // add the finalizer to the list
                    var finalizerList = finalizersBySupportedTasks[taskName] as List<ITaskFinalizer>;
                    finalizerList.Add(taskFinalizer);
                }
            }

            return finalizersBySupportedTasks;
        }

        private static void Instance_TaskFinished(object sender, TaskFinishedEventArgs e)
        {
            // no finalizers were registered to this task
            if (!TaskFinalizers.ContainsKey(e.TaskResult.Task.Type))
                return;

            // execute only those finalizers that support this kind of task
            foreach (var f in TaskFinalizers[e.TaskResult.Task.Type])
            {
                try
                {
                    f.Finalize(e.TaskResult);
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                }
            }
        }

        public static void Start()
        {
            Instance.Start();
        }
        public static void Stop()
        {
            Instance.Stop();
        }

        public static bool IsUpdateAvailable(Version currentVersion, Dictionary<string, Version> executorVersions)
        {
            return Instance.IsUpdateAvailable(currentVersion, executorVersions);
        }

        public static ServerContext ServerContext
        {
            get
            {
                return new ServerContext { ServerType = Instance.Distributed ? ServerType.Distributed : ServerType.Local };
            }
        }
    }

    internal class DefaultTaskManager : ITaskManager
    {
        public void Start() { }
        public void Stop() { }
        public bool Distributed { get { return false; } }
        public event EventHandler<TaskFinishedEventArgs> TaskFinished;
        public void RegisterTask(string type, TaskPriority priority, string data) { }
        public bool IsUpdateAvailable(Version currentVersion, Dictionary<string, Version> executorVersions) { return false; }
    }

    internal class DefaultTaskFinalizer : ITaskFinalizer
    {
        public void Finalize(SnTaskResult result)
        {
            Debug.WriteLine("#TaskManager> DefaultTaskFinalizer.Finalize called.");
        }
        
        public string[] GetSupportedTaskNames()
        {
            return new string[0];
        }
    }

}

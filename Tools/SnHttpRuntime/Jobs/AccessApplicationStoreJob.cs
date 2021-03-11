using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities.ExecutionTesting;
using ApplicationModel;
using System.IO;

namespace ConcurrencyTester
{
    [Serializable]
    public class AccessApplicationStoreJob: Job
    {
        public AccessApplicationStoreJob(string name, TextWriter output)
            : base(name, output)
        {
        }
        private static Random r = new Random();

        public override Action<JobExecutionContext> Action
        {
            get
            {
                return context =>
                {
                    var app = ApplicationStorage.Instance;
                    Console.WriteLine(this.Name + ":inshit");
                };
            }
        }
    }
}

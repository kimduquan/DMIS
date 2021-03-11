using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Services.Instrumentation
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class TraceSourceNameAttribute : Attribute
    {
        public string TraceSourceName { get; private set; }

        public TraceSourceNameAttribute(string traceSourceName)
        {
            TraceSourceName = traceSourceName;
        }
    }
}

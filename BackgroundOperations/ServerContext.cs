using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackgroundOperations
{
    public enum ServerType { Local, Distributed }

    [Serializable]
    public class ServerContext
    {
        public ServerType ServerType { get; set; }
    }
}

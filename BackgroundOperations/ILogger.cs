using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackgroundOperations
{
    public interface ILogger
    {
        void WriteVerbose(string message);
        void WriteInformation(string message, int eventId);
        void WriteError(string message, Exception ex);
    }
}

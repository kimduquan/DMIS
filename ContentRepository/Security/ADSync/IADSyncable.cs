using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContentRepository.Security.ADSync
{
    public interface IADSyncable
    {
        void UpdateLastSync(Guid? guid);
    }
}

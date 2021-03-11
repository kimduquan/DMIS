using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Diagnostics;

namespace ContentRepository.Storage.Security
{
    public class SystemAccount : IDisposable
    {
        public SystemAccount()
        {
            AccessProvider.ChangeToSystemAccount();
        }

        public void  Dispose()
        {
            AccessProvider.RestoreOriginalUser();
        }
    }
}

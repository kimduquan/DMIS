using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContentRepository
{
    public enum RestoreResultType
    {
        Nonedefined,
        Success,
        UnknownError,
        ExistingName,
        PermissionError,
        NoParent,
        ForbiddenContentType
    }
}

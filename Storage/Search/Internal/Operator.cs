using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContentRepository.Storage.Search.Internal
{
    public enum Operator
    {
        Equal,
        NotEqual,
        LessThan,
        GreaterThan,
        LessThanOrEqual,
        GreaterThanOrEqual,
        StartsWith,
        EndsWith,
        Contains
    }
}

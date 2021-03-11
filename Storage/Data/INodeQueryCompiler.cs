using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Storage.Search;

namespace ContentRepository.Storage.Data
{
    public interface INodeQueryCompiler
    {
        string Compile(NodeQuery query, out NodeQueryParameter[] parameters);
    }
}

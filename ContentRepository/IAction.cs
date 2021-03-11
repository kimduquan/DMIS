using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using ContentRepository.Storage.Schema;

namespace ContentRepository
{
    public interface IAction
    {
        string Name { get; }
        bool Enabled { get; }
        bool Visible { get; }
        IEnumerable<PermissionType> RequiredPermissions { get; }

        IAction Clone();
    }
}

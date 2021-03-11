using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectoryServices
{
    public struct ADGroupMember
    {
        public string Path;
        public string SamAccountName;
        public ADObjectType objType;
    }
}

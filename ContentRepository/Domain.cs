using System;
using ContentRepository.Schema;
using ContentRepository.Storage;
using ContentRepository.Security.ADSync;
using ContentRepository.Storage.Data;

namespace ContentRepository
{
    [ContentHandler]
    public class Domain : Folder, IADSyncable
    {
        public Domain(Node parent) : this(parent, null) { }
        public Domain(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Domain(NodeToken token) : base(token) { }

        //////////////////////////////////////// Public Properties ////////////////////////////////////////

        public bool IsBuiltInDomain
        {
            get { return Name == RepositoryConfiguration.BuiltInDomainName; }
        }

        //=================================================================================== IADSyncable Members
        public void UpdateLastSync(System.Guid? guid)
        {
            if (guid.HasValue)
                this["SyncGuid"] = ((System.Guid)guid).ToString();
            this["LastSync"] = System.DateTime.UtcNow;

            this.Save();
        }
    }
}
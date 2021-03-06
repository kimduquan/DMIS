using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using ContentRepository.Schema;
using ContentRepository.Storage;
using Search;

namespace ContentRepository
{
    [ContentHandler]
    public class VotingItem : GenericContent
    {
        public VotingItem(Node parent) : this(parent, null)
        {
        }
        public VotingItem(Node parent, string nodeTypeName) : base(parent, nodeTypeName)
        {
        }
        protected VotingItem(NodeToken nt) : base(nt)
        {    
        }

        protected override void OnCreating(object sender, ContentRepository.Storage.Events.CancellableNodeEventArgs e)
        {
            base.OnCreating(sender, e);

            if (!StorageContext.Search.IsOuterEngineEnabled)
                return;

            var searchPath = e.SourceNode.Parent.GetType().Name == "Voting"
                                 ? e.SourceNode.ParentPath
                                 : e.SourceNode.Parent.ParentPath;

            // Count Voting Items
            var votingItemCount = ContentQuery.Query(SafeQueries.InTreeAndTypeIsCountOnly,
                new QuerySettings { EnableAutofilters = FilterStatus.Disabled },
                searchPath, typeof(VotingItem).Name).Count;

            // Get children (VotingItems) count
            String tempName;
            if (votingItemCount < 10 && votingItemCount != 9)
                tempName = "VotingItem_0" + (votingItemCount + 1);
            else
                tempName = "VotingItem_" + (votingItemCount + 1);

            // If node already exits
            while (Node.Exists(RepositoryPath.Combine(e.SourceNode.Parent.Path, tempName)))
            {
                votingItemCount++;
                if (votingItemCount < 10)
                    tempName = "VotingItem_0" + (votingItemCount + 1);
                else
                    tempName = "VotingItem_" + (votingItemCount + 1);
            }

            e.SourceNode["DisplayName"] = tempName;
            e.SourceNode["Name"] = tempName.ToLower();
        }
    
    }
}

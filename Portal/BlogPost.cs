using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Schema;
using ContentRepository;
using ContentRepository.Storage;

namespace Portal
{
    [ContentHandler]
    public class BlogPost : GenericContent
    {
        public BlogPost(Node parent) : this(parent, null) { }
        public BlogPost(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected BlogPost(NodeToken nt) : base(nt) { }

        //==================================================================================== Properties
        public const string PUBLISHEDON = "PublishedOn";
        [RepositoryProperty(PUBLISHEDON, RepositoryDataType.DateTime)]
        public DateTime PublishedOn
        {
            get { return this.GetProperty<DateTime>(PUBLISHEDON); }
            set { base.SetProperty(PUBLISHEDON, value); }
        }

        //==================================================================================== Overrides
        public override void Save(NodeSaveSettings settings)
        {
            base.Save(settings);
            this.MoveToFolder();
        }
        private void MoveToFolder()
        {
            DateTime pubDate;
            if (DateTime.TryParse(this[PUBLISHEDON].ToString(), out pubDate))
            {
                string dateFolderName = String.Format("{0}-{1:00}", pubDate.Year, pubDate.Month);

                // check if the post is already in the proper folder
                if (this.ParentName == dateFolderName) return;

                // check if the proper folder exists
                var targetPath = RepositoryPath.Combine(this.WorkspacePath, String.Concat("Posts/", dateFolderName));
                if (!Node.Exists(targetPath))
                {
                    // target folder needs to be created
                    Content.CreateNew("Folder", Node.LoadNode(RepositoryPath.Combine(this.WorkspacePath, "Posts")), dateFolderName).Save();
                }

                // hide this move from journal
                this.NodeOperation = ContentRepository.Storage.NodeOperation.HiddenJournal;

                // move blog post to the proper folder
                this.MoveTo(Node.LoadNode(targetPath));
                Security.HasPermission(User.Current, Node.LoadNode(1), ContentRepository.Storage.Schema.PermissionType.Delete);
            }
        }

        //==================================================================================== Property get/set

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case PUBLISHEDON:
                    return this.PublishedOn;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case PUBLISHEDON:
                    this.PublishedOn = (DateTime)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}

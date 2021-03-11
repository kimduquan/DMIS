using ContentRepository.Schema;
using ContentRepository.Storage;

namespace ContentRepository
{
    [ContentHandler]
    public class Book : Folder
    {
        public Book(Node parent) : this(parent, null) { }
        public Book(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Book(NodeToken nt) : base(nt) { }


        public override void Save(SavingMode mode)
        {

            this["Genre"] = ParentName;
            base.Save(mode);
        }
    }
}
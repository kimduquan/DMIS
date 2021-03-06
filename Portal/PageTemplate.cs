using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using ContentRepository.Storage;
using  ContentRepository.Schema;
using ContentRepository.Storage.Search;

using SN = ContentRepository;
using ContentRepository;

namespace Portal
{
	[ContentHandler]
	public class PageTemplate : SN.File
    {

		//================================================================================= Variables

		MemoryStream _oldStream = null;
		private Stream OriginalTemplateStream
		{
			get { return _oldStream; }
			set
			{
			    if (value == null)
			    {
			        _oldStream = null;
			    }
			    else
			    {
			    var tempBuffer = new byte[value.Length];
			    value.Read(tempBuffer, 0, (int) value.Length);
			    _oldStream = new MemoryStream(tempBuffer);
			    }
		    }
		}

		//================================================================================= Properties

		[RepositoryProperty("MasterPageNode", RepositoryDataType.Reference)]
		public MasterPage MasterPageNode
		{
			get { return GetReference<MasterPage>("MasterPageNode"); }
			set { SetReference("MasterPageNode", value); }
		}

		//================================================================================= Construction

        public PageTemplate(Node parent) : this(parent, null) { }
		public PageTemplate(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
		protected PageTemplate(NodeToken nt) : base(nt) { }

		//================================================================================= Methods

        public override void Save(SavingMode mode)
        {
            bool isLocalTransaction = !TransactionScope.IsActive;
            if (isLocalTransaction)
            {
                //TransactionScope.Begin();
            }
            try
            {
                base.Save(mode);

                if (Binary != null)
                {
                    //this is very ugly: recreates pages that use this template
                    PageTemplateManager.GetBinaryData(this.Id, OriginalTemplateStream);
                }

                if (isLocalTransaction)
                {
                    //TransactionScope.Commit();
                }
            }
            finally
            {
                if (isLocalTransaction && TransactionScope.IsActive)
                {
                    //TransactionScope.Rollback();
                }
            }
        }

		//================================================================================= IFile Members

		[RepositoryProperty("Binary", RepositoryDataType.Binary)]
		public override BinaryData Binary
		{
			get
			{
				var bd = this.GetBinary("Binary");
				if (OriginalTemplateStream == null && bd != null)
					OriginalTemplateStream = bd.GetStream();
				return bd;
			}
			set { this.SetBinary("Binary", value); }
		}

		//================================================================================= Generic Property handling

		public override object GetProperty(string name)
		{
			switch (name)
			{
				case "Binary":
					return this.Binary;
				case "MasterPageNode":
					return this.MasterPageNode;
				default:
					return base.GetProperty(name);
			}
		}
		public override void SetProperty(string name, object value)
		{
			switch (name)
			{
				case "Binary":
					this.Binary = (BinaryData)value;
					break;
				case "MasterPageNode":
					this.MasterPageNode = (MasterPage)value;
					break;
				default:
					base.SetProperty(name, value);
					break;
			}
		}
	}
}
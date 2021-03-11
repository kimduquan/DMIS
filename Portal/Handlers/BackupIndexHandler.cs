using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using ApplicationModel;
using ContentRepository.Storage;
using ContentRepository.Schema;
using Search.Indexing;

namespace Portal.Handlers
{
    [ContentHandler]
    public class BackupIndexHandler : Application, IHttpHandler
    {
        public BackupIndexHandler(Node parent) : this(parent, null) { }
        public BackupIndexHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected BackupIndexHandler(NodeToken nt) : base(nt) { }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            LuceneManager.Backup();
            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write("ok");
                writer.Flush();
            }
        }
    }
}

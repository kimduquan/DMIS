using System;
using System.Collections.Generic;
using System.Security;
using ContentRepository;
using ContentRepository.Storage;
using ContentRepository.Storage.Security;
using Diagnostics;
using Portal;
using Portal.WebDAV;

namespace Services.WebDav
{
    public class MkCol : IHttpMethod
    {
        private WebDavHandler _handler;

        public MkCol(WebDavHandler handler)
        {
            _handler = handler;
        }

        #region IHttpMethod Members

        public void HandleMethod()
        {
            var parentPath = RepositoryPath.GetParentPath(_handler.GlobalPath);
            var folderName = RepositoryPath.GetFileName(_handler.GlobalPath);

            WebDavProvider.Current.AssertCreateContent(parentPath, folderName, "Folder");

            try
            {
                var f = new Folder(Node.LoadNode(parentPath)) { Name = folderName };
                f.Save();

                _handler.Context.Response.StatusCode = 201;
            }
            catch (System.Security.SecurityException e) //logged
            {
                Logger.WriteException(e);
                _handler.Context.Response.StatusCode = 403;
            }
            catch (ContentRepository.Storage.Security.SecurityException ee) //logged
            {
                Logger.WriteException(ee);
                _handler.Context.Response.StatusCode = 403;
            }
            catch (Exception eee) //logged
            {
                Logger.WriteError(Portal.EventId.WebDav.FolderError, "Could not save folder. Error: " + eee.Message, properties: new Dictionary<string, object> { { "Parent path", parentPath}, { "Folder name", folderName}});
                _handler.Context.Response.StatusCode = 405;
            }
        }

        #endregion
    }
}

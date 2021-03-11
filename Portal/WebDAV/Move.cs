﻿using System;
using System.Collections.Generic;
using System.Web;
using ContentRepository.Storage;
using ContentRepository.Storage.Security;
using System.Security;
using Diagnostics;
using Portal.WebDAV;

namespace Services.WebDav
{
    public class Move : IHttpMethod
    {
        private WebDavHandler _handler;
        public Move(WebDavHandler handler)
        {
            _handler = handler;
        }

        #region IHttpMethod Members

        public void HandleMethod()
        {
            bool overwrite = false;
            string origPath = _handler.GlobalPath;
            var destPathHeaderValue = _handler.Context.Server.UrlDecode(_handler.Context.Request.Headers["Destination"]);
            var destUrl = new Uri(destPathHeaderValue);
            var destPath = _handler.GetGlobalPath(HttpUtility.UrlDecode(destUrl.AbsolutePath));
            
            if (_handler.Context.Request.Headers["Overwrite"] != null && _handler.Context.Request.Headers["Overwrite"] == "T")
                overwrite = true;

            try
            {
                var destNode = Node.LoadNode(destPath);
                if (overwrite || destNode == null)
                {
                    var origName = RepositoryPath.GetFileName(origPath);
                    var destName = RepositoryPath.GetFileName(destPath);
                    var origNode = Node.LoadNode(_handler.GlobalPath);
                    var destParentPath = RepositoryPath.GetParentPath(destPath);
                    var originalParentPath = RepositoryPath.GetParentPath(origPath);

                    // check if moving
                    if (destParentPath != originalParentPath)
                    {
                        WebDavProvider.Current.AssertMoveContent(origNode, destParentPath);

                        // move node to destination directory
                        origNode.MoveTo(Node.LoadNode(destParentPath));
                    }
                    // renaming
                    if (origName != destName)
                    {
                        WebDavProvider.Current.AssertModifyContent(origNode);

                        origNode.Name = destName;
                        origNode.DisplayName = destName;   // also set displayname
                        origNode.Save();
                    }

                    _handler.Context.Response.StatusCode = 201;
                }
                else
                {
                    _handler.Context.Response.StatusCode = 409;
                }
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
                Logger.WriteError(Portal.EventId.WebDav.FolderError, "Error during webdav move operation. " + eee, properties: new Dictionary<string, object> {
                    {"Original path", origPath}, 
                    {"Destination path header", destPathHeaderValue},
                    {"Destination path", destPath},
                    {"Destination absolute path", destUrl.AbsolutePath}
                });

                _handler.Context.Response.StatusCode = 409;
            }
        }
        
        #endregion
    }
}

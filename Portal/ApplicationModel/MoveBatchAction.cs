using ContentRepository.i18n;
using System;
using ContentRepository;
using System.Collections.Generic;
using ContentRepository.Storage;
using System.Globalization;
using Diagnostics;
using System.Linq;
using Portal.OData;

namespace ApplicationModel
{
    public class MoveBatchAction : MoveToAction
    {
        protected override string GetCallBackScript()
        {
            return GetServiceCallBackScript(
                url: ODataTools.GetODataOperationUrl(Content, "MoveBatch", true),
                scriptBeforeServiceCall: "var paths = " + GetPathListMethod(),
                postData: "JSON.stringify({ targetPath: targetPath, paths: paths })",
                inprogressTitle: ResourceManager.Current.GetString("Action", "MoveInProgressDialogTitle"),
                successContent: ResourceManager.Current.GetString("Action", "MoveDialogContent"),
                successTitle: ResourceManager.Current.GetString("Action", "MoveDialogTitle"),
                successCallback: @"
var pathToRefresh = SN.Util.GetParentPath(paths[0]);
SN.Util.RefreshExploreTree([pathToRefresh, targetPath]);",
                errorCallback: @"
var pathToRefresh = SN.Util.GetParentPath(paths[0]);
SN.Util.RefreshExploreTree([pathToRefresh, targetPath]);",
                successCallbackAfterDialog: "location=location;",
                errorCallbackAfterDialog: "location=location;"
                );
        }

        //=========================================================================== OData

        public override bool IsODataOperation { get { return true; } }
        private ActionParameter[] _actionParameters = new[] { new ActionParameter("targetPath", typeof(string), true), new ActionParameter("paths", typeof(string[]), true) };
        public override ActionParameter[] ActionParameters { get { return _actionParameters; } }

        public override object Execute(Content content, params object[] parameters)
        {
            var targetPath = (string)parameters[0];
            var paths = (string[])parameters[1];

            var exceptions = new List<Exception>();
            foreach (var path in paths)
            {
                try
                {
                    Node.Move(path, targetPath);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);

                    //TODO: we should log only relevant exceptions here and skip
                    //business logic-related errors, e.g. lack of permissions or
                    //existing target content path.
                    Logger.WriteException(e);
                }
            }

            if (exceptions.Count > 0)
                throw new Exception(String.Join(Environment.NewLine, exceptions.Select(e => e.Message)));

            return null;
        }
    }
}

using System.Web;
using ContentRepository.i18n;
using Services.ContentStore;
using System;
using Portal.OData;

namespace ApplicationModel
{
    public class MoveToAction : OpenPickerAction
    {
        protected override string GetCallBackScript()
        {
            var callbackScript = GetServiceCallBackScript(
                url: ODataTools.GetODataOperationUrl(Content, "MoveTo", true),
                scriptBeforeServiceCall: "var path = '" + Content.Path + "'",
                postData: "JSON.stringify({ targetPath: targetPath })",
                inprogressTitle: ResourceManager.Current.GetString("Action", "MoveInProgressDialogTitle"),
                successContent: ResourceManager.Current.GetString("Action", "MoveDialogContent"),
                successTitle: ResourceManager.Current.GetString("Action", "MoveDialogTitle"),
                successCallback: @"
var pathToRefresh = SN.Util.GetParentPath(path);
SN.Util.RefreshExploreTree([pathToRefresh, targetPath]);",
                errorCallback: @"
var pathToRefresh = SN.Util.GetParentPath(path);
SN.Util.RefreshExploreTree([pathToRefresh, targetPath]);",
                successCallbackAfterDialog: @"
location=" + (this.RedirectToBackUrl ? string.Concat("\'", HttpUtility.UrlDecode(this.BackUri), "\'") : "SN.Util.GetParentUrlForPath(path)") + ";",
                errorCallbackAfterDialog: "location=location;"
                );
            
            return callbackScript;
        }

        //=========================================================================== OData

        public override bool IsODataOperation { get { return true; } }
        private ActionParameter[] _actionParameters = new[] { new ActionParameter("targetPath", typeof(string), true) };
        public override ActionParameter[] ActionParameters { get { return _actionParameters; } }

        public override object Execute(ContentRepository.Content content, params object[] parameters)
        {
            ContentRepository.Storage.Node.Move(content.Path, (string)parameters[0]);
            return null;
        }
    }
}

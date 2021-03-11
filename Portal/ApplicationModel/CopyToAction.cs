using ContentRepository.i18n;
using System;
using Portal.OData;
namespace ApplicationModel
{
    public class CopyToAction : OpenPickerAction
    {
        protected override string GetCallBackScript()
        {
            return GetServiceCallBackScript(
                url: ODataTools.GetODataOperationUrl(Content, "CopyTo", true),
                scriptBeforeServiceCall: "var path = '" + Content.Path + "'",
                postData: "JSON.stringify({ targetPath: targetPath })",
                inprogressTitle: ResourceManager.Current.GetString("Action", "CopyInProgressDialogTitle"),
                successContent: ResourceManager.Current.GetString("Action", "CopyDialogContent"),
                successTitle: ResourceManager.Current.GetString("Action", "CopyDialogTitle"),
                successCallback: @"SN.Util.RefreshExploreTree([targetPath]);",
                errorCallback: @"SN.Util.RefreshExploreTree([targetPath]);",
                successCallbackAfterDialog: "location = location;",
                errorCallbackAfterDialog: "location = location;"
                );
        }

        //=========================================================================== OData

        public override bool IsODataOperation { get { return true; } }
        private ActionParameter[] _actionParameters = new[] { new ActionParameter("targetPath", typeof(string), true) };
        public override ActionParameter[] ActionParameters { get { return _actionParameters; } }

        public override object Execute(ContentRepository.Content content, params object[] parameters)
        {
            ContentRepository.Storage.Node.Copy(content.Path, (string)parameters[0]);
            return null;
        }
    }
}

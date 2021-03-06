using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository;
using ContentRepository.i18n;
using ContentRepository.Storage;
using Portal.UI.Controls;
using Portal.Virtualization;
using System.Web.UI.WebControls;
using Search;
using Portal.Helpers;
using ContentRepository.Storage.Security;
using ContentRepository.Storage.Schema;

namespace Portal.UI
{
    public class SurveyGenericContentView : GenericContentView
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            var parent = ContentRepository.Content.Create(Content.ContentHandler.Parent);
            var survey = this.Content.ContentHandler.Parent as ContentList;
            if (survey != null && survey.FieldSettingContents.Count() == 0)
            {
                var plcEmptySurvey = this.FindControlRecursive("PlcEmptySurvey") as PlaceHolder;
                if (plcEmptySurvey != null){
                    plcEmptySurvey.Visible = true;

                    var myLabel = this.FindControlRecursive("LblNoQuestion") as Label;

                    if (myLabel != null){
                        myLabel.Visible = true;
                        myLabel.Text = ResourceManager.Current.GetString("Survey", "NoQuestion");
                    }
                }

                var gfControl = this.FindControlRecursive("GenericFieldControl1") as GenericFieldControl;
                if (gfControl == null || gfControl.Wizard == null) 
                    return;

                gfControl.Wizard.Visible = false;
                return;
            }

            //if (!SecurityHandler.HasPermission(this.ContentHandler, PermissionType.AddNew))
            //{
            //    Session["error"] = "permission";
            //    Response.Redirect(PortalContext.Current.RequestedUri.GetLeftPart(UriPartial.Path));
            //}

            if (Convert.ToBoolean(parent["EnableLifespan"]) &&
                (DateTime.UtcNow < Convert.ToDateTime(parent["ValidFrom"]) ||
                 DateTime.UtcNow > Convert.ToDateTime(parent["ValidTill"])))
            {
                Session["error"] = "invalid";
                Response.Redirect(PortalContext.Current.RequestedUri.GetLeftPart(UriPartial.Path));
            }

            if (Convert.ToBoolean(parent["EnableMoreFilling"])) 
                return;         

            if (ContentQuery.Query("+Type:surveyitem +InFolder:@0 +CreatedById:@1 .COUNTONLY", null, parent.Path, User.Current.Id).Count > 0)
            {
                Session["error"] = "morefilling";
                Response.Redirect(PortalContext.Current.RequestedUri.GetLeftPart(UriPartial.Path));
            }
        }
    }
}

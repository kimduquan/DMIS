using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using ApplicationModel;
using ContentRepository.Storage;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Security;
using ContentRepository.i18n;
using Search;
using ContentRepository;
using System.Web.UI.WebControls;

namespace Portal.UI
{
    public class SurveyContentView : SingleContentView
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (Session["error"] != null)
            {
                var error = Session["error"].ToString();

                switch (error)
                {
                    case "permission":
                        SetMessageText(ResourceManager.Current.GetString("Survey", "NoPermission"));
                        break;

                    case "invalid":
                        SetInvalidSurveyView();
                        break;

                    case "morefilling":
                        SetMessageText(ResourceManager.Current.GetString("Survey", "CannotFillMore"));
                        break;
                }

                Session.Remove("error");
            }
            else
            {
                if (((List<Node>)Content.Fields["FieldSettingContents"].GetData()).Count() == 0)
                {
                    SetMessageText(ResourceManager.Current.GetString("Survey", "NoQuestion"));
                    return;
                }

                if (!SecurityHandler.HasPermission(this.ContentHandler, PermissionType.AddNew))
                {
                    SetMessageText(ResourceManager.Current.GetString("Survey", "NoPermission"));
                    return;
                }

                if (Convert.ToBoolean(Content["EnableLifespan"]) &&
                    (DateTime.UtcNow < Convert.ToDateTime(Content["ValidFrom"]) ||
                        DateTime.UtcNow > Convert.ToDateTime(Content["ValidTill"])))
                {
                    SetInvalidSurveyView();
                    return;
                }

                if (!Convert.ToBoolean(Content["EnableMoreFilling"]) && ContentQuery.Query("+Type:surveyitem +InFolder:@0 +CreatedById:@1 .AUTOFILTERS:OFF .COUNTONLY", null,
                        Content.Path, User.Current.Id).Count > 0)
                {
                    SetMessageText(ResourceManager.Current.GetString("Survey", "CannotFillMore"));
                    return;
                }
                
                SetHyperLink();
            }
        }

        private void SetMessageText(string text)
        {
            var literalMessage = this.FindControl("LiteralMessage") as Label;
            if (literalMessage == null) 
                return;

            literalMessage.Text = text;
            literalMessage.Visible = true;
        }

        private void SetInvalidSurveyView()
        {
            var placeHolder = this.FindControl("phInvalidPage");
            var templateContent = ContentRepository.Content.Load(this.ContentHandler.GetReference<Node>("InvalidSurveyPage").Path);
            var pageContentView = this.ContentHandler.GetReference<Node>("PageContentView").Path;
            var contentView = ContentView.Create(templateContent, this.Page, ViewMode.Browse, pageContentView);

            if (placeHolder != null && contentView != null)
                placeHolder.Controls.Add(contentView);
        }

        private void SetHyperLink()
        {
            var hyperLinkFill = this.FindControl("HyperLinkFill") as HyperLink;
            if (hyperLinkFill == null) 
                return;

            hyperLinkFill.NavigateUrl = ActionFramework.GetAction("Add", Content, new {ContentTypeName = "SurveyItem"}).Uri;
            hyperLinkFill.Visible = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Portal.Virtualization;

namespace Portal.UI.PortletFramework
{
    public class QueryBuilderEditorPartField : TextEditorPartField
    {
        //================================================================================= Properties

        private QueryBuilderEditorPartOptions _textEditorOptions;
        public QueryBuilderEditorPartOptions QueryBuilderEditorOptions
        {
            get 
            {
                return _textEditorOptions ?? (_textEditorOptions = this.Options as QueryBuilderEditorPartOptions ?? new QueryBuilderEditorPartOptions());
            }
        }

        //================================================================================= Overrides

        protected override void OnPreRender(EventArgs e)
        {
            //we need to place this here, because editor parts are only rendered,
            //not added as controls, so OnInit is not executed!
            UITools.AddScript(UITools.ClientScriptConfigurations.SNQueryBuilderJSPath);
            UITools.AddStyleSheetToHeader(UITools.GetHeader(), UITools.ClientScriptConfigurations.SNQueryBuilderCSSPath);

            base.OnPreRender(e);
        }

        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            this.Attributes.Add("spellcheck","false");
            base.Render(writer);

            var contentPath = PortalContext.Current.ContextNode.ParentPath;
            var contentName = PortalContext.Current.ContextNode.Name;
            var content = contentPath + "('" + contentName + "')";

            //construct the jquery selector to find the appropriate text area and initialize the plugin
            var script = string.Concat(@"$('.sn-editorpart-", this.PropertyName, @" textarea').queryBuilder({
                showQueryEditor: true,
                showQueryBuilder: true,
                content: """ + content + "\"            });");

            UITools.RegisterStartupScript("querybuilder_" + this.ClientID, script, Page);
        }
    }
}

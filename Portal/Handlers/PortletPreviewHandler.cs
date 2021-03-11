using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using ContentRepository.Storage;
using Portal.Virtualization;
using Portal.UI.PortletFramework;
using Portal.UI.Controls;

namespace Portal.Handlers
{
    public enum PreviewPortletType
    {
        None = 0,
        ContentCollectionPortlet
    }

    public partial class PortletPreviewHandler : System.Web.UI.Page
    {
        /* ====================================================================== Query params */
        public string CustomRootPath
        {
            get
            {
                return Request.QueryString["customrootpath"];
            }
        }
        public string Renderer
        {
            get
            {
                return Request.QueryString["renderer"];
            }
        }
        public string PortletTypeStr
        {
            get
            {
                return Request.QueryString["portlettype"];
            }
        }
        public Portal.UI.PortletFramework.RenderMode RenderingMode
        {
            get
            {
                if (string.IsNullOrEmpty(Renderer))
                    return Portal.UI.PortletFramework.RenderMode.Ascx;

                if (Renderer.EndsWith(".ascx"))
                    return Portal.UI.PortletFramework.RenderMode.Ascx;

                return Portal.UI.PortletFramework.RenderMode.Xslt;
            }
        }


        /* ====================================================================== Properties */
        public PreviewPortletType PortletType
        {
            get
            {
                if (string.IsNullOrEmpty(PortletTypeStr))
                    return PreviewPortletType.None;

                if (PortletTypeStr.ToUpper() == "CONTENTCOLLECTIONPORTLET")
                    return PreviewPortletType.ContentCollectionPortlet;

                // default:
                return PreviewPortletType.ContentCollectionPortlet;
            }
        }

        protected PlaceHolder renderedContent
        {
            get
            {
                return this.FindControlRecursive("renderedContent") as PlaceHolder;
            }
        }

        /* ====================================================================== Methods */
        protected override void OnInit(EventArgs e)
        {
            //ViewStateUserKey = Session.SessionID;
            CreateLayout();
            base.OnInit(e);
        }
        private void CreateLayout()
        {
            switch (PortletType)
            {
                case PreviewPortletType.ContentCollectionPortlet:
                    var collectionPortlet = TypeHandler.CreateInstance("Portal.Portlets.ContentCollectionPortlet") as ContextBoundPortlet;
                    collectionPortlet.ID = "previewedPortlet";
                    collectionPortlet.BindTarget = Portal.UI.PortletFramework.BindTarget.CustomRoot;
                    collectionPortlet.CustomRootPath = CustomRootPath;
                    collectionPortlet.RenderingMode = RenderingMode;
                    collectionPortlet.Renderer = Renderer;
                    //this.form1.Controls.Add(collectionPortlet);
                    renderedContent.Controls.Add(collectionPortlet);
                    break;
                default:
                    break;
            }
        }
    }
}

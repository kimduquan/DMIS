using System;
using System.Collections.Generic;
using ContentRepository.Storage;
using ContentRepository.Storage.Search;
using ContentRepository.Storage.Security;
using Portal.UI.ContentListViews.FieldControls;
using Portal.UI.Controls;
using Search;
using ContentRepository.Storage.Schema;

namespace Portal.UI.ContentListViews
{
    public abstract class ViewBase : System.Web.UI.UserControl
    {
        #region properties

        private DataSource _viewDataSource;
        protected DataSource ViewDataSource
        {
            get
            {
                if (_viewDataSource == null)
                    _viewDataSource = this.FindControl("ViewDataSource") as DataSource;

                return _viewDataSource;
            }
        }

        private ViewFrame _ownerFrame;
        public ViewFrame OwnerFrame
        {
            get
            {
                if (_ownerFrame == null)
                    _ownerFrame = ViewFrame.GetContainingViewFrame(this);

                return _ownerFrame;
            }
        }

        private Handlers.ViewBase _viewDefinition;
        public Handlers.ViewBase ViewDefinition
        {
            get { return _viewDefinition ?? (_viewDefinition = ViewManager.LoadViewWithPermissions(this.AppRelativeVirtualPath.Substring(1)) as Handlers.ViewBase); }
        }

        private Node _contextNode;
        public Node ContextNode
        {
            get
            {
                if (_contextNode == null)
                    _contextNode = OwnerFrame.ContextNode;

                return _contextNode;
            }

        }

        #endregion

        #region view_queries

        protected virtual NodeQuery GetFilter()
        {
            NodeQuery filter = null;
            if (!string.IsNullOrEmpty(ViewDefinition.FilterXml))
            {
                if (ViewDefinition.FilterIsContentQuery)
                    return null;
                
                filter = NodeQuery.Parse(Query.GetNodeQueryXml(ViewDefinition.FilterXml));
            }

            return filter;
        }

        #endregion

        #region aspnet_members

        protected override void OnLoad(EventArgs e)
        {
            ViewDataSource.ContentPath = ContextNode.Path;

            if (ViewDefinition != null)
            {
                if (ViewDefinition.FilterIsContentQuery)
                {
                    if (this.OwnerFrame == null || this.OwnerFrame.OwnerPortlet == null)
                        ViewDataSource.Query = ViewDefinition.FilterXml;
                    else
                        ViewDataSource.Query = this.OwnerFrame.OwnerPortlet.ReplaceTemplates(ViewDefinition.FilterXml);
                    
                }
                else
                {
                    ViewDataSource.QueryFilter = GetFilter();
                }
            }

            if (ViewDataSource.Settings == null)
                ViewDataSource.Settings = new QuerySettings();

            if (ViewDefinition != null)
            {
                if (ViewDefinition.EnableAutofilters != FilterStatus.Default)
                    ViewDataSource.Settings.EnableAutofilters = ViewDefinition.EnableAutofilters;

                if (ViewDefinition.EnableLifespanFilter != FilterStatus.Default)
                    ViewDataSource.Settings.EnableLifespanFilter = ViewDefinition.EnableLifespanFilter;

                if (ViewDefinition.QueryTop > 0)
                    ViewDataSource.Settings.Top = ViewDefinition.QueryTop;

                if (ViewDefinition.QuerySkip > 0)
                    ViewDataSource.Settings.Skip = ViewDefinition.QuerySkip;
            }

            base.OnLoad(e);
        }

        #endregion

        #region abstract_members

        protected abstract IEnumerable<string> GetFieldList();

        #endregion
    }
}

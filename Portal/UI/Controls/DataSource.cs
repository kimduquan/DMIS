using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using ContentRepository;
using ContentRepository.Storage.Search;
using Portal.Virtualization;
using ContentRepository.Storage;
using ContentRepository.Schema;
using ContentRepository.Storage.Schema;
using Portal.UI.PortletFramework;
using Search;

namespace Portal.UI.Controls
{
    public delegate void SqlDataSourceStatusEventHandler(object sender, DataSourceStatusEventArgs e);

    public class DataSource : DataSourceControl
    {
        private DataSourceView _view;

        //======================================================= Properties

        public string ContextInfoID { get; set; }

        public Expression QueryFilter { get; set; }

        private string _query;
        public string Query
        {
            get { return _query; }
            set
            {
                _query = value;

                if (_view != null)
                    _view.QueryText = value;
            }
        }

        private QuerySettings _settings;
        public QuerySettings Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;

                if (_view != null)
                    _view.Settings = value;
            }
        }

        public string ContentPath
        {
            get { return this.Content == null ? string.Empty : this.Content.Path; } 
            set{ this.Content = string.IsNullOrEmpty(value) ? null : Content.Load(value); }
        }

        public string MemberName { get; set; }

        public string FieldNames { get; set; }

        public string DefaultOrdering { get; set; }

        public string GroupBy { get; set; }

        private bool _showHidden = true;
        public bool ShowHidden
        {
            get { return _showHidden; }
            set { _showHidden = value; }
        }

        [Obsolete("Use Settings property instead.")]
        public bool ShowSystem
        {
            get
            {
                return LucQuery.IsAutofilterEnabled(Settings.EnableAutofilters);
            }
            set
            {
                if (Settings == null)
                    Settings = new QuerySettings();
                Settings.EnableAutofilters = value ? FilterStatus.Enabled : FilterStatus.Disabled;
            }
        }

        public bool FlattenResults { get; set; }

        [Obsolete("Use Settings property instead.")]
        public int? Top
        {
            get
            {
                if (Settings == null || Settings.Top == 0)
                    return null;

                return Settings.Top;
            }
            set
            {
                if (Settings == null)
                    Settings = new QuerySettings();

                Settings.Top = value.HasValue ? value.Value : 0;
            }
        }

        public Content Content { get; set; }

        public int Count { get; set; }

        //======================================================= Methods

        public IEnumerable<Content> Select(DataSourceSelectArguments selectArgs)
        {
            var view = GetView(DataSourceView.DefaultViewName);

            return ((DataSourceView)view).Select(selectArgs);
        }

        protected override System.Web.UI.DataSourceView GetView(string viewName)
        {
            if (null == _view)
            {
                RefreshContextInfo();

                _view = new DataSourceView(this)
                            {
                                Content = this.Content,
                                MemberName = this.MemberName,
                                FieldNames = this.FieldNames,
                                QueryFilter = this.QueryFilter,
                                QueryText = this.Query,
                                ShowHidden = this.ShowHidden,
                                Settings = this.Settings,
                                FlattenResults = this.FlattenResults,
                                DefaultOrdering = this.DefaultOrdering,
                                GroupBy = this.GroupBy
                            };

                _view.Selected += SnDataSourceViewSelected;
            }

            return _view;
        }

        protected void SnDataSourceViewSelected(object sender, DataSourceStatusEventArgs e)
        {
            this.Count = e.AffectedRows;
        }

        protected override ICollection GetViewNames()
        {
            return new List<string> {DataSourceView.DefaultViewName};
        }

        private ContextInfo FindContextInfo(string controlID)
        {
            if (string.IsNullOrEmpty(controlID))
                return null;

            var nc = this as Control;
            Control control = null;

            while (control == null && nc != this.Page)
            {
                nc = nc.NamingContainer;

                if (nc == null)
                    throw new ArgumentException(string.Format("No ContextInfo control found with the ID '{0}'", controlID), "controlID");

                control = nc.FindControl(controlID);
            }

            if (control == null)
                control = nc.FindControl(controlID);

            return control as ContextInfo;
        }

        private void RefreshContextInfo()
        {
            var ci = FindContextInfo(this.ContextInfoID);

            if (ci == null)
            {
                this.ContextInfoID = null;
            }
            else
            {
                this.ContentPath = ci.Path;
            }

            if (this.Content != null) 
                return;

            var ctx = ContextBoundPortlet.GetContextNodeForControl(this);
            if (ctx != null)
                this.ContentPath = ctx.Path;
        }

        public void ResetView()
        {
            _view = null;
        }
    }

    public class DataSourceStatusEventArgs : EventArgs
    {
        public DataSourceStatusEventArgs(int affectedRows) : this(affectedRows, null)
        {
        }

        public DataSourceStatusEventArgs(int affectedRows, Exception ex)
        {
            this.AffectedRows = affectedRows;
            this.Exception = ex;
        }

        public int AffectedRows { get; private set; }
        public Exception Exception { get; private set; }
    }
}

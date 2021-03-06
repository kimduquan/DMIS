using System;
using System.Collections.Generic;
using System.Linq;
using Portal.UI.PortletFramework;
using System.Web.UI.WebControls;
using repository = ContentRepository;
using System.Web.UI.WebControls.WebParts;
using ContentRepository;
using ContentRepository.Storage;
using ContentRepository.Storage.Security;
using System.ComponentModel;

namespace Portal.Portlets
{
    public class ContentQueryPresenterPortlet : ContentCollectionPortlet
    {
        private const string ContentQueryPresenterPortletClass = "ContentQueryPresenterPortlet";

        public const string ContentListID = "ContentList";

        [LocalizedWebDisplayName(ContentQueryPresenterPortletClass, "Prop_QueryString_DisplayName")]
        [LocalizedWebDescription(ContentQueryPresenterPortletClass, "Prop_QueryString_Description")]
        [WebBrowsable(true), Personalizable(true)]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order), WebOrder(10)]
        [Editor(typeof(QueryBuilderEditorPartField), typeof(IEditorPartField))]
        [QueryBuilderEditorPartOptions(TextEditorCommonType.MultiLine)]
        public string QueryString { get; set; }

        public static class DataBindingHelper
        {
            //private static Dictionary<Type, Action<object, object>> map;

            //static DataBindingHelper()
            //{
            //    map = new Dictionary<Type, Action<object, object>>();
            //    map.Add(
            //        typeof(DataBoundControl),
            //            (control, data) =>
            //            {
            //                ((DataBoundControl)control).DataSource = data;
            //                ((DataBoundControl)control).DataBind();
            //            });
            //    map.Add(typeof(BaseDataList),
            //            (control, data) =>
            //            {
            //                ((BaseDataList)control).DataSource = data;
            //                ((BaseDataList)control).DataBind();
            //            });

            //}
            public static void SetDataSourceAndBind(object control, object dataSource)
            {
                if (control is DataBoundControl)
                {
                    DataBoundControl bindable = (DataBoundControl)control;
                    bindable.DataSource = dataSource;
                    bindable.DataBind();
                }
                else if (control is BaseDataList)
                {
                    BaseDataList bindable = (BaseDataList)control;
                    bindable.DataSource = dataSource;
                    bindable.DataBind();
                }
                else if (control is Repeater)
                {
                    Repeater bindable = (Repeater)control;
                    bindable.DataSource = dataSource;
                    bindable.DataBind();
                }
                else
                    throw new NotSupportedException(control.GetType().Name + " is not a supported data control");

                //foreach (var type in map.Keys)
                //{
                //    if (type.IsAssignableFrom(control.GetType()))
                //    {
                //        map[type](control, dataSource);
                //        return;
                //    }
                //}
                //throw new NotSupportedException(control.GetType().Name + " is not a supported data control");
            }

        }

        protected override void RenderWithAscx(System.Web.UI.HtmlTextWriter writer)
        {
            this.RenderContents(writer);
        }

        public ContentQueryPresenterPortlet()
        {
            this.Name = "$ContentQueryPresenterPortlet:PortletDisplayName";
            this.Description = "$ContentQueryPresenterPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Collection);
            this.HiddenPropertyCategories = new List<string>() { EditorCategory.Collection, EditorCategory.ContextBinding };
        }

        protected override object GetModel()
        {
            var sf = SmartFolder.GetRuntimeQueryFolder();
            sf.Query = ReplaceTemplates(this.QueryString);

            var c = ContentRepository.Content.Create(sf);

            //Get base model as Content and use some of its children definition properties.
            //Do not override the whole ChildrenDefinition object here because SmartFolder 
            //has its own special children definition override.
            var oldc = base.GetModel() as ContentRepository.Content;
            if (oldc != null)
            {
                c.ChildrenDefinition.EnableAutofilters = oldc.ChildrenDefinition.EnableAutofilters;
                c.ChildrenDefinition.EnableLifespanFilter = oldc.ChildrenDefinition.EnableLifespanFilter;
                c.ChildrenDefinition.Skip = oldc.ChildrenDefinition.Skip;
                c.ChildrenDefinition.Sort = oldc.ChildrenDefinition.Sort;
                c.ChildrenDefinition.Top = oldc.ChildrenDefinition.Top;
            }

            return c;
        }
    }
}

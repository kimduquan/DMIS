using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository;
using System.Xml.Serialization;
using Diagnostics;
using Portal.Virtualization;
using ContentRepository.Storage;

namespace Portal.Portlets
{
    //TODO rename
    [XmlRoot("Model")]
    public class ContentCollectionViewModel : ContentViewModel
    {
        
        [XmlIgnore]
        public IQueryable<Content> Items
        {
            get
            {
                if (this.Content == null)
                    return new List<Content>().AsQueryable();

                try
                {
                    if (string.IsNullOrEmpty(this.ReferenceAxisName))
                        return this.Content.Children;
                }
                catch (Exception)
                {
                    //this error is already logged in the portlet
                    return new List<Content>().AsQueryable();
                }

                try
                {
                    var items = this.Content[ReferenceAxisName] as IEnumerable<Node>;
                    if (items == null)
                    {
                        Logger.WriteWarning(Logger.EventId.NotDefined, string.Format("Content collection portlet error: invalid reference field ({0}).", ReferenceAxisName));
                        return new List<Content>().AsQueryable();
                    }

                    return items.Select(node => Content.Create(node)).AsQueryable();
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                    return new List<Content>().AsQueryable();
                }
            }
        }

        [XmlIgnore]
        public string ReferenceAxisName { get; set; }

        public PagerModel Pager { get; set; }

        public ContentCollectionPortletState State { get; set; }

        [XmlArray("VisibleFieldNames")]
        [XmlArrayItem("FieldName")]
        public string[] VisibleFieldNames
        {
            get
            {
                return State.VisibleFieldNames;
            }
            set
            {
            }
        }
        [XmlArray("SortActions")]
        [XmlArrayItem("Sort")]
        public SortByColumnAction[] FieldSortActions
        {
            get
            {
                return SortActions.ToArray();
            }
            set
            {
            }
        }

        [XmlIgnore]
        public IEnumerable<SortByColumnAction> SortActions
        {
            get
            {
                foreach (var field in State.VisibleFieldNames)
                {
                    yield return new SortByColumnAction()
                    {
                        Portlet = (ContentCollectionPortlet)State.Portlet,
                        SortColumn = field,
                        SortDescending = false
                    };
                    yield return new SortByColumnAction()
                    {
                        Portlet = (ContentCollectionPortlet)State.Portlet,
                        SortColumn = field,
                        SortDescending = true
                    };

                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ContentRepository.Schema;
using ContentRepository.Storage;
using Diagnostics;

namespace ContentRepository
{
    [ContentHandler]
    public class Task : GenericContent
    {
        public Task(Node parent) : this(parent, null) { }
		public Task(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Task(NodeToken nt) : base(nt) { }

        public int RemainingDays
        {
            get 
            {
                try
                {
                    var dueDate = this.GetProperty<DateTime>("DueDate");

                    return dueDate.Year < ActiveSchema.DateTimeMinValue.Year ? 0 : Math.Abs((dueDate - DateTime.Today).Days);
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                }

                return 0;
            }
        }

        public string DueText
        {
            get
            {
                try
                {
                    var dueDate = this.GetProperty<DateTime>("DueDate").Date;

                    if (dueDate < DateTime.Today) return HttpContext.GetGlobalResourceObject("Portal", "DaysOverdue") as string;
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                }

                return HttpContext.GetGlobalResourceObject("Portal", "DaysLeft") as string;
            }
        }

        public string DueCssClass
        {
            get
            {
                try
                {
                    var dueDate = this.GetProperty<DateTime>("DueDate").Date;

                    if (dueDate < DateTime.Today) return "sn-deadline-overdue";
                    if (dueDate < DateTime.Today.AddDays(7)) return "sn-deadline-soon";
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                }

                return "sn-deadline-later";
            }
        }

        //================================================================================= Generic Property handling

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "RemainingDays":
                    return this.RemainingDays;
                case "DueText":
                    return this.DueText;
                case "DueCssClass":
                    return this.DueCssClass;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}

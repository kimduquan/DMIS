using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Portal.Virtualization;
using ContentRepository;
using ContentRepository.Storage;
using ContentRepository.i18n;

namespace Portal.UI
{
    public class VotingResultContentView : SingleContentView
    {
        public Dictionary<string, string> Result { get; set; }
        public int DecimalsInResult { get; set; }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            var voting = Node.LoadNode(Content.Id) as Voting;
            if (voting == null)
            {
                Controls.Add(new Literal { Text = "Error with loading the content with ID: " + Content.Id });
                return;
            }

            var dt = new DataTable();

            dt.Columns.AddRange(new[] { 
                                        new DataColumn("Question", typeof(String)),
                                        new DataColumn("Count", typeof(Double))
                                });

            var sum = voting.Result.Sum(item => Convert.ToDouble(item.Value));

            var formatString = string.Concat("N",
                                                DecimalsInResult < 0 || DecimalsInResult > 5
                                                    ? 0
                                                    : DecimalsInResult);

            foreach (var item in voting.Result)
            {
                string classname;
                string keyname;
                var itemText = item.Key;
                if (ResourceManager.ParseResourceKey(itemText, out classname, out keyname))
                    itemText = ResourceManager.Current.GetString(classname, keyname);

                dt.Rows.Add(new[]{
                     itemText,
                     sum == 0 || Convert.ToInt32(item.Value) == 0 ? 0.ToString() : ((Convert.ToDouble(item.Value)/sum)*100).ToString(formatString)
                });
            }

            var lv = FindControl("ResultList") as ListView;
            if (lv != null)
            {
                lv.DataSource = dt.DefaultView;
                lv.DataBind();
            }

            
        }
    }
}

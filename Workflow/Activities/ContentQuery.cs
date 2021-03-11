using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;

namespace Workflow.Activities
{
    public class ContentQuery : AsyncCodeActivity<Search.QueryResult>
    {
        public InArgument<string> QueryText { get; set; }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var queryText = QueryText.Get(context);

            var queryDelegate = new Func<string, Search.QueryResult>(RunQuery);
            context.UserState = queryDelegate;
            return queryDelegate.BeginInvoke(queryText, callback, state);
        }

        protected override Search.QueryResult EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var queryDelegate = (Func<string, Search.QueryResult>)context.UserState;
            return (Search.QueryResult)queryDelegate.EndInvoke(result);
        }

        private Search.QueryResult RunQuery(string text)
        {
            return Search.ContentQuery.Query(text);
        }
    }
}

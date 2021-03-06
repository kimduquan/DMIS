using System;
using System.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using ContentRepository.Storage;
using ContentRepository.Storage.Search;
using Diagnostics;

namespace Portal.Portlets
{
    public class NodeQueryXsltProxy
    {
        [XmlRoot]
        public class Result
        {
            public Services.ContentStore.Content[] ContentList;
        }

        [XmlRoot("Exception")]
        public class QueryException
        {
            private string message;

            public string Message
            {
                get { return message; }
                set { message = value; }
            }
        }

        public XPathNavigator Execute(object param)
        {
            return Execute(param, true);
        }

        public XPathNavigator Execute(object param, bool raiseExceptions)
        {
            try
            {
                NodeQuery queryFromXml = NodeQuery.Parse(((XPathNavigator)param).OuterXml);
                var result = queryFromXml.Execute();

                Result queryResult = new Result()
                {
                    ContentList =
                        result.Nodes.Select(
                            node => new Services.ContentStore.Content(node)).ToArray()
                };

                return queryResult.ToXPathNavigator();
            }
            catch (Exception exc) //logged
            {
                if (raiseExceptions)
                    throw;
                Logger.WriteException(exc);
                return new QueryException() { Message = exc.Message }.ToXPathNavigator();
            }
        }
    }
}

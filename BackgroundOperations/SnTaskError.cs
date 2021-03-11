using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackgroundOperations
{
    public class SnTaskError
    {
        public string ErrorCode { get; set; }
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }

        public static SnTaskError Create(Exception e)
        {
            return new SnTaskError
            {
                ErrorCode = null,
                ErrorType = e.GetType().Name,
                Message = e.Message,
                Details = e.StackTrace
            };
        }

        public static SnTaskError Parse(string src)
        {
            var result = new SnTaskError();
            try
            {
                var jErr = JObject.Parse(src);
                foreach (var prop in jErr.Properties())
                {
                    var val = prop.Value.ToString();
                    switch (prop.Name)
                    {
                        case "ErrorCode": result.ErrorCode = val; break;
                        case "Message": result.Message = val; break;
                        case "ErrorType": result.ErrorType = val; break;
                        case "Details": result.Details = val; break;
                    }
                }
            }
            catch //compensation
            {
                result.ErrorCode = "unknown";
                result.Message = "An error occured during error parsing. The Details property contains the raw error data of the tast executor.";
                result.ErrorType = "unknown";
                result.Details = src;
            }
            return result;
        }

        public override string ToString()
        {
            var writer = new StringWriter();
            JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
                .Serialize(writer, this);
            return writer.GetStringBuilder().ToString();
        }
    }
}

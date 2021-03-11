using System;
using System.Collections.Generic;
using BackgroundOperations;
using Newtonsoft.Json;
using IO = System.IO;
using Diagnostics;
using ContentRepository;
using ContentRepository.Storage.Security;
using ContentRepository.Storage;
using Newtonsoft.Json.Linq;

namespace Preview
{
    public class PreviewGeneratorTaskFinalizer : ITaskFinalizer
    {
        public virtual void Finalize(SnTaskResult result)
        {
            // not enough information
            if (result.Task == null || string.IsNullOrEmpty(result.Task.TaskData))
                return;

            // the task was executed successfully without an error message
            if (result.Successful && result.Error == null)
                return;

            try
            {
                if (result.Error != null)
                {
                    // log the error message and details for admins
                    Logger.WriteError(ContentRepository.EventId.Preview.PreviewGenerationError,
                        "Preview generation error, see the details below.", properties: new Dictionary<string, object>
                        {
                            {"ErrorCode", result.Error.ErrorCode},
                            {"ErrorType", result.Error.ErrorType},
                            {"Message", result.Error.Message},
                            {"Details", result.Error.Details}
                        });
                }

                // deserialize task data to retrieve content info
                var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
                var serializer = JsonSerializer.Create(settings);

                using (var jreader = new JsonTextReader(new IO.StringReader(result.Task.TaskData)))
                {
                    var previewData = serializer.Deserialize(jreader) as JObject;
                    var contentId = previewData["Id"].Value<int>();

                    using (new SystemAccount())
                    {
                        DocumentPreviewProvider.SetPreviewStatus(Node.Load<File>(contentId), PreviewStatus.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }
        }

        public virtual string[] GetSupportedTaskNames()
        {
            // We need to collect the supported preview generator tasks from
            // the provider, because 3rd party developers may freely create
            // their own custom task executors for generating preview images
            // for certain file types.
            return DocumentPreviewProvider.Current.GetSupportedTaskNames();
        }
    }
}

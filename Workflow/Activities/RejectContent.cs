using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using System.ComponentModel;
using ContentRepository.Storage;
using ContentRepository;
using Workflow.Activities.Design;

namespace Workflow.Activities
{
    [Designer(typeof(RejectContentDesigner))]
    public sealed class RejectContent : NativeActivity
    {
        public InArgument<string> ContentPath { get; set; }
        public InArgument<string> Reason { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            var contentItem = Node.Load<GenericContent>(ContentPath.Get(context));
            using (InstanceManager.CreateRelatedContentProtector(contentItem, context))
            {
                contentItem["RejectReason"] = Reason == null ? string.Empty : (Reason.Get(context) ?? string.Empty);
                contentItem.Reject();
            }
        }
    }
}

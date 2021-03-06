using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ContentRepository;
using ContentRepository.Storage.Security;
using Workflow.Activities.Design;

namespace Workflow.Activities
{
    [Designer(typeof(CreateStructureDesigner))]
    public class CreateStructure : NativeActivity<WfContent>
    {
        public InArgument<string> FullPath { get; set; }
        public InArgument<string> ContainerTypeName { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            using (new SystemAccount())
            {
                var content = Content.Load(FullPath.Get(context));
                if (content != null)
                {
                    //return the leaf content if exists
                    Result.Set(context, new WfContent(content.ContentHandler));
                    return;
                }

                //create structure
                var ctName = ContainerTypeName.Get(context);
                
                content = string.IsNullOrEmpty(ctName)
                    ? Tools.CreateStructure(FullPath.Get(context)) 
                    : Tools.CreateStructure(FullPath.Get(context), ctName);

                Result.Set(context, new WfContent(content.ContentHandler));
            }
        }

        protected override void Cancel(NativeActivityContext context)
        {
            Debug.WriteLine("##WF> CreateStructure.Cancel: " + FullPath.Get(context));
            base.Cancel(context);
        }
    }
}

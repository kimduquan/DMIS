using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using ContentRepository.Storage;

namespace Workflow.Activities
{
    public class LoadContent : NativeActivity<WfContent>
    {
        public InArgument<string> Path { get; set; }



        protected override void Execute(NativeActivityContext context)
        {
            Result.Set(context, new WfContent() { Path = Path.Get(context) });
        }
    }
}

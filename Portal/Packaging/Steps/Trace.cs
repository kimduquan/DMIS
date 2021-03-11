using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Packaging.Steps;

namespace Packaging.Steps
{
    [Annotation("Writes the value of the Text property to the console.")]
    public class Trace : Step
    {
        [DefaultProperty]
        [Annotation("Runtime information that will be displayed.")]
        public string Text { get; set; }

        public override void Execute(Packaging.ExecutionContext context)
        {
            Logger.LogMessage(this.Text);
        }
    }
}

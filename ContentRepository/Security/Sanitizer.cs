using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContentRepository.Security
{
    public class Sanitizer
    {
        public static string Sanitize(string userInput)
        {
            return HtmlSanitizer.sanitize(userInput);
        }
    }
}

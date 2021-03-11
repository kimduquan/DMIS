using System;
using ContentRepository.Security;

namespace Portal
{
    public class Security
    {
        [Obsolete("Use ContentRepository.Security.Sanitizer.Sanitize instead.")]
        public static string Sanitize(string userInput)
        {
            return Sanitizer.Sanitize(userInput);
        }
    }
}

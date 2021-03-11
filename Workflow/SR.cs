using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.i18n;

namespace Workflow
{
    internal static class SR
    {
        internal static class Mail
        {
            internal static string Error_UnknownAttachmentType_3 = "$Error_Workflow:Mail_UnknownAttachmentType_3";
        }

        public static string GetString(string fullResourceKey)
        {
            return ResourceManager.Current.GetString(fullResourceKey);
        }

        public static string GetString(string fullResourceKey, params object[] args)
        {
            return String.Format(GetString(fullResourceKey), args);
        }
    }
}

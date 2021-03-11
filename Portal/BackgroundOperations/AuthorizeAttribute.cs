using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using ContentRepository.Security;
using ContentRepository.Storage.Security;
using ContentRepository;
using ContentRepository.Storage;
using ContentRepository.Storage.Schema;

namespace BackgroundOperations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class AuthorizeAttribute : Microsoft.AspNet.SignalR.AuthorizeAttribute
    {
        private static readonly string PermissionPlaceholderPath = "/Root/System/PermissionPlaceholders/Signalr/";

        protected Type HubType { get; private set; }

        public AuthorizeAttribute(Type type)
        {
            HubType = type;
        }

        protected override bool UserAuthorized(System.Security.Principal.IPrincipal user)
        {
            var princ = user as PortalPrincipal;
            if (princ == null || princ.Identity == null)
                throw new ArgumentNullException("user");

            if (!princ.Identity.IsAuthenticated)
                return false;

            var permissionHead = NodeHead.Get(PermissionPlaceholderPath + HubType.Name);
            if (permissionHead != null && SecurityHandler.HasPermission(permissionHead, PermissionType.RunApplication))
                return true;

            return false;
        }
    }
}

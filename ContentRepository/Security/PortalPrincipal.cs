using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Principal;
using ContentRepository.Storage.Security;

namespace ContentRepository.Security
{
    public class PortalPrincipal : IPrincipal
    {
        IUser _user;

        public PortalPrincipal(IUser user)
        {
            if (user == null)
                throw new ArgumentNullException("user", "The user parameter cannot be null. Use 'User.Visitor' instead.");
            _user = user;
        }

        #region IPrincipal Members

        public IIdentity Identity
        {
            get { return _user; }
        }

        public bool IsInRole(string role)
        {
            throw new NotSupportedException("Role management is not supported.");
        }

        #endregion
    }
}
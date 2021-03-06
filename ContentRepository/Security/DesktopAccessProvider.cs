using System;
using ContentRepository.Schema;
using ContentRepository.Storage.Security;
using System.Security.Principal;
using System.Threading;

namespace ContentRepository.Security
{
    public class SystemPrincipal : IPrincipal
    {
        IUser user;

        public SystemPrincipal(IUser user)
        {
            this.user = user;
        }

        //========================================================== IPrincipal Members

        public IIdentity Identity { get { return user; } }
        public bool IsInRole(string role) { return false; }
    }

    public class DesktopAccessProvider : AccessProvider
    {
        private bool _initialized;

        private IUser CurrentUser
        {
            get
            {
                var user = Thread.CurrentPrincipal.Identity as IUser;
                if (user != null)
                    return user;

                CurrentUser = StartupUser;
                user = User.Administrator;
                CurrentUser = user;
                return user;
            }
            set
            {
                Thread.CurrentPrincipal = new SystemPrincipal(value);
            }
        }

        public override IUser GetCurrentUser()
        {
            if (!_initialized)
            {
                _initialized = true;
                AccessProvider.ChangeToSystemAccount();
                CurrentUser = User.Administrator;
            }
            return CurrentUser;
        }

        protected override void DoSetCurrentUser(IUser user)
        {
            CurrentUser = user;
        }

        public override bool IsAuthenticated
        {
            get { return true; }
        }

        protected override void Initialize()
        {
            base.Initialize();
            ContentType.TypeSystemRestarted += ContentType_TypeSystemRestarted;
        }

        void ContentType_TypeSystemRestarted(object sender, EventArgs e)
        {
            // Do not reset system user here, because if this reset
            // happens inside a using(new SystemAccount) block, it would
            // ruin the desired credential settings.
            var cu = GetCurrentUser();
            if (cu != null && cu.Username == "SYSTEM")
                return;

            _initialized = false;
            Thread.CurrentPrincipal = null;
        }
    }
}
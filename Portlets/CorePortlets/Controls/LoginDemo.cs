using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.Security;
using ContentRepository;
using ContentRepository.Storage.Security;
using System.Web.UI.WebControls;
using System.Web;
using Portal.Virtualization;

namespace Portal.Portlets.Controls
{
    public class LoginDemo : UserControl
    {
        protected override void CreateChildControls()
        {
            foreach (var control in this.Controls)
            {
                var link = control as LinkButton;
                if (link == null)
                    continue;

                link.Click += new EventHandler(link_Click);
            }

            this.ChildControlsCreated = true;
        }

        void link_Click(object sender, EventArgs e)
        {
            var link = sender as LinkButton;
            if (link == null)
                return;

            var fullUserName = link.CommandArgument;

            if (string.IsNullOrEmpty(fullUserName))
                return;

            var slashIndex = fullUserName.IndexOf('\\');
            var domain = fullUserName.Substring(0, slashIndex).Trim('\\');
            var username = fullUserName.Substring(slashIndex + 1).Trim('\\');

            IUser user;

            // Elevation: we need to have the user content in hand
            // regardless of the current users permissions (she is
            // a Visitor right now, before logging in).
            using (new SystemAccount())
            {
                user = User.Load(domain, username); 
            }

            if (user == null)
                return;

            var password = _demoPasswords[user.Name.ToLowerInvariant()];
            if (!PasswordHashProvider.CheckPassword(password, user.PasswordHash, (IPasswordSaltProvider)user))
                return;


            FormsAuthentication.SetAuthCookie(user.Username, false);
            var originalUrl = PortalContext.GetLoginOriginalUrl();
            if (String.IsNullOrEmpty(originalUrl))
                originalUrl = Request.RawUrl;

            Response.Redirect(originalUrl);
        }

        Dictionary<string, string> _demoPasswords = new Dictionary<string, string>
        {
            {"builtin\\alba", "alba" },
            {"builtin\\mike", "mike" },
            {"builtin\\admin", "admin" },
        };
    }
}

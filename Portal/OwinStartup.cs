using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Diagnostics;
using Microsoft.AspNet.SignalR;
using ContentRepository.Storage.Data;

[assembly: OwinStartup(typeof(Portal.OwinStartup))]

namespace Portal
{
    public class OwinStartup
    {
        public void Configuration(IAppBuilder app)
        {
            // create SignalR SQL tables only if the NLB option is enabled
            if (RepositoryConfiguration.SignalRSqlEnabled)
                GlobalHost.DependencyResolver.UseSqlServer(RepositoryConfiguration.SignalRDatabaseConnectionString);

            app.MapSignalR();
        }
    }
}

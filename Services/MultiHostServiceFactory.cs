using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Diagnostics;
using System.ServiceModel.Web;
using System.Web;
using System.Web.Hosting;
using Services.ContentStore;

namespace Services
{
     public class CustomHost : ServiceHost
     {
        public CustomHost(Type serviceType, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        { }

        protected override void ApplyConfiguration()
        {
            //base.ApplyConfiguration();
        }
    }
    public class MultiHostServiceFactory<T> : WebServiceHostFactory
    {
        public MultiHostServiceFactory()
        {
            //Debug.WriteLine("cant creat error factory");
        }

        public override ServiceHostBase CreateServiceHost(string constructorString, Uri[] baseAddresses)
        {
            StackTrace st = new StackTrace(true);
            HttpContext context = HttpContext.Current;
            baseAddresses.Select(ba => { Debug.WriteLine(ba); return true; }).ToArray();
            //
            var sh = new CustomHost(typeof(T), baseAddresses[0]);
            
            return sh;
        }

        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return base.CreateServiceHost(serviceType, baseAddresses);
        }

    }

    public class ContentStoreServiceFactory : MultiHostServiceFactory<ContentStoreService>
    {
    }
}

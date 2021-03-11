using ContentRepository.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;

namespace Portal
{
    public abstract class WebApiConfiguration
    {
        internal static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            var configTypes = TypeHandler.GetTypesByBaseType(typeof(WebApiConfiguration));
            foreach (var type in configTypes)
                if(!type.IsAbstract)
                    ((WebApiConfiguration)Activator.CreateInstance(type)).Configure(config);

            //config.Routes.MapHttpRoute(
            //    name: "DefaultApi",
            //    routeTemplate: "api/{controller}/{id}",
            //    defaults: new { id = RouteParameter.Optional }
            //);

            //var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
            //config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);


            //config.Services.Replace(typeof(System.Web.Http.Dispatcher.IHttpControllerTypeResolver), new SnHttpControllerTypeResolver());

            //var suffix = typeof(System.Web.Http.Dispatcher.DefaultHttpControllerSelector).GetField("ControllerSuffix", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            //if (suffix != null)
            //    suffix.SetValue(null, string.Empty);

            //config.Services.Replace(typeof(System.Web.Http.Dispatcher.IHttpControllerSelector), new SnHttpControllerSelector(config));
        }

        //public IHttpRoute MapHttpRoute(this HttpRouteCollection routes, string name, string routeTemplate, object defaults, object constraints, HttpMessageHandler handler);

        protected abstract void Configure(HttpConfiguration config);
    }

}
